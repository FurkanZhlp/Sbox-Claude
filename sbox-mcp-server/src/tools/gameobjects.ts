/**
 * Scene-building MCP tools (Phase 2, Tasks 1–6).
 * Each tool maps 1:1 to a C# handler in sbox-plugin/Handlers/.
 */
import type { Tool } from '@modelcontextprotocol/sdk/types.js';
import type { BridgeClient } from '../BridgeClient.js';

// ── Shared sub-schemas ────────────────────────────────────────────────────────

const vector3Schema = {
  type: 'object' as const,
  properties: {
    x: { type: 'number' as const },
    y: { type: 'number' as const },
    z: { type: 'number' as const },
  },
};

const rotationSchema = {
  type: 'object' as const,
  properties: {
    pitch: { type: 'number' as const, description: 'Degrees around the X axis' },
    yaw: { type: 'number' as const, description: 'Degrees around the Z axis' },
    roll: { type: 'number' as const, description: 'Degrees around the Y axis' },
  },
};

// ── Tool definitions ──────────────────────────────────────────────────────────

export const GAMEOBJECT_TOOLS: Tool[] = [
  // ── Task 1 ─────────────────────────────────────────────────────────────
  {
    name: 'create_gameobject',
    description:
      'Create a new GameObject in the active s&box scene. ' +
      'Returns the GUID of the created object. ' +
      'Optionally set its world-space position, rotation, and parent.',
    inputSchema: {
      type: 'object',
      properties: {
        name: {
          type: 'string',
          description: 'Display name for the new object. Default: "New Object".',
        },
        position: {
          ...vector3Schema,
          description: 'World-space position { x, y, z }. Default: origin.',
        },
        rotation: {
          ...rotationSchema,
          description: 'World-space rotation in degrees { pitch, yaw, roll }.',
        },
        parent: {
          type: 'string',
          description:
            'GUID of the parent GameObject. ' +
            'Omit to attach to the scene root.',
        },
      },
    },
  },

  // ── Task 2 ─────────────────────────────────────────────────────────────
  {
    name: 'delete_gameobject',
    description:
      'Permanently remove a GameObject (and all its children) from the active scene.',
    inputSchema: {
      type: 'object',
      properties: {
        guid: {
          type: 'string',
          description: 'GUID of the GameObject to delete.',
        },
      },
      required: ['guid'],
    },
  },

  // ── Task 3 ─────────────────────────────────────────────────────────────
  {
    name: 'set_transform',
    description:
      'Set the world-space transform of a GameObject. ' +
      'Position, rotation, and scale are all optional; ' +
      'supply only the fields you want to change. ' +
      'Returns the final transform values after the update.',
    inputSchema: {
      type: 'object',
      properties: {
        guid: {
          type: 'string',
          description: 'GUID of the target GameObject.',
        },
        position: {
          ...vector3Schema,
          description: 'New world-space position.',
        },
        rotation: {
          ...rotationSchema,
          description: 'New world-space rotation in degrees.',
        },
        scale: {
          ...vector3Schema,
          description: 'New world-space scale.',
        },
      },
      required: ['guid'],
    },
  },

  // ── Task 4 ─────────────────────────────────────────────────────────────
  {
    name: 'get_scene_hierarchy',
    description:
      "Return the full object tree of the active scene so Claude can 'see' what exists. " +
      'Each node includes GUID, name, enabled state, component list, and nested children. ' +
      'Call this first when starting work on a scene.',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },

  // ── Task 5 ─────────────────────────────────────────────────────────────
  {
    name: 'get_all_properties',
    description:
      'List every [Property]-annotated field on a component, including name, CLR type, ' +
      'and current value. Use this to discover what is configurable before calling ' +
      'add_component_with_properties or set_transform.',
    inputSchema: {
      type: 'object',
      properties: {
        guid: {
          type: 'string',
          description: 'GUID of the GameObject that owns the component.',
        },
        component_type: {
          type: 'string',
          description:
            'Type name of the component to inspect (e.g. "Rigidbody", "PlayerController").',
        },
      },
      required: ['guid', 'component_type'],
    },
  },

  // ── Task 6 ─────────────────────────────────────────────────────────────
  {
    name: 'add_component_with_properties',
    description:
      'Add a component to a GameObject and set its properties in one call. ' +
      'Uses TypeLibrary to resolve the component type by name. ' +
      'Property values that fail to deserialise are skipped with a warning.',
    inputSchema: {
      type: 'object',
      properties: {
        guid: {
          type: 'string',
          description: 'GUID of the target GameObject.',
        },
        component_type: {
          type: 'string',
          description:
            'Type name of the component to add (e.g. "Rigidbody", "ModelRenderer").',
        },
        properties: {
          type: 'object',
          description:
            'Key-value pairs of [Property] names and the values to assign. ' +
            'Values must be JSON-compatible with the property CLR type.',
          additionalProperties: true,
        },
      },
      required: ['guid', 'component_type'],
    },
  },
];

// ── Dispatch ──────────────────────────────────────────────────────────────────

const GAMEOBJECT_COMMAND_NAMES = new Set(GAMEOBJECT_TOOLS.map((t) => t.name));

export async function handleGameObjectTool(
  name: string,
  args: Record<string, unknown>,
  bridge: BridgeClient,
): Promise<unknown> {
  if (!GAMEOBJECT_COMMAND_NAMES.has(name)) {
    throw new Error(`Unknown gameobject tool: ${name}`);
  }
  // All gameobject tools forward directly to the matching C# handler
  return bridge.send(name, args);
}
