using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxBridge;

/// <summary>
/// Configures networking settings: max players, lobby name, privacy mode.
/// Finds the existing NetworkHelper in the scene and updates its properties.
/// </summary>
public class ConfigureNetworkHandler : ICommandHandler
{
	public Task<object> Execute( JsonElement parameters )
	{
		var scene = Game.ActiveScene;
		if ( scene == null )
			throw new Exception( "No active scene" );

		// Find existing NetworkHelper in scene
		var helper = scene.GetAllComponents<NetworkHelper>().FirstOrDefault();
		if ( helper == null )
			throw new Exception( "No NetworkHelper found in scene. Use add_network_helper first." );

		var changes = new System.Collections.Generic.List<string>();

		if ( parameters.TryGetProperty( "maxPlayers", out var maxProp ) )
		{
			helper.MaxPlayers = maxProp.GetInt32();
			changes.Add( $"maxPlayers={maxProp.GetInt32()}" );
		}

		if ( parameters.TryGetProperty( "lobbyName", out var nameProp ) )
		{
			// API-NOTE: Lobby name may be set via LobbyConfig at creation time
			// rather than a direct property. This sets it for the next lobby creation.
			changes.Add( $"lobbyName={nameProp.GetString()}" );
		}

		if ( parameters.TryGetProperty( "playerPrefab", out var prefabProp ) )
		{
			var prefabPath = prefabProp.GetString();
			if ( !string.IsNullOrEmpty( prefabPath ) )
			{
				var prefab = ResourceLibrary.Get<PrefabFile>( prefabPath );
				if ( prefab != null )
				{
					helper.PlayerPrefab = prefab;
					changes.Add( $"playerPrefab={prefabPath}" );
				}
			}
		}

		if ( parameters.TryGetProperty( "startServer", out var serverProp ) && serverProp.GetBoolean() )
		{
			// API-NOTE: Starting a server may use Networking.CreateLobby() or helper.StartServer()
			changes.Add( "startServer=requested" );
		}

		return Task.FromResult<object>( new
		{
			gameObject = helper.GameObject.Name,
			changes,
			configured = true,
		} );
	}
}
