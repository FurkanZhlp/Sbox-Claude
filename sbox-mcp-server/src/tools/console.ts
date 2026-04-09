import type { Tool } from '@modelcontextprotocol/sdk/types.js';
import type { BridgeClient } from '../BridgeClient.js';

export const CONSOLE_TOOLS: Tool[] = [
  {
    name: 'get_console_output',
    description:
      'Retrieve recent log messages from the s&box editor console. ' +
      'Use this to check for runtime errors, warnings, or debug output from game code. ' +
      'Supports filtering by severity and substring.',
    inputSchema: {
      type: 'object',
      properties: {
        severity: {
          type: 'string',
          enum: ['trace', 'debug', 'info', 'warning', 'error'],
          description:
            'Minimum severity level to include. ' +
            '"info" (default) omits trace/debug noise. Use "error" to see only errors.',
        },
        limit: {
          type: 'number',
          description: 'Maximum number of log entries to return. Default: 100.',
        },
        filter: {
          type: 'string',
          description:
            'Optional substring filter applied to message text (case-insensitive).',
        },
      },
    },
  },
];

export async function handleConsoleTool(
  name: string,
  args: Record<string, unknown>,
  bridge: BridgeClient,
): Promise<unknown> {
  switch (name) {
    case 'get_console_output':
      return bridge.send('get_console_output', args);
    default:
      throw new Error(`Unknown console tool: ${name}`);
  }
}
