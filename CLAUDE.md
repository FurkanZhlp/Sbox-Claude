# s&box + Claude Code MCP Integration

> Let non-coders build s&box games through conversation with Claude Code.

## Status: Phase 4 Complete — Phase 5 Next

**Last updated:** 2026-04-09
**Current phase:** Phase 4 (Play & Test) ✅ — 53 tools implemented
**Next up:** Phase 5 (Game Logic Templates) — prefabs, player controllers, AI, UI systems

---

## Architecture

```
Claude Code → (stdio) → MCP Server → (WebSocket :29015) → Bridge Addon → s&box Editor
```

Two components:
1. **MCP Server** (`sbox-mcp-server/`) — TypeScript/Node.js, stdio transport, talks to Claude Code
2. **Bridge Addon** (`sbox-bridge-addon/`) — C#, runs inside s&box editor, executes commands

The MCP Server translates Claude's tool calls into WebSocket messages. The Bridge Addon receives them inside the running s&box editor and calls the actual engine APIs.

---

## Project Structure

```
Sbox-Claude/
├── CLAUDE.md                          ← YOU ARE HERE — project context for Claude
├── README.md                          ← User-facing docs + setup guide
├── .gitignore
│
├── sbox-mcp-server/                   # MCP Server (TypeScript)
│   ├── package.json
│   ├── tsconfig.json
│   ├── src/
│   │   ├── index.ts                   # Entry point — registers all tools, starts stdio
│   │   ├── transport/
│   │   │   └── bridge-client.ts       # WebSocket client → s&box Bridge (:29015)
│   │   └── tools/
│   │       ├── project.ts             # get_project_info, list_project_files, read_file, write_file
│   │       ├── scripts.ts             # create_script, edit_script, delete_script, trigger_hotload
│   │       ├── console.ts             # get_console_output, get_compile_errors, clear_console
│   │       ├── scenes.ts              # list_scenes, load_scene, save_scene, create_scene
│   │       ├── gameobjects.ts         # create/delete/duplicate/rename, set_parent/enabled/transform, hierarchy, selection
│   │       ├── components.ts          # get_property, get_all_properties, list_available_components, add_component
│   │       ├── assets.ts              # search_assets, list_asset_library, install_asset, get_asset_info
│   │       ├── materials.ts           # assign_model, create_material, assign_material, set_material_property
│   │       ├── audio.ts              # list_sounds, create_sound_event, assign_sound, play_sound_preview
│   │       ├── playmode.ts           # play mode control, runtime properties, screenshot, undo/redo, set_property
│   │       └── status.ts             # get_bridge_status diagnostic tool
│   └── dist/                          # Compiled JS (gitignored, built with `npm run build`)
│
└── sbox-bridge-addon/                 # s&box Bridge Addon (C#)
    ├── sbox-bridge-addon.sbproj       # s&box project config
    └── Code/
        ├── Core/
        │   ├── BridgeAddon.cs         # Entry point — registers handlers on editor load
        │   ├── BridgeServer.cs        # WebSocket server, accepts connections, dispatches commands
        │   ├── ICommandHandler.cs     # Interface: Execute(JsonElement) → Task<object>
        │   ├── LogCapture.cs          # Hooks Logger.OnMessage → buffers for get_console_output
        │   └── ComponentHelper.cs     # Serialize/deserialize component property values
        └── Commands/
            ├── GetProjectInfoHandler.cs
            ├── ListProjectFilesHandler.cs
            ├── ReadFileHandler.cs
            ├── WriteFileHandler.cs
            ├── CreateScriptHandler.cs
            ├── EditScriptHandler.cs
            ├── DeleteScriptHandler.cs
            ├── TriggerHotloadHandler.cs
            ├── GetConsoleOutputHandler.cs
            ├── GetCompileErrorsHandler.cs
            ├── ClearConsoleHandler.cs
            ├── ListScenesHandler.cs
            ├── LoadSceneHandler.cs
            ├── SaveSceneHandler.cs
            ├── CreateSceneHandler.cs
            ├── CreateGameObjectHandler.cs
            ├── DeleteGameObjectHandler.cs
            ├── DuplicateGameObjectHandler.cs
            ├── RenameGameObjectHandler.cs
            ├── SetParentHandler.cs
            ├── SetEnabledHandler.cs
            ├── SetTransformHandler.cs
            ├── GetPropertyHandler.cs
            ├── GetAllPropertiesHandler.cs
            ├── ListAvailableComponentsHandler.cs
            ├── AddComponentWithPropertiesHandler.cs
            ├── GetSceneHierarchyHandler.cs
            ├── GetSelectedObjectsHandler.cs
            ├── SelectObjectHandler.cs
            ├── FocusObjectHandler.cs
            ├── SearchAssetsHandler.cs
            ├── ListAssetLibraryHandler.cs
            ├── InstallAssetHandler.cs
            ├── GetAssetInfoHandler.cs
            ├── AssignModelHandler.cs
            ├── CreateMaterialHandler.cs
            ├── AssignMaterialHandler.cs
            ├── SetMaterialPropertyHandler.cs
            ├── ListSoundsHandler.cs
            ├── CreateSoundEventHandler.cs
            ├── AssignSoundHandler.cs
            ├── PlaySoundPreviewHandler.cs
            ├── PlayModeHandler.cs
            ├── SetPropertyHandler.cs
            ├── RuntimePropertyHandler.cs
            ├── TakeScreenshotHandler.cs
            └── UndoRedoHandler.cs
```

