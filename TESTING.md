# Testing Guide

This document provides a comprehensive test plan for the s&box Claude Bridge. All 78 tools across 6 phases need to be verified against a running s&box editor.

> **Note:** These tests require s&box running with the Bridge Addon loaded and Claude Code connected via the MCP server. Most tests modify the active project/scene — use a test project, not a production one.

## Prerequisites

- [ ] s&box editor installed and running
- [ ] Bridge Addon compiled and loaded (check console for `[SboxBridge] All Phase 1–6 command handlers registered`)
- [ ] MCP server connected (`get_bridge_status` returns connected)
- [ ] A test project open with at least one scene

---

## Phase 1 — Foundation (15 tools)

### Project Awareness

| # | Tool | Test Steps | Expected Result | Status |
|---|------|-----------|-----------------|--------|
| 1 | `get_project_info` | Call with no params | Returns project path, name, type, dependencies | [ ] |
| 2 | `list_project_files` | Call with `directory: ""` | Returns file tree of project root | [ ] |
| 3 | `list_project_files` | Call with `extension: ".cs"` | Returns only .cs files | [ ] |
| 4 | `read_file` | Pass path to an existing .cs file | Returns file content | [ ] |
| 5 | `read_file` | Pass path outside project (e.g. `../../etc/passwd`) | Returns error (path traversal blocked) | [ ] |
| 6 | `write_file` | Write a new .txt file in project | File created, content correct | [ ] |
| 7 | `write_file` | Write to nested directory that doesn't exist | Directory auto-created, file written | [ ] |

### Script Management

| # | Tool | Test Steps | Expected Result | Status |
|---|------|-----------|-----------------|--------|
| 8 | `create_script` | Create "TestComponent" with default params | .cs file created with Component boilerplate | [ ] |
| 9 | `create_script` | Create with `content` param (raw mode) | File written with exact raw content | [ ] |
| 10 | `edit_script` | Find/replace a string in existing script | String replaced correctly | [ ] |
| 11 | `edit_script` | Insert line at specific line number | Line inserted at correct position | [ ] |
| 12 | `delete_script` | Delete the test script created above | File removed from disk | [ ] |
| 13 | `delete_script` | Try to delete file outside project | Error (path traversal blocked) | [ ] |
| 14 | `trigger_hotload` | Call after modifying a script | s&box recompiles (check console) | [ ] |

### Console & Errors

| # | Tool | Test Steps | Expected Result | Status |
|---|------|-----------|-----------------|--------|
| 15 | `get_console_output` | Call with no filter | Returns recent log entries | [ ] |
| 16 | `get_console_output` | Call with `severity: "error"` | Returns only errors | [ ] |
| 17 | `get_compile_errors` | Introduce a syntax error, call | Returns diagnostics with file/line/message | [ ] |
| 18 | `clear_console` | Call after console has entries | Log buffer cleared, subsequent get returns empty | [ ] |

### Scene Operations

| # | Tool | Test Steps | Expected Result | Status |
|---|------|-----------|-----------------|--------|
| 19 | `list_scenes` | Call in project with scenes | Returns list of .scene files | [ ] |
| 20 | `load_scene` | Pass path to existing scene | Scene loads in editor | [ ] |
| 21 | `save_scene` | Modify scene, call save | Scene saved to disk | [ ] |
| 22 | `create_scene` | Create with `includeCamera: true, includeLight: true` | New .scene file with camera + directional light | [ ] |

---

## Phase 2 — Scene Building (15 tools)

### GameObject Lifecycle

| # | Tool | Test Steps | Expected Result | Status |
|---|------|-----------|-----------------|--------|
| 23 | `create_gameobject` | Create with name + position | Object appears in scene at position | [ ] |
| 24 | `create_gameobject` | Create with parent GUID | Object parented correctly | [ ] |
| 25 | `delete_gameobject` | Delete object by GUID | Object removed from scene | [ ] |
| 26 | `duplicate_gameobject` | Duplicate with offset | Clone created at offset position | [ ] |
| 27 | `rename_gameobject` | Change object name | Name updated in hierarchy | [ ] |
| 28 | `set_parent` | Move object to different parent | Parent changed in hierarchy | [ ] |
| 29 | `set_parent` | Set parent to null | Object moved to scene root | [ ] |
| 30 | `set_enabled` | Disable then re-enable | Object toggles visibility | [ ] |
| 31 | `set_transform` | Set position/rotation/scale | Transform updated correctly | [ ] |

