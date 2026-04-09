using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxBridge;

/// <summary>
/// Transfers network ownership of a GameObject to a different connection.
/// Only the host or current owner can transfer ownership.
/// </summary>
public class SetOwnershipHandler : ICommandHandler
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

		if ( go.Network?.Active != true )
			throw new Exception( "GameObject is not networked. Use network_spawn first." );

		// Transfer to specific connection or release to host
		if ( parameters.TryGetProperty( "connectionId", out var connProp ) )
		{
			var connectionId = connProp.GetString();
			if ( string.IsNullOrEmpty( connectionId ) )
			{
				// Release ownership (return to host)
				go.Network.DropOwnership();
				return Task.FromResult<object>( new
				{
					id,
					name = go.Name,
					ownershipDropped = true,
				} );
			}

			// Find connection by ID
			if ( Guid.TryParse( connectionId, out var connGuid ) )
			{
				var connection = Networking.Connections?
					.FirstOrDefault( c => c.Id == connGuid );

				if ( connection == null )
					throw new Exception( $"Connection not found: {connectionId}" );

				go.Network.AssignOwnership( connection );

				return Task.FromResult<object>( new
				{
					id,
					name = go.Name,
					newOwner = connection.DisplayName,
					newOwnerId = connectionId,
					transferred = true,
				} );
			}
		}

		// Default: take ownership for local connection
		go.Network.TakeOwnership();

		return Task.FromResult<object>( new
		{
			id,
			name = go.Name,
			ownershipTaken = true,
			isOwner = go.Network.IsOwner,
		} );
	}
}