---

## Implemented Tools

### Phase 1 — Foundation (15 tools) ✅

| Tool | MCP File | Bridge Handler | What It Does |
|------|----------|----------------|-------------|
| `get_project_info` | `tools/project.ts` | `GetProjectInfoHandler.cs` | Returns project path, name, type, deps |
| `list_project_files` | `tools/project.ts` | `ListProjectFilesHandler.cs` | Browse file tree, filter by dir/extension |
| `read_file` | `tools/project.ts` | `ReadFileHandler.cs` | Read any project file contents |
| `write_file` | `tools/project.ts` | `WriteFileHandler.cs` | Create/overwrite files, auto-mkdir |
| `create_script` | `tools/scripts.ts` | `CreateScriptHandler.cs` | Generate C# component with boilerplate or raw |
| `edit_script` | `tools/scripts.ts` | `EditScriptHandler.cs` | Find/replace, insert, append, delete lines |
| `delete_script` | `tools/scripts.ts` | `DeleteScriptHandler.cs` | Remove script files |
| `trigger_hotload` | `tools/scripts.ts` | `TriggerHotloadHandler.cs` | Force recompile + hotload |
| `get_console_output` | `tools/console.ts` | `GetConsoleOutputHandler.cs` | Read buffered log entries by severity |
| `get_compile_errors` | `tools/console.ts` | `GetCompileErrorsHandler.cs` | Get diagnostics with file/line/column |
| `clear_console` | `tools/console.ts` | `ClearConsoleHandler.cs` | Clear log buffer |
| `list_scenes` | `tools/scenes.ts` | `ListScenesHandler.cs` | Find all .scene files in project |
| `load_scene` | `tools/scenes.ts` | `LoadSceneHandler.cs` | Open scene in editor |
| `save_scene` | `tools/scenes.ts` | `SaveSceneHandler.cs` | Save current scene |
| `create_scene` | `tools/scenes.ts` | `CreateSceneHandler.cs` | New scene with optional defaults |

### Phase 2 — Scene Building (15 tools) ✅

| Tool | MCP File | Bridge Handler | What It Does |
|------|----------|----------------|-------------|
| `create_gameobject` | `tools/gameobjects.ts` | `CreateGameObjectHandler.cs` | Create object with name, position, rotation, scale, parent |
| `delete_gameobject` | `tools/gameobjects.ts` | `DeleteGameObjectHandler.cs` | Remove object by GUID |
| `duplicate_gameobject` | `tools/gameobjects.ts` | `DuplicateGameObjectHandler.cs` | Clone with all components, optional offset |
| `rename_gameobject` | `tools/gameobjects.ts` | `RenameGameObjectHandler.cs` | Change display name |
| `set_parent` | `tools/gameobjects.ts` | `SetParentHandler.cs` | Reparent object (or move to root) |
| `set_enabled` | `tools/gameobjects.ts` | `SetEnabledHandler.cs` | Enable/disable object |
| `set_transform` | `tools/gameobjects.ts` | `SetTransformHandler.cs` | Set position/rotation/scale (world or local) |
| `get_property` | `tools/components.ts` | `GetPropertyHandler.cs` | Read single component property value |
| `get_all_properties` | `tools/components.ts` | `GetAllPropertiesHandler.cs` | Dump all properties as JSON |
| `list_available_components` | `tools/components.ts` | `ListAvailableComponentsHandler.cs` | Browse all component types (built-in + custom) |
| `add_component_with_properties` | `tools/components.ts` | `AddComponentWithPropertiesHandler.cs` | Add component + set properties in one call |
| `get_scene_hierarchy` | `tools/gameobjects.ts` | `GetSceneHierarchyHandler.cs` | Full scene tree with GUIDs, components, positions |
| `get_selected_objects` | `tools/gameobjects.ts` | `GetSelectedObjectsHandler.cs` | What the user has selected in editor |
| `select_object` | `tools/gameobjects.ts` | `SelectObjectHandler.cs` | Programmatically select an object |
| `focus_object` | `tools/gameobjects.ts` | `FocusObjectHandler.cs` | Move editor camera to look at object |