### Components

| # | Tool | Test Steps | Expected Result | Status |
|---|------|-----------|-----------------|--------|
| 32 | `get_property` | Read a known property | Returns correct value | [ ] |
| 33 | `set_property` | Write a property value | Value updated on component | [ ] |
| 34 | `get_all_properties` | Call on object with components | Returns all property names + values as JSON | [ ] |
| 35 | `list_available_components` | Call with no filter | Returns component types sorted by group | [ ] |
| 36 | `add_component_with_properties` | Add ModelRenderer with model path | Component added, model assigned | [ ] |

### Hierarchy & Selection

| # | Tool | Test Steps | Expected Result | Status |
|---|------|-----------|-----------------|--------|
| 37 | `get_scene_hierarchy` | Call on scene with objects | Returns full tree with GUIDs, names, components | [ ] |
| 38 | `get_selected_objects` | Select object in editor, call | Returns selected object info | [ ] |
| 39 | `select_object` | Pass valid GUID | Object selected in editor | [ ] |
| 40 | `focus_object` | Pass valid GUID | Editor camera moves to object | [ ] |

---

## Phase 3 — Assets & Resources (12 tools)

| # | Tool | Test Steps | Expected Result | Status |
|---|------|-----------|-----------------|--------|
| 41 | `search_assets` | Search for "cube" or common model name | Returns matching assets | [ ] |
| 42 | `search_assets` | Search with `type: "model"` | Returns only models | [ ] |
| 43 | `list_asset_library` | Call with search term | Returns community packages | [ ] |
| 44 | `install_asset` | Install a small free package | Package added to project | [ ] |
| 45 | `get_asset_info` | Pass known asset path | Returns metadata (size, type, etc.) | [ ] |
| 46 | `assign_model` | Set model on a GameObject | ModelRenderer created/updated | [ ] |
| 47 | `create_material` | Create a .vmat file | Material file created with shader properties | [ ] |
| 48 | `assign_material` | Apply material to renderer | Material applied to slot | [ ] |
| 49 | `set_material_property` | Change color or roughness | Material property updated | [ ] |
| 50 | `list_sounds` | Call in project with sounds | Returns sound assets | [ ] |
| 51 | `create_sound_event` | Create .sound file | Sound event file created | [ ] |
| 52 | `assign_sound` | Attach to SoundPointComponent | Sound attached to object | [ ] |

---

## Phase 4 — Play & Test (11 tools)

| # | Tool | Test Steps | Expected Result | Status |
|---|------|-----------|-----------------|--------|
| 53 | `start_play` | Call when not playing | Play mode enters | [ ] |
| 54 | `is_playing` | Call during play mode | Returns `{ state: "playing" }` | [ ] |
| 55 | `pause_play` | Call during play mode | Game pauses | [ ] |
| 56 | `resume_play` | Call while paused | Game resumes | [ ] |
| 57 | `stop_play` | Call during play mode | Returns to editor | [ ] |
| 58 | `get_runtime_property` | Read property during play | Returns live value | [ ] |
| 59 | `set_runtime_property` | Write property during play | Value changes in running game | [ ] |
| 60 | `take_screenshot` | Call during play or editor | Returns base64 PNG (or placeholder) | [ ] |
| 61 | `undo` | Make a change, call undo | Change reverted | [ ] |
| 62 | `redo` | Undo then redo | Change re-applied | [ ] |

---

## Phase 5 — Game Logic (15 tools)

### Prefabs

| # | Tool | Test Steps | Expected Result | Status |
|---|------|-----------|-----------------|--------|
| 63 | `create_prefab` | Save a GameObject as .prefab | Prefab file created with object data | [ ] |
| 64 | `instantiate_prefab` | Spawn prefab at position | New instance appears in scene | [ ] |
| 65 | `list_prefabs` | Call after creating prefab | Lists the created prefab | [ ] |
| 66 | `get_prefab_info` | Read created prefab | Returns JSON contents | [ ] |

