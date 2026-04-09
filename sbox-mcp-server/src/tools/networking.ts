import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { BridgeClient } from "../transport/bridge-client.js";

/**
 * Networking tools: add_network_helper, configure_network, get_network_status,
 * network_spawn, set_ownership, add_sync_property, add_rpc_method,
 * create_networked_player, create_lobby_manager, create_network_events.
 *
 * Manages s&box multiplayer: lobby creation, networked objects, RPCs, sync properties.
 */
export function registerNetworkingTools(
  server: McpServer,
  bridge: BridgeClient
): void {
  // ── add_network_helper ────────────────────────────────────────────
  server.tool(
    "add_network_helper",
    "Add a NetworkHelper component to the scene for quick multiplayer setup. Handles lobby creation and player prefab spawning",
    {
      id: z
        .string()
        .optional()
        .describe("GUID of existing GameObject. Creates new if omitted"),
      name: z
        .string()
        .optional()
        .describe("Name for the network manager object. Defaults to 'Network Manager'"),
      maxPlayers: z
        .number()
        .optional()
        .describe("Maximum number of players in the lobby"),
      playerPrefab: z
        .string()
        .optional()
        .describe("Path to the player prefab to spawn for each connection"),
    },
    async (params) => {
      const res = await bridge.send("add_network_helper", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );

  // ── configure_network ─────────────────────────────────────────────
  server.tool(
    "configure_network",
    "Configure networking settings on the existing NetworkHelper: max players, lobby name, player prefab, start server",
    {
      maxPlayers: z
        .number()
        .optional()
        .describe("Maximum number of players"),
      lobbyName: z
        .string()
        .optional()
        .describe("Display name for the lobby"),
      playerPrefab: z
        .string()
        .optional()
        .describe("Path to the player prefab"),
      startServer: z
        .boolean()
        .optional()
        .describe("Start the server/lobby immediately"),
    },
    async (params) => {
      const res = await bridge.send("configure_network", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );

  // ── get_network_status ────────────────────────────────────────────
  server.tool(
    "get_network_status",
    "Check the current multiplayer status: connection state, player count, lobby info, networked objects",
    {},
    async (params) => {
      const res = await bridge.send("get_network_status", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );

  // ── network_spawn ─────────────────────────────────────────────────
  server.tool(
    "network_spawn",
    "Network-enable a GameObject so it is synchronized across all connected clients. Calls NetworkSpawn()",
    {
      id: z.string().describe("GUID of the GameObject to network"),
    },
    async (params) => {
      const res = await bridge.send("network_spawn", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );

  // ── set_ownership ─────────────────────────────────────────────────
  server.tool(
    "set_ownership",
    "Transfer network ownership of a GameObject to a different connection, or take/drop ownership",
    {
      id: z.string().describe("GUID of the networked GameObject"),
      connectionId: z
        .string()
        .optional()
        .describe(
          "GUID of the target connection. Empty string = drop ownership. Omit = take ownership"
        ),
    },
    async (params) => {
      const res = await bridge.send("set_ownership", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );

  // ── add_sync_property ─────────────────────────────────────────────
  server.tool(
    "add_sync_property",
    "Add a [Sync] networked property to a C# script. The property auto-replicates across the network",
    {
      path: z
        .string()
        .describe("Relative path to the script file (e.g. 'code/Player.cs')"),
      propertyName: z.string().describe("Name for the new property"),
      propertyType: z
        .string()
        .optional()
        .describe("C# type (float, int, string, Vector3, bool, etc.). Defaults to 'float'"),
      syncFlags: z
        .string()
        .optional()
        .describe("Sync flags: 'FromHost' (host→clients only). Omit for bidirectional"),
      defaultValue: z
        .string()
        .optional()
        .describe("Default value expression (e.g. '100f', 'true', 'Vector3.Zero')"),
    },
    async (params) => {
      const res = await bridge.send("add_sync_property", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );

  // ── add_rpc_method ────────────────────────────────────────────────
  server.tool(
    "add_rpc_method",
    "Add an RPC method to a C# script. Supports [Rpc.Broadcast] (all clients), [Rpc.Host] (host only), [Rpc.Owner] (owner only)",
    {
      path: z
        .string()
        .describe("Relative path to the script file"),
      methodName: z.string().describe("Name for the RPC method"),
      rpcType: z
        .enum(["Broadcast", "Host", "Owner"])
        .optional()
        .describe("RPC type: Broadcast (all), Host (server), Owner (owning client). Defaults to Broadcast"),
      methodParams: z
        .string()
        .optional()
        .describe("Method parameters as C# signature (e.g. 'float damage, Vector3 hitPos')"),
      body: z
        .string()
        .optional()
        .describe("Method body code (without braces). Defaults to a log statement"),
    },
    async (params) => {
      const res = await bridge.send("add_rpc_method", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );

  // ── create_networked_player ───────────────────────────────────────
  server.tool(
    "create_networked_player",
    "Generate a network-aware player controller with [Sync] properties, owner-only input, and [Rpc.Broadcast] actions",
    {
      name: z
        .string()
        .optional()
        .describe("Class name. Defaults to 'NetworkedPlayer'"),
      directory: z
        .string()
        .optional()
        .describe("Subdirectory under code/"),
      moveSpeed: z.number().optional().describe("Movement speed. Defaults to 300"),
      includeHealth: z
        .boolean()
        .optional()
        .describe("Include health/damage system with host-authoritative TakeDamage. Defaults to true"),
    },
    async (params) => {
      const res = await bridge.send("create_networked_player", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );

  // ── create_lobby_manager ──────────────────────────────────────────
  server.tool(
    "create_lobby_manager",
    "Generate a lobby manager script with create/join/leave lobby, player spawning, and connection cleanup",
    {
      name: z
        .string()
        .optional()
        .describe("Class name. Defaults to 'LobbyManager'"),
      directory: z
        .string()
        .optional()
        .describe("Subdirectory under code/"),
      maxPlayers: z
        .number()
        .optional()
        .describe("Default max players. Defaults to 8"),
    },
    async (params) => {
      const res = await bridge.send("create_lobby_manager", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );

  // ── create_network_events ─────────────────────────────────────────
  server.tool(
    "create_network_events",
    "Generate a network event handler script implementing INetworkListener for connect/disconnect/chat events",
    {
      name: z
        .string()
        .optional()
        .describe("Class name. Defaults to 'NetworkEvents'"),
      directory: z
        .string()
        .optional()
        .describe("Subdirectory under code/"),
      includeChat: z
        .boolean()
        .optional()
        .describe("Include a chat message broadcast system. Defaults to false"),
    },
    async (params) => {
      const res = await bridge.send("create_network_events", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );
}
