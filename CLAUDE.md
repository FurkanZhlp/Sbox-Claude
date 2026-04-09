# CLAUDE.md — s&box MCP Bridge

## Architecture

```
Claude AI (MCP Client)
      ↕  stdio JSON-RPC
sbox-mcp-server  (Node.js / TypeScript)
      ↕  WebSocket  ws://localhost:8765
Claude Bridge Plugin  (C# / s&box Editor)
      ↕  s&box Engine APIs
Scene / GameObjects / Components
```

Two parts work together:

1. **MCP Server** (`sbox-mcp-server/`) — A Node.js process Claude connects to via
   stdio. It speaks the Model Context Protocol and forwards every tool call to s&box
   over WebSocket.

2. **s&box Plugin** (`sbox-plugin/`) — A C# editor plugin that runs a WebSocket
   server inside the s&box editor. It receives JSON commands and dispatches them to
   registered handlers that call the s&box engine APIs.

---

## Project Structure

```
sbox-plugin/
├── .addon                              # s&box addon metadata
├── sbox-plugin.csproj                  # C# project (Sandbox.Game.Sdk)
├── BridgeServer.cs                     # WebSocket server + request routing
├── LogCapture.cs                       # Captures s&box log messages
└── Handlers/
    ├── IToolHandler.cs                 # Handler interface
    ├── GetConsoleOutputHandler.cs      # get_console_output
    ├── CreateGameObjectHandler.cs      # create_gameobject
    ├── DeleteGameObjectHandler.cs      # delete_gameobject
    ├── SetTransformHandler.cs          # set_transform
    ├── GetSceneHierarchyHandler.cs     # get_scene_hierarchy
    ├── GetAllPropertiesHandler.cs      # get_all_properties
    └── AddComponentWithPropertiesHandler.cs  # add_component_with_properties

sbox-mcp-server/
├── package.json
├── tsconfig.json
├── jest.config.json
├── .eslintrc.json
├── .prettierrc
├── src/
│   ├── index.ts                        # Entry point + MCP server setup
│   ├── BridgeClient.ts                 # WebSocket client (reconnect + ping)
│   └── tools/
│       ├── console.ts                  # Console / logging tools
│       ├── gameobjects.ts              # Scene-building tools
│       └── status.ts                   # Bridge health / status tool
└── tests/
    └── BridgeClient.test.ts            # WebSocket reconnect tests

examples/
└── horror-game/
    ├── CLAUDE.md                       # Example project context file
    ├── PlayerController.cs             # Starter player controller
    └── horror-game.scene               # Minimal scene JSON

CLAUDE.md          ← you are here
CONTRIBUTING.md
README.md
```

---

## Wire Protocol

Every WebSocket message is UTF-8 JSON. Messages stay under 1 MB.

**Request** (MCP Server → s&box Plugin):

```json
{ "id": "550e8400-e29b-41d4-a716", "command": "create_gameobject", "params": { "name": "Cube" } }
```

**Success response** (s&box Plugin → MCP Server):

```json
{ "id": "550e8400-e29b-41d4-a716", "result": { "guid": "abc123", "name": "Cube" } }
```

**Error response**:

```json
{ "id": "550e8400-e29b-41d4-a716", "error": { "code": "UNKNOWN_COMMAND", "message": "Unknown command: foo" } }
```

Error codes used by `BridgeServer.cs`:

| Code | Meaning |
|------|---------|
| `INVALID_REQUEST` | Missing id/command, malformed JSON, or message >1 MB |
| `UNKNOWN_COMMAND` | No handler registered for the command name |
| `HANDLER_ERROR` | Handler threw an exception |

---

## How to Add a New Tool

### Step 1 — Create the C# handler

Create `sbox-plugin/Handlers/MyToolHandler.cs`:

```csharp
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxClaude;

public class MyToolHandler : IToolHandler
{
    // Must exactly match the MCP tool name used in TypeScript
    public string Command => "my_tool";

    public Task<object> ExecuteAsync(JsonElement parameters)
    {
        // Read parameters safely
        var value = parameters.TryGetProperty("my_param", out var p)
            ? p.GetString()
            : "default";

        // Use s&box APIs here
        // Scene.Active, new GameObject(), TypeLibrary, etc.

        // Return any JSON-serializable object
        return Task.FromResult<object>(new { success = true, value });
    }
}
```

### Step 2 — Register the handler in BridgeServer.cs

In `RegisterHandlers()`, add one line:

