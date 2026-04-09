# Contributing to sbox-claude

## Dev Environment Setup

### Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| Node.js | 20+ | For the MCP server |
| npm | 9+ | Bundled with Node |
| .NET / s&box SDK | latest | Only needed to compile the C# plugin |
| s&box editor | latest | Only needed for end-to-end testing |

### Initial setup

```bash
git clone https://github.com/lousputthole/sbox-claude.git
cd sbox-claude

# MCP server
cd sbox-mcp-server
npm install
npm run build
```

The C# plugin (`sbox-plugin/`) is compiled by the s&box editor automatically when
you drop the folder into your s&box addons directory. You don't need a separate
`dotnet build` step.

---

## How to Add a New Tool

Full walkthrough is in [CLAUDE.md](./CLAUDE.md#how-to-add-a-new-tool). Short version:

1. **C# handler** — Create `sbox-plugin/Handlers/MyToolHandler.cs` implementing
   `IToolHandler`. The `Command` property is the string key Claude will call.

2. **Register** — Add `new MyToolHandler()` to the array in
   `BridgeServer.cs:RegisterHandlers()`.

3. **TypeScript tool definition** — Add a `Tool` object and a handler function in
   `sbox-mcp-server/src/tools/*.ts`.

4. **Wire into index.ts** — Add the tool to `allTools` and add a dispatch case.

5. **Test** — Write a test in `sbox-mcp-server/tests/` using the mock WebSocket
   server pattern from `BridgeClient.test.ts`.

---

## Testing

### Unit tests (no s&box required)

```bash
cd sbox-mcp-server
npm test
```

Tests use a real `WebSocketServer` on a random port so no mocking library is needed
for the network layer. The s&box game engine is never involved.

### Manual end-to-end

1. Start s&box editor with the plugin installed — watch for `[Claude Bridge] Listening on port 8765` in the console.
2. `npm start` in `sbox-mcp-server/`.
3. Use Claude Code or call the MCP server directly with a JSON-RPC client.

### Testing a handler without s&box

Create a lightweight test that starts a `WebSocketServer`, has it reply with canned
JSON, and asserts the MCP tool returns the expected result. See
`tests/BridgeClient.test.ts` for the pattern.

---

## Linting and Formatting

```bash
cd sbox-mcp-server
npm run lint        # ESLint (TypeScript rules)
npm run format      # Prettier
npm run lint -- --fix   # Auto-fix ESLint issues
```

Configuration lives in `.eslintrc.json` and `.prettierrc`. CI will fail on lint
errors, so run these before pushing.

---

## PR Conventions

- **Branch names**: `feat/<short-slug>`, `fix/<short-slug>`, `chore/<short-slug>`
- **Commit messages**: imperative present tense — *"Add set_transform handler"* not
  *"Added"* or *"Adding"*
- **PR description**: include what the tool does, what s&box API it calls, and how
  you tested it
- **One tool per PR** is preferred; infrastructure changes can be batched
- All CI checks must pass before merge

---

## Phase Roadmap

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | ✅ Complete | Infrastructure — BridgeServer, BridgeClient, get_console_output |
| 2 | 🚧 In progress | Scene building — create/delete/transform/hierarchy/components |
| 3 | 📋 Planned | Asset management — find/load materials, models, prefabs |
| 4 | 📋 Planned | Code generation — create/edit C# files, hot-reload |
| 5 | 📋 Planned | Play-mode control — start/stop, query runtime state |

Tasks available to claim are listed in the GitHub issues. Each issue is tagged with
the phase it belongs to and an estimated size (S / M / L).

---

## Claimed Tasks

If you are working on a task from the issue tracker, comment on the issue to claim
it so two people don't duplicate work. Unclaim it (or ask for a review) within two
weeks or the issue will be re-opened.