### Phase 3 — Assets & Resources (12 tools) ✅

| Tool | MCP File | Bridge Handler | What It Does |
|------|----------|----------------|-------------|
| `search_assets` | `tools/assets.ts` | `SearchAssetsHandler.cs` | Search project assets by name/type |
| `list_asset_library` | `tools/assets.ts` | `ListAssetLibraryHandler.cs` | Browse community asset packages |
| `install_asset` | `tools/assets.ts` | `InstallAssetHandler.cs` | Add community package to project |
| `get_asset_info` | `tools/assets.ts` | `GetAssetInfoHandler.cs` | Detailed asset metadata |
| `assign_model` | `tools/materials.ts` | `AssignModelHandler.cs` | Set model on ModelRenderer (auto-creates) |
| `create_material` | `tools/materials.ts` | `CreateMaterialHandler.cs` | New .vmat with shader + properties |
| `assign_material` | `tools/materials.ts` | `AssignMaterialHandler.cs` | Apply material to renderer slot |
| `set_material_property` | `tools/materials.ts` | `SetMaterialPropertyHandler.cs` | Change color, roughness, texture, etc. |
| `list_sounds` | `tools/audio.ts` | `ListSoundsHandler.cs` | Find sound assets in project |
| `create_sound_event` | `tools/audio.ts` | `CreateSoundEventHandler.cs` | New .sound with volume, pitch, falloff |
| `assign_sound` | `tools/audio.ts` | `AssignSoundHandler.cs` | Attach sound to SoundPointComponent |
| `play_sound_preview` | `tools/audio.ts` | `PlaySoundPreviewHandler.cs` | Preview sound in editor |

### Phase 4 — Play & Test (11 tools) ✅

| Tool | MCP File | Bridge Handler | What It Does |
|------|----------|----------------|-------------|
| `start_play` | `tools/playmode.ts` | `PlayModeHandler.cs` | Enter play mode |
| `stop_play` | `tools/playmode.ts` | `PlayModeHandler.cs` | Exit play mode |
| `pause_play` | `tools/playmode.ts` | `PlayModeHandler.cs` | Pause running game |
| `resume_play` | `tools/playmode.ts` | `PlayModeHandler.cs` | Resume paused game |
| `is_playing` | `tools/playmode.ts` | `PlayModeHandler.cs` | Check state: playing/paused/stopped |
| `set_property` | `tools/playmode.ts` | `SetPropertyHandler.cs` | Write a component property (editor mode) |
| `get_runtime_property` | `tools/playmode.ts` | `RuntimePropertyHandler.cs` | Read property during play mode |
| `set_runtime_property` | `tools/playmode.ts` | `RuntimePropertyHandler.cs` | Write property during play mode |
| `take_screenshot` | `tools/playmode.ts` | `TakeScreenshotHandler.cs` | Capture viewport as PNG |
| `undo` | `tools/playmode.ts` | `UndoRedoHandler.cs` | Undo last editor action |
| `redo` | `tools/playmode.ts` | `UndoRedoHandler.cs` | Redo last undone action |

### Phase 5–7 — See README.md roadmap

---

## How to Add a New Tool

Every tool requires exactly two files:

### 1. Bridge Handler (C#) — `sbox-bridge-addon/Code/Commands/YourHandler.cs`

```csharp
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxBridge;

public class YourHandler : ICommandHandler
{
    public Task<object> Execute( JsonElement parameters )
    {
        // Read params: parameters.GetProperty("name").GetString()
        // Call s&box APIs
        // Return anonymous object (gets serialized to JSON)
        return Task.FromResult<object>( new { result = "ok" } );
    }
}
```

