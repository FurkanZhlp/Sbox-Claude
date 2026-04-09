/**
 * Bridge health / status MCP tool (Task 13).
 *
 * get_bridge_status — returns connection state, host/port,
 * round-trip latency, and the s&box editor version.
 */
import type { Tool } from '@modelcontextprotocol/sdk/types.js';
import type { BridgeClient } from '../BridgeClient.js';

export const STATUS_TOOLS: Tool[] = [
  {
    name: 'get_bridge_status',
    description:
      'Check the health of the connection between this MCP server and the s&box editor. ' +
      'Returns whether the WebSocket is connected, the configured host/port, ' +
      'round-trip latency (measured with a ping), and the s&box editor version. ' +
      "Use this first when debugging 'is s&box even running?' problems.",
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
];

export async function handleStatusTool(
  name: string,
  _args: Record<string, unknown>,
  bridge: BridgeClient,
): Promise<unknown> {
  switch (name) {
    case 'get_bridge_status': {
      const base = {
        connected: bridge.connected,
        host: bridge.host,
        port: bridge.port,
        url: bridge.url,
      };

      if (!bridge.connected) {
        return { ...base, latency_ms: null, sbox_version: null };
      }

      const start = Date.now();
      try {
        const pong = await bridge.send('ping', {});
        const latency = Date.now() - start;
        const version =
          (pong as Record<string, unknown> | null)?.['version'] ?? 'unknown';
        return { ...base, latency_ms: latency, sbox_version: version };
      } catch (err) {
        return {
          ...base,
          latency_ms: null,
          sbox_version: null,
          ping_error: err instanceof Error ? err.message : String(err),
        };
      }
    }

    default:
      throw new Error(`Unknown status tool: ${name}`);
  }
}
