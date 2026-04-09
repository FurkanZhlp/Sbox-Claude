using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxClaude;

/// <summary>
/// Command: set_transform
///
/// Updates the world-space transform of a GameObject.
/// Any combination of position / rotation / scale can be set in one call;
/// omitted fields are left unchanged.
///
/// Params:
///   guid      string  GUID of the target GameObject. Required.
///   position  object  { x, y, z } world-space position.
///   rotation  object  { pitch, yaw, roll } in degrees.
///   scale     object  { x, y, z } world-space scale.
///
/// Returns the final transform values after applying the update.
/// </summary>
public sealed class SetTransformHandler : IToolHandler
{
    public string Command => "set_transform";

    public Task<object> ExecuteAsync(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("guid", out var guidEl) ||
            !Guid.TryParse(guidEl.GetString(), out var guid))
        {
            throw new ArgumentException("Parameter 'guid' is missing or not a valid GUID.");
        }

        var scene = Scene.Active
            ?? throw new InvalidOperationException("No active scene.");

        var go = scene.GetAllObjects(false).FirstOrDefault(o => o.Id == guid)
            ?? throw new ArgumentException($"GameObject not found: {guid}");

        if (parameters.TryGetProperty("position", out var posEl))
            go.WorldPosition = CreateGameObjectHandler.ReadVector3(posEl, go.WorldPosition);

        if (parameters.TryGetProperty("rotation", out var rotEl))
        {
            // Use existing angles as fallback so you can set only one axis
            var a = go.WorldRotation.Angles();
            var pitch = rotEl.TryGetProperty("pitch", out var p) ? p.GetSingle() : a.pitch;
            var yaw   = rotEl.TryGetProperty("yaw",   out var y) ? y.GetSingle() : a.yaw;
            var roll  = rotEl.TryGetProperty("roll",  out var r) ? r.GetSingle() : a.roll;
            go.WorldRotation = Rotation.From(pitch, yaw, roll);
        }

        if (parameters.TryGetProperty("scale", out var scaleEl))
            go.WorldScale = CreateGameObjectHandler.ReadVector3(scaleEl, go.WorldScale);

        var angles = go.WorldRotation.Angles();

        return Task.FromResult<object>(new
        {
            guid     = go.Id.ToString(),
            position = new { x = go.WorldPosition.x, y = go.WorldPosition.y, z = go.WorldPosition.z },
            rotation = new { pitch = angles.pitch, yaw = angles.yaw, roll = angles.roll },
            scale    = new { x = go.WorldScale.x, y = go.WorldScale.y, z = go.WorldScale.z },
        });
    }
}