### 2. MCP Tool (TypeScript) — add to existing file or create `sbox-mcp-server/src/tools/your-domain.ts`

```typescript
server.tool(
  "your_tool_name",
  "Description of what this tool does",
  {
    param1: z.string().describe("What this param is"),
  },
  async (params) => {
    const res = await bridge.send("your_tool_name", params);
    if (!res.success) {
      return { content: [{ type: "text", text: `Error: ${res.error}` }] };
    }
    return { content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }] };
  }
);
```

### 3. Register it in `BridgeAddon.cs`

```csharp
BridgeServer.RegisterHandler( "your_tool_name", new YourHandler() );
```

### 4. If new tool file, register in `index.ts`

```typescript
import { registerYourTools } from "./tools/your-domain.js";
registerYourTools(server, bridge);
```

---

## Coding Conventions

### Bridge Addon (C#)
- One handler class per command in `Code/Commands/`
- Class name = `{CommandPascalCase}Handler` (e.g. `GetProjectInfoHandler`)
- All file paths are **relative to s&box project root**
- Always validate paths stay within project dir: normalize `projectRoot` with trailing separator, then `fullPath.StartsWith(projectRoot)`
- Use s&box's `Log.Info()` / `Log.Warning()` for debug output
- Tab indentation, Allman-ish braces with s&box spacing style

### MCP Server (TypeScript)
- Tools grouped by domain in `src/tools/` (project, scripts, console, scenes)
- Use Zod schemas for parameter validation
- Every tool returns `{ content: [{ type: "text", text: ... }] }`
- Error responses: `Error: ${res.error}` format
- Bridge command name = MCP tool name (1:1 mapping)

### Protocol
- WebSocket default port: **29015** (configurable via `SBOX_BRIDGE_PORT`)
- Request format: `{ id: string, command: string, params: object }`
- Response format: `{ id: string, success: boolean, data?: any, error?: string }`
- Timeout: 30 seconds per request

---

## Development

```bash
# Build MCP Server
cd sbox-mcp-server && npm install && npm run build

# Watch mode (auto-rebuild on change)
cd sbox-mcp-server && npm run dev

# Connect to Claude Code
claude mcp add sbox -- node /path/to/sbox-mcp-server/dist/index.js
```

The Bridge Addon is compiled automatically by s&box when placed in the addons directory.

---

## Troubleshooting

### Bridge won't connect
- Is s&box running? The editor must be open with the Bridge Addon installed
- Check port: default is 29015, configurable via `SBOX_BRIDGE_PORT`
- Run `get_bridge_status` to see connection state and latency
- Firewall: ensure localhost:29015 isn't blocked

### Commands timeout (30s)
- The s&box editor may be frozen (compiling, loading assets)
- Try `get_bridge_status` — if latency is -1, the connection is dead
- Restart the MCP server and reconnect

### Compile errors after script edit
1. Run `get_compile_errors` to see what's broken (file, line, message)
2. Fix with `edit_script` (find/replace the broken code)
3. Run `trigger_hotload` to recompile
4. Run `get_compile_errors` again to verify clean

### Scene hierarchy is empty
- No scene loaded — use `list_scenes` then `load_scene`
- Scene exists but has no objects — use `create_gameobject` to add some

### Play mode failures
- `is_playing` returns current state — check before calling pause/resume
- Runtime property tools throw if not in play mode — call `start_play` first
- `stop_play` discards all runtime changes — save the scene first if needed

### Screenshot returns placeholder
- The screenshot API (`Camera.RenderToTexture()`) needs real s&box SDK wiring
- See `TakeScreenshotHandler.cs` for 3 candidate APIs to try

---

## Known Limitations / TODO

- [ ] Several s&box APIs need verification against real SDK (see API-NOTE comments in handlers)
- [ ] LogCapture has 3 candidate hook APIs — compile against SDK and uncomment the right one
- [ ] `TakeScreenshotHandler` uses placeholder until camera render API is wired
- [ ] `UndoRedoHandler` uses `Undo.PerformUndo()` — may need different API path
- [ ] `EditorScene.IsPaused` in `IsPlayingHandler` — may not exist, fallback to playing/stopped only
- [ ] No authentication on WebSocket — fine for localhost, would need auth for remote
- [ ] `create_scene` generates JSON manually — should use s&box scene serialization if available
- [ ] Batch commands run sequentially — could parallelize independent handlers
