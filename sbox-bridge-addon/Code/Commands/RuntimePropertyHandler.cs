using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxBridge;

/// <summary>
/// Reads a component property value during play mode (runtime).
/// Throws if not in play mode.
/// </summary>
public class GetRuntimePropertyHandler : ICommandHandler
{
	public Task<object> Execute( JsonElement parameters )
	{
		AssertPlayMode();

		var scene = Game.ActiveScene;
		if ( scene == null )
			throw new Exception( "No active scene" );

		var id = parameters.GetProperty( "id" ).GetString()
			?? throw new Exception( "Missing required parameter: id" );
		var componentType = parameters.GetProperty( "component" ).GetString()
			?? throw new Exception( "Missing required parameter: component" );
		var propertyName = parameters.GetProperty( "property" ).GetString()
			?? throw new Exception( "Missing required parameter: property" );

		if ( !Guid.TryParse( id, out var guid ) )
			throw new Exception( $"Invalid GUID: {id}" );

		var go = scene.Directory.FindByGuid( guid );
		if ( go == null )
			throw new Exception( $"GameObject not found at runtime: {id}" );

		var component = go.Components
			.FirstOrDefault( c => c.GetType().Name.Equals( componentType, StringComparison.OrdinalIgnoreCase ) );

		if ( component == null )
			throw new Exception( $"Component '{componentType}' not found on '{go.Name}'" );

		var typeDesc = TypeLibrary.GetType( component.GetType() );
		var prop = typeDesc?.Properties.FirstOrDefault( p => p.Name.Equals( propertyName, StringComparison.OrdinalIgnoreCase ) );
		if ( prop == null )
			throw new Exception( $"Property '{propertyName}' not found on '{componentType}'" );

		var value = prop.GetValue( component );

		return Task.FromResult<object>( new
		{
			id,
			component = componentType,
			property = prop.Name,
			type = prop.PropertyType.Name,
			value = ComponentHelper.SerializeValue( value ),
			runtime = true,
		} );
	}

	private static void AssertPlayMode()
	{
		if ( !EditorScene.IsPlaying )
			throw new Exception( "Not in play mode. Call start_play first to read runtime properties." );
	}
}

/// <summary>
/// Sets a component property value during play mode (runtime).
/// Allows live-tweaking values while the game runs.
/// </summary>
public class SetRuntimePropertyHandler : ICommandHandler
{
	public Task<object> Execute( JsonElement parameters )
	{
		AssertPlayMode();

		var scene = Game.ActiveScene;
		if ( scene == null )
			throw new Exception( "No active scene" );

		var id = parameters.GetProperty( "id" ).GetString()
			?? throw new Exception( "Missing required parameter: id" );
		var componentType = parameters.GetProperty( "component" ).GetString()
			?? throw new Exception( "Missing required parameter: component" );
		var propertyName = parameters.GetProperty( "property" ).GetString()
			?? throw new Exception( "Missing required parameter: property" );

		if ( !parameters.TryGetProperty( "value", out var valueProp ) )
			throw new Exception( "Missing required parameter: value" );

		if ( !Guid.TryParse( id, out var guid ) )
			throw new Exception( $"Invalid GUID: {id}" );

		var go = scene.Directory.FindByGuid( guid );
		if ( go == null )
			throw new Exception( $"GameObject not found at runtime: {id}" );

		var component = go.Components
			.FirstOrDefault( c => c.GetType().Name.Equals( componentType, StringComparison.OrdinalIgnoreCase ) );

		if ( component == null )
			throw new Exception( $"Component '{componentType}' not found on '{go.Name}'" );

		var typeDesc = TypeLibrary.GetType( component.GetType() );
		var prop = typeDesc?.Properties.FirstOrDefault( p => p.Name.Equals( propertyName, StringComparison.OrdinalIgnoreCase ) );
		if ( prop == null )
			throw new Exception( $"Property '{propertyName}' not found on '{componentType}'" );

		var value = ComponentHelper.DeserializeValue( valueProp, prop.PropertyType );
		prop.SetValue( component, value );

		var readBack = prop.GetValue( component );

		return Task.FromResult<object>( new
		{
			id,
			component = componentType,
			property = prop.Name,
			type = prop.PropertyType.Name,
			value = ComponentHelper.SerializeValue( readBack ),
			runtime = true,
			set = true,
		} );
	}

	private static void AssertPlayMode()
	{
		if ( !EditorScene.IsPlaying )
			throw new Exception( "Not in play mode. Call start_play first to set runtime properties." );
	}
}