### Physics

| # | Tool | Test Steps | Expected Result | Status |
|---|------|-----------|-----------------|--------|
| 67 | `add_physics` | Add to object with `collider: "sphere"` | Rigidbody + SphereCollider added | [ ] |
| 68 | `add_collider` | Add BoxCollider with `isTrigger: true` | Trigger collider added | [ ] |
| 69 | `add_joint` | Add spring joint between two objects | SpringJoint created with target | [ ] |
| 70 | `raycast` | Cast ray from above ground downward | Returns hit with position/normal | [ ] |
| 71 | `raycast` | Cast ray with `all: true` | Returns multiple hits | [ ] |

### UI

| # | Tool | Test Steps | Expected Result | Status |
|---|------|-----------|-----------------|--------|
| 72 | `create_razor_ui` | Create HUD panel | .razor + .razor.scss files created | [ ] |
| 73 | `create_razor_ui` | Create with raw `content` | Custom content written | [ ] |
| 74 | `add_screen_panel` | Create screen panel object | ScreenPanel component on new object | [ ] |
| 75 | `add_world_panel` | Create world panel at position | WorldPanel at specified world position | [ ] |

### Templates

| # | Tool | Test Steps | Expected Result | Status |
|---|------|-----------|-----------------|--------|
| 76 | `create_player_controller` | Generate FPS controller | Script with CharacterController movement | [ ] |
| 77 | `create_player_controller` | Generate TPS with `type: "third_person"` | Script with third-person camera | [ ] |
| 78 | `create_npc_controller` | Generate patrol NPC | Script with NavMeshAgent patrol logic | [ ] |
| 79 | `create_npc_controller` | Generate with `behavior: "chase"` | Script with chase AI | [ ] |
| 80 | `create_game_manager` | Generate with score + timer | Script with GameState enum, score, countdown | [ ] |
| 81 | `create_trigger_zone` | Generate teleport trigger | Script with ITriggerListener + teleport | [ ] |

---

## Phase 6 — Multiplayer (10 tools)

### Networking Setup

| # | Tool | Test Steps | Expected Result | Status |
|---|------|-----------|-----------------|--------|
| 82 | `add_network_helper` | Call with no params | Creates "Network Manager" with NetworkHelper | [ ] |
| 83 | `add_network_helper` | Call with `maxPlayers: 4` | NetworkHelper configured with max 4 | [ ] |
| 84 | `configure_network` | Set lobbyName + playerPrefab | NetworkHelper updated | [ ] |
| 85 | `get_network_status` | Call before hosting | Returns `isActive: false` | [ ] |

### Networked Objects

| # | Tool | Test Steps | Expected Result | Status |
|---|------|-----------|-----------------|--------|
| 86 | `network_spawn` | Spawn a test object on network | Object network-enabled | [ ] |
| 87 | `network_spawn` | Call on already-networked object | Returns `alreadyNetworked: true` | [ ] |
| 88 | `set_ownership` | Take ownership (no connectionId) | Ownership taken by local | [ ] |
| 89 | `set_ownership` | Drop ownership (`connectionId: ""`) | Ownership released | [ ] |

### Script Helpers

| # | Tool | Test Steps | Expected Result | Status |
|---|------|-----------|-----------------|--------|
| 90 | `add_sync_property` | Add `[Sync] float Health` to script | Property inserted after class brace | [ ] |
| 91 | `add_sync_property` | Add with `syncFlags: "FromHost"` | `[Sync( SyncFlags.FromHost )]` attribute | [ ] |
| 92 | `add_rpc_method` | Add broadcast RPC method | `[Rpc.Broadcast]` method inserted | [ ] |
| 93 | `add_rpc_method` | Add host RPC with body | `[Rpc.Host]` method with custom body | [ ] |

### Multiplayer Templates

| # | Tool | Test Steps | Expected Result | Status |
|---|------|-----------|-----------------|--------|
| 94 | `create_networked_player` | Generate with health | Script with [Sync], [Rpc.Broadcast], [Rpc.Host] | [ ] |
| 95 | `create_lobby_manager` | Generate with `maxPlayers: 4` | Script with CreateLobby, OnActive, OnDisconnected | [ ] |
| 96 | `create_network_events` | Generate with `includeChat: true` | Script with INetworkListener + chat RPC | [ ] |

