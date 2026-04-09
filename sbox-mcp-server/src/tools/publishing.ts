import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { BridgeClient } from "../transport/bridge-client.js";

/**
 * Publishing tools: get_project_config, set_project_config, validate_project,
 * build_project, get_build_status, clean_build, export_project,
 * set_project_thumbnail, get_package_details, prepare_publish.
 *
 * Manages project configuration, building, exporting, and publishing preparation.
 */
export function registerPublishingTools(
  server: McpServer,
  bridge: BridgeClient
): void {
  // ── get_project_config ───────────────────────────────────────────
  server.tool(
    "get_project_config",
    "Read the full project configuration from the .sbproj file including title, description, version, type, package references, metadata, and raw JSON",
    {},
    async (params) => {
      const res = await bridge.send("get_project_config", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );

  // ── set_project_config ───────────────────────────────────────────
  server.tool(
    "set_project_config",
    "Update project configuration fields for publishing: title, description, version, type, package ident, summary, visibility. Only provided fields are changed",
    {
      title: z.string().optional().describe("Project display title"),
      description: z
        .string()
        .optional()
        .describe("Project description for publishing"),
      version: z
        .string()
        .optional()
        .describe("Version string (e.g. '1.0.0', '2.1.3')"),
      type: z
        .string()
        .optional()
        .describe(
          "Project type: 'game', 'addon', 'library', or 'template'"
        ),
      packageIdent: z
        .string()
        .optional()
        .describe("Package identifier (e.g. 'myorg.mygame')"),
      summary: z
        .string()
        .optional()
        .describe("Short summary for asset.party listing"),
      isPublic: z
        .boolean()
        .optional()
        .describe("Whether the project is publicly visible"),
    },
    async (params) => {
      const res = await bridge.send("set_project_config", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );

  // ── validate_project ─────────────────────────────────────────────
  server.tool(
    "validate_project",
    "Validate that the project is ready for publishing. Checks: compile errors, metadata completeness, scenes, scripts, thumbnail, and project type",
    {},
    async (params) => {
      const res = await bridge.send("validate_project", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );

  // ── build_project ────────────────────────────────────────────────
  server.tool(
    "build_project",
    "Trigger a full project build/recompilation. Returns build result with error and warning counts",
    {
      configuration: z
        .enum(["Debug", "Release"])
        .optional()
        .describe("Build configuration. Defaults to 'Release'"),
    },
    async (params) => {
      const res = await bridge.send("build_project", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );

  // ── get_build_status ─────────────────────────────────────────────
  server.tool(
    "get_build_status",
    "Get the current build/compilation status: is compiling, errors, warnings, and full diagnostics list",
    {},
    async (params) => {
      const res = await bridge.send("get_build_status", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );

  // ── clean_build ──────────────────────────────────────────────────
  server.tool(
    "clean_build",
    "Clean all compiled output (bin, obj) and trigger a fresh rebuild from scratch",
    {},
    async (params) => {
      const res = await bridge.send("clean_build", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );

  // ── export_project ───────────────────────────────────────────────
  server.tool(
    "export_project",
    "Export the project as a standalone game. Copies assemblies, assets, and scenes to an output directory for distribution",
    {
      outputPath: z
        .string()
        .optional()
        .describe(
          "Relative output directory within the project. Defaults to 'export'"
        ),
      configuration: z
        .enum(["Debug", "Release"])
        .optional()
        .describe("Build configuration for export. Defaults to 'Release'"),
    },
    async (params) => {
      const res = await bridge.send("export_project", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );

  // ── set_project_thumbnail ────────────────────────────────────────
  server.tool(
    "set_project_thumbnail",
    "Set or update the project thumbnail image (thumb.png) used for publishing. Provide either a source path or base64 image data",
    {
      sourcePath: z
        .string()
        .optional()
        .describe(
          "Relative path to an image file within the project to use as thumbnail"
        ),
      base64: z
        .string()
        .optional()
        .describe("Base64-encoded image data to write as thumbnail"),
      format: z
        .enum(["png", "jpg"])
        .optional()
        .describe("Image format when using base64 mode. Defaults to 'png'"),
    },
    async (params) => {
      const res = await bridge.send("set_project_thumbnail", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );

  // ── get_package_details ──────────────────────────────────────────
  server.tool(
    "get_package_details",
    "Fetch detailed package information from the s&box asset library (asset.party) including title, author, version, downloads, ratings, and dependencies",
    {
      ident: z
        .string()
        .describe(
          "Package identifier (e.g. 'facepunch.flatgrass', 'myorg.mygame')"
        ),
    },
    async (params) => {
      const res = await bridge.send("get_package_details", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );

  // ── prepare_publish ──────────────────────────────────────────────
  server.tool(
    "prepare_publish",
    "Comprehensive publish preparation: validates project, compiles, checks metadata, and generates a detailed readiness report with issues, warnings, and next steps",
    {},
    async (params) => {
      const res = await bridge.send("prepare_publish", params);
      if (!res.success) {
        return { content: [{ type: "text", text: `Error: ${res.error}` }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify(res.data, null, 2) }],
      };
    }
  );
}
