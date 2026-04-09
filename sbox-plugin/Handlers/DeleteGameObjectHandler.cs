using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxClaude;

/// <summary>
/// Command: delete_gameobject
///
/// Removes a GameObject from the scene by its GUID.
///
/// Params:
///   guid  string  GUID of the GameObject to destroy. Required.
///
/// Returns:
///   { success: bool }  or throws on invalid input.
/// </summary>
public sealed class DeleteGameObjectHandler : IToolHandler
{
    public string Command => "delete_gameobject";

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

        go.Destroy();

        return Task.FromResult<object>(new { success = true });
    }
}