```csharp
private void RegisterHandlers()
{
    var handlers = new IToolHandler[]
    {
        new GetConsoleOutputHandler(),
        new CreateGameObjectHandler(),
        // ... existing handlers ...
        new MyToolHandler(),   // <-- add here
    };
    foreach (var h in handlers)
        _handlers[h.Command] = h;
}
```

### Step 3 — Add the MCP tool definition

In `sbox-mcp-server/src/tools/` (add to an existing file or create a new one):

```typescript
import type { Tool } from '@modelcontextprotocol/sdk/types.js';
import type { BridgeClient } from '../BridgeClient.js';

export const MY_TOOL: Tool = {
  name: 'my_tool',
  description:
    'One sentence. Be specific — Claude uses this to decide when to call the tool.',
  inputSchema: {
    type: 'object',
    properties: {
      my_param: {
        type: 'string',
        description: 'What this parameter does',
      },
    },
    required: ['my_param'],
  },
};

export async function handleMyTool(
  args: Record<string, unknown>,
  bridge: BridgeClient
): Promise<unknown> {
  return bridge.send('my_tool', args);
}
```

### Step 4 — Wire it into index.ts

```typescript
// At the top of src/index.ts, add the import:
import { MY_TOOL, handleMyTool } from './tools/mytools.js';

// Add to the allTools array:
const allTools: Tool[] = [
  ...CONSOLE_TOOLS,
  ...GAMEOBJECT_TOOLS,
  ...STATUS_TOOLS,
  MY_TOOL,          // <-- add here
];

// Add a case to the dispatch switch:
} else if (name === 'my_tool') {
  result = await handleMyTool(args, bridge);
}
```

That's it. Four files touched, usually only one new file created.

---

## s&box API Quick Reference

```csharp
// ── Scene ─────────────────────────────────────────────────────────────────
Scene.Active                            // the running scene
Scene.Active.GetAllObjects(false)       // flat list of all GOs (false = include inactive)
Scene.Active.Children                   // root-level GameObjects only

// ── GameObjects ───────────────────────────────────────────────────────────
var go = new GameObject(true, "Name");  // create enabled GO
go.Parent = Scene.Active;               // attach to scene root
go.Parent = otherGo;                    // or to another GO
go.Destroy();                           // remove from scene
go.Id                                   // Guid (use .ToString() for JSON)
go.Enabled                              // bool
go.Name                                 // string

// ── Transform ─────────────────────────────────────────────────────────────
go.WorldPosition = new Vector3(x, y, z);
go.WorldRotation = Rotation.From(pitch, yaw, roll);  // degrees
go.WorldScale    = new Vector3(x, y, z);
go.LocalPosition / go.LocalRotation / go.LocalScale  // parent-relative

// ── Components ────────────────────────────────────────────────────────────
go.Components.Create(typeDesc);         // add component by TypeDescription
go.Components.GetAll();                 // IEnumerable<Component>
go.Components.Get<T>();                 // first component of type T

// ── TypeLibrary ───────────────────────────────────────────────────────────
var td = TypeLibrary.GetType("Rigidbody");   // TypeDescription by name
td.Properties                                // IEnumerable<PropertyDescription>
pd.GetValue(instance)                        // read a property value
pd.SetValue(instance, value)                 // write a property value
pd.HasAttribute<PropertyAttribute>()        // check for [Property]

// ── Logging ───────────────────────────────────────────────────────────────
Log.Info("message");
Log.Warning("message");
Log.Error("message");
```

---

## Environment Variables

| Variable    | Default     | Description                |
|-------------|-------------|----------------------------|
| `SBOX_HOST` | `localhost` | Host where s&box is running |
| `SBOX_PORT` | `8765`      | Bridge WebSocket port       |

---

## Running Locally

```bash
# 1. Install and load the s&box plugin
#    Copy sbox-plugin/ into your s&box addons folder and restart the editor.
#    The bridge starts automatically and logs: [Claude Bridge] Listening on port 8765

# 2. Build and start the MCP server
cd sbox-mcp-server
npm install
npm run build
npm start

# 3. Connect Claude Code
#    Add the MCP server to your Claude Code config:
#    claude mcp add sbox -- node /path/to/sbox-mcp-server/dist/index.js
```

## Testing Without s&box

The test suite uses a mock WebSocket server (no s&box required):

```bash
cd sbox-mcp-server
npm test
```

See `tests/BridgeClient.test.ts` for examples of how to mock the bridge for
integration tests of your own handlers.
