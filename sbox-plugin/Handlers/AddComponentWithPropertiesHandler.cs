using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxClaude;

/// <summary>
/// Command: add_component_with_properties
///
/// Adds a component to a GameObject and immediately sets any supplied properties,
/// all in one round-trip.
///
/// Params:
///   guid            string  GUID of the target GameObject. Required.
///   component_type  string  Type name of the component to add (e.g. "Rigidbody"). Required.
///   properties      object  Key-value map of property names → values to set. Optional.
///
/// Returns:
///   { guid, component, success: true }
///
/// Property values in the JSON are deserialised using the target property's
/// declared type. If a value cannot be set, a warning is logged but the call
/// still succeeds for the remaining properties.
/// </summary>
public sealed class AddComponentWithPropertiesHandler : IToolHandler
{
    public string Command => "add_component_with_properties";

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

        var typeDesc = TypeLibrary.GetType(typeName)
            ?? throw new ArgumentException($"TypeLibrary has no entry for '{typeName}'. " +
               "Check spelling and ensure the assembly is loaded.");

        // Create the component
        var component = go.Components.Create(typeDesc);

        // Apply property values if supplied
        if (parameters.TryGetProperty("properties", out var propsEl) &&
            propsEl.ValueKind == JsonValueKind.Object)
        {
            var componentTypeDesc = TypeLibrary.GetType(component.GetType());

            foreach (var kv in propsEl.EnumerateObject())
            {
                var propDesc = componentTypeDesc?.Properties
                    .FirstOrDefault(p => p.Name == kv.Name);

                if (propDesc is null)
                {
                    Log.Warning($"[Claude Bridge] add_component: no property '{kv.Name}' on '{typeName}'");
                    continue;
                }

                try
                {
                    var clrType = propDesc.PropertyType;
                    if (clrType is null) continue;

                    // Deserialise the JSON value to the target CLR type
                    var value = JsonSerializer.Deserialize(
                        kv.Value.GetRawText(),
                        clrType,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    propDesc.SetValue(component, value);
                }
                catch (Exception ex)
                {
                    Log.Warning($"[Claude Bridge] add_component: failed to set '{kv.Name}': {ex.Message}");
                }
            }
        }

        return Task.FromResult<object>(new
        {
            guid      = go.Id.ToString(),
            component = component.GetType().Name,
            success   = true,
        });
    }
}
