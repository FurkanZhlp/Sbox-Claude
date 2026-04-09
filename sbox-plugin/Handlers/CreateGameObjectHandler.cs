using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxClaude;

/// <summary>
/// Command: create_gameobject
///
/// Creates a new GameObject in the active scene.
///
/// Params:
///   name      string   Name for the object. Default: "New Object".
///   position  object   World-space position { x, y, z }. Default: origin.
///   rotation  object   World-space rotation { pitch, yaw, roll } in degrees.
///   parent    string   GUID of the parent GameObject. Default: scene root.
///
/// Returns:
///   { guid: string, name: string }
/// </summary>
public sealed class CreateGameObjectHandler : IToolHandler
{
    public string Command => "create_gameobject";

    public Task<object> ExecuteAsync(JsonElement parameters)
    {
        var scene = Scene.Active
            ?? throw new InvalidOperationException("No active scene.");

        var name = parameters.TryGetProperty("name", out var nameEl)
            ? nameEl.GetString() ?? "New Object"
            : "New Object";

        // Resolve optional parent
        GameObject? parent = null;
        if (parameters.TryGetProperty("parent", out var parentEl) &&
            Guid.TryParse(parentEl.GetString(), out var parentGuid))
        {
            parent = scene.GetAllObjects(false)
                         .FirstOrDefault(go => go.Id == parentGuid)
                  ?? throw new ArgumentException($"Parent GameObject not found: {parentGuid}");
        }

        var go = new GameObject(enabled: true, name: name);
        go.Parent = parent ?? (SceneObject)scene;

        // Apply position
        if (parameters.TryGetProperty("position", out var posEl))
            go.WorldPosition = ReadVector3(posEl, go.WorldPosition);

        // Apply rotation
        if (parameters.TryGetProperty("rotation", out var rotEl))
            go.WorldRotation = ReadRotation(rotEl);

        return Task.FromResult<object>(new
        {
            guid = go.Id.ToString(),
            name = go.Name,
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    internal static Vector3 ReadVector3(JsonElement el, Vector3 fallback = default)
    {
        return new Vector3(
            el.TryGetProperty("x", out var x) ? x.GetSingle() : fallback.x,
            el.TryGetProperty("y", out var y) ? y.GetSingle() : fallback.y,
            el.TryGetProperty("z", out var z) ? z.GetSingle() : fallback.z);
    }

    internal static Rotation ReadRotation(JsonElement el)
    {
        var pitch = el.TryGetProperty("pitch", out var p) ? p.GetSingle() : 0f;
        var yaw   = el.TryGetProperty("yaw",   out var ya) ? ya.GetSingle() : 0f;
        var roll  = el.TryGetProperty("roll",  out var r) ? r.GetSingle() : 0f;
        return Rotation.From(pitch, yaw, roll);
    }
}