---

## Integration Tests

These test multi-tool workflows that simulate real user scenarios.

### Scenario 1: Build a Simple Game

```
1. create_scene → new scene with camera + light + ground
2. create_player_controller → FPS controller script
3. create_gameobject → "Player" object
4. add_component_with_properties → add CharacterController
5. add_component_with_properties → add PlayerController script
6. start_play → enter play mode
7. is_playing → verify playing
8. take_screenshot → capture viewport
9. stop_play → exit play mode
```

**Expected:** Player moves with WASD, camera rotates with mouse.

### Scenario 2: Prefab Workflow

```
1. create_gameobject → "Enemy" with position
2. add_component_with_properties → add ModelRenderer
3. assign_model → set enemy model
4. add_physics → add Rigidbody + BoxCollider
5. create_prefab → save as "prefabs/enemy.prefab"
6. instantiate_prefab → spawn 3 instances at different positions
7. list_prefabs → verify prefab listed
8. get_scene_hierarchy → verify 4 enemy objects in scene
```

### Scenario 3: Multiplayer Setup

```
1. create_networked_player → network-aware controller
2. create_lobby_manager → lobby management
3. add_network_helper → add NetworkHelper to scene
4. configure_network → set maxPlayers, playerPrefab
5. get_network_status → verify configured but not active
6. create_network_events → event handler with chat
```

### Scenario 4: UI Overlay

```
1. create_razor_ui → HUD panel with health/score
2. add_screen_panel → ScreenPanel container
3. create_game_manager → game manager with score
4. start_play → enter play mode
5. take_screenshot → verify HUD visible
```

### Scenario 5: Error Recovery

```
1. create_script → write broken C# (missing semicolon)
2. trigger_hotload → force compile
3. get_compile_errors → see the error
4. edit_script → fix the syntax error
5. trigger_hotload → recompile
6. get_compile_errors → verify clean
```

---

## Security Tests

| # | Test | Steps | Expected |
|---|------|-------|----------|
| S1 | Path traversal (read) | `read_file` with `../../etc/passwd` | Error: path must be within project |
| S2 | Path traversal (write) | `write_file` with `../../../tmp/evil` | Error: path must be within project |
| S3 | Path traversal (delete) | `delete_script` with `../../system.dll` | Error: path must be within project |
| S4 | Path traversal (load scene) | `load_scene` with `../../etc/hosts` | Error: path must be within project |
| S5 | Large input | `get_console_output` with `maxResults: 999999` | Clamped to 1000, no crash |
| S6 | Invalid GUID | `delete_gameobject` with `not-a-guid` | Error: Invalid GUID |
| S7 | Missing object | `get_property` with random valid GUID | Error: GameObject not found |

---

## Performance Tests

| # | Test | Steps | Expected |
|---|------|-------|----------|
| P1 | Large scene hierarchy | Create 100+ objects, call `get_scene_hierarchy` | Returns within 5s, complete tree |
| P2 | Rapid sequential calls | Call `get_project_info` 20 times quickly | All return successfully, no crashes |
| P3 | Large file write | `write_file` with 100KB content | File written correctly |
| P4 | Concurrent tool calls | Call 5 different tools via batch | All respond within 30s timeout |

---

## Bridge Connection Tests

| # | Test | Steps | Expected |
|---|------|-------|----------|
| B1 | Status check | `get_bridge_status` | Returns connected, latency > 0 |
| B2 | Reconnection | Restart s&box, retry tool | Reconnects automatically |
| B3 | Timeout | Call tool while s&box is frozen | Returns timeout error after 30s |
| B4 | Ping keepalive | Leave idle for 60s, then call tool | Still connected (ping keeps alive) |

---

## Test Execution Notes

- Tests marked `[ ]` are pending. Mark `[x]` when passing.
- Some s&box APIs have `API-NOTE` comments in handlers — these may need adjustment for your specific SDK version.
- The `take_screenshot` tool uses a placeholder until camera render API is verified.
- Networking tests (Phase 6) require either a running lobby or may only verify setup/code generation.
- Run security tests in an isolated environment to prevent accidental file modifications.
