using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxBridge;

/// <summary>
/// Returns the current networking status: connection state, player count,
/// lobby info, and whether a NetworkHelper is configured.
/// </summary>
public class GetNetworkStatusHandler : ICommandHandler
{
	public Task<object> Execute( JsonElement parameters )
	{
		var scene = Game.ActiveScene;
		if ( scene == null )
			throw new Exception( "No active scene" );

		// Check for NetworkHelper
		var helper = scene.GetAllComponents<NetworkHelper>().FirstOrDefault();

		// Get connection info
		// API-NOTE: Connection/Networking API may vary by SDK version
		var isConnected = Networking.IsActive;
		var isHost = Networking.IsHost;

		// Count networked objects
		var networkedObjects = scene.GetAllObjects( true )
			.Where( go => go.Network?.Active == true )
			.Count();

		// Get connected players
		var connections = Networking.Connections?.ToList()
			?? new System.Collections.Generic.List<Connection>();

		var players = connections.Select( c => new
		{
			displayName = c.DisplayName,
			id = c.Id.ToString(),
			isHost = c.IsHost,
		} ).ToList();

		return Task.FromResult<object>( new
		{
			isActive = isConnected,
			isHost,
			playerCount = connections.Count,
			players,
			networkedObjectCount = networkedObjects,
			hasNetworkHelper = helper != null,
			maxPlayers = helper?.MaxPlayers ?? 0,
			hasPlayerPrefab = helper?.PlayerPrefab != null,
		} );
	}
}
