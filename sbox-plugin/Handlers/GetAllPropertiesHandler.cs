using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxClaude;

/// <summary>
/// Command: get_all_properties
///
/// Reflects over all [Property]-annotated fields on a component and returns
/// their names, types, and current values. Lets Claude understand what is
/// configurable on any component without hard-coded knowledge.
///
/// Params:
///   guid            string  GUID of the target GameObject. Required.
///   component_type  string  Type name of the component (e.g. "Rigidbody"). Required.
///
/// Returns:
/// {
///   guid:       string,
///   component:  string,
///   properties: [ { name, type, value } ]
/// }
/// </summary>
public sealed class GetAllPropertiesHandler : IToolHandler
{
    public string Command => "get_all_properties";

    public Task<object> ExecuteAsync(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("guid", out var guidEl) ||
            !Guid.TryParse(guidEl.GetString(), out var guid))
        {
            throw new ArgumentException("Parameter 'guid' is missing or not a valid GUID.");
        }

        if (!parameters.TryGetProperty("component_type", out var typeEl) ||
            string.IsNullOrWhiteSpace(typeEl.GetString()))
        {
            throw new ArgumentException("Parameter 'component_type' is required.");
        }

        var typeName = typeEl.GetString()!;

        var scene = Scene.Active
            ?? throw new InvalidOperationException("No active scene.");

        var go = scene.GetAllObjects(false).FirstOrDefault(o => o.Id == guid)
            ?? throw new ArgumentException($"GameObject not found: {guid}");

        var component = go.Components.GetAll()
                          .FirstOrDefault(c => c.GetType().Name == typeName)
            ?? throw new ArgumentException(
                $"Component '{typeName}' not found on GameObject '{go.Name}'.");

        // Use TypeLibrary for s&box-aware reflection over [Property]-annotated members
        var typeDesc = TypeLibrary.GetType(component.GetType())
            ?? throw new InvalidOperationException(
                $"TypeLibrary has no entry for '{typeName}'.");

        var props = typeDesc.Properties
            .Where(p => p.HasAttribute<PropertyAttribute>())
            .Select(p => new
            {
                name  = p.Name,
                type  = p.PropertyType?.Name ?? "unknown",
                value = SafeGetValue(p, component),
            })
            .ToList();

        return Task.FromResult<object>(new
        {
            guid       = go.Id.ToString(),
            component  = typeName,
            properties = props,
        });
    }

    private static object? SafeGetValue(Sandbox.PropertyDescription pd, object instance)
    {
        try   { return pd.GetValue(instance); }
        catch { return null; }
    }
}
