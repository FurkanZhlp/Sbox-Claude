using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxClaude;

/// <summary>
/// Command: get_scene_hierarchy
///
/// Returns the full scene object tree so Claude can "see" the scene.
/// Each node includes GUID, name, enabled state, component list, and children.
///
/// No params required.
///
/// Returns:
/// {
///   scene:   string,
///   objects: [ { guid, name, enabled, components: string[], children: [...] } ]
/// }
/// </summary>
public sealed class GetSceneHierarchyHandler : IToolHandler
{
    public string Command => "get_scene_hierarchy";

    public Task<object> ExecuteAsync(JsonElement parameters)
    {
        var scene = Scene.Active
            ?? throw new InvalidOperationException("No active scene.");

        return Task.FromResult<object>(new
        {
            scene   = scene.Name,
            objects = scene.Children.Select(BuildNode).ToList(),
        });
    }

    private static object BuildNode(GameObject go)
    {
        return new
        {
            guid       = go.Id.ToString(),
            name       = go.Name,
            enabled    = go.Enabled,
            components = go.Components.GetAll()
                           .Select(c => c.GetType().Name)
                           .ToList(),
            children   = go.Children.Select(BuildNode).ToList(),
        };
    }
}
