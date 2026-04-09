using System;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxBridge;

/// <summary>
/// Network-enables a GameObject by calling NetworkSpawn().
/// This makes the object visible and synchronized across all connected clients.
/// The object must have networking-compatible components.
/// </summary>
public class NetworkSpawnHandler : ICommandHandler
{
	public Task<object> Execute( JsonElement parameters )
	{
		var scene = Game.ActiveScene;
		if ( scene == null )
			throw new Exception( "No active scene" );

		var id = parameters.GetProperty( "id" ).GetString()
			?? throw new Exception( "Missing required parameter: id" );

		if ( !Guid.TryParse( id, out var guid ) )
			throw new Exception( $"Invalid GUID: {id}" );

		var go = scene.Directory.FindByGuid( guid );
		if ( go == null )
			throw new Exception( $"GameObject not found: {id}" );

		// Check if already networked
		if ( go.Network?.Active == true )
		{
			return Task.FromResult<object>( new
			{
				id,
				name = go.Name,
				alreadyNetworked = true,
				isOwner = go.Network.IsOwner,
			} );
		}

		// Network spawn the object
		// API-NOTE: NetworkSpawn() vs Network.Spawn() may vary by SDK
		go.NetworkSpawn();

		return Task.FromResult<object>( new
		{
			id,
			name = go.Name,
			networked = true,
			isOwner = go.Network?.IsOwner ?? false,
		} );
	}
}
