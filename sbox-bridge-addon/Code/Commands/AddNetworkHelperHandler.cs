using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxBridge;

/// <summary>
/// Adds a NetworkHelper component to a GameObject for quick multiplayer setup.
/// NetworkHelper handles lobby creation, player spawning, and connection management.
/// </summary>
public class AddNetworkHelperHandler : ICommandHandler
{
	public Task<object> Execute( JsonElement parameters )
	{
		var scene = Game.ActiveScene;
		if ( scene == null )
			throw new Exception( "No active scene" );

		// Find or create the target GameObject
		GameObject go;
		if ( parameters.TryGetProperty( "id", out var idProp ) )
		{
			var idStr = idProp.GetString();
			if ( !string.IsNullOrEmpty( idStr ) && Guid.TryParse( idStr, out var guid ) )
			{
				go = scene.Directory.FindByGuid( guid )
					?? throw new Exception( $"GameObject not found: {idStr}" );
			}
			else
			{
				go = scene.CreateObject();
				go.Name = "Network Manager";
			}
		}
		else
		{
			go = scene.CreateObject();
			go.Name = parameters.TryGetProperty( "name", out var nameProp )
				? nameProp.GetString() ?? "Network Manager"
				: "Network Manager";
		}

		// Add NetworkHelper if not already present
		var helper = go.Components.Get<NetworkHelper>();
		var added = false;

		if ( helper == null )
		{
			helper = go.Components.Create<NetworkHelper>();
			added = true;
		}

		// Configure properties
		if ( parameters.TryGetProperty( "maxPlayers", out var maxProp ) )
		{
			// API-NOTE: NetworkHelper.MaxPlayers may need adjustment per SDK version
			helper.MaxPlayers = maxProp.GetInt32();
		}

		// Set player prefab if provided
		if ( parameters.TryGetProperty( "playerPrefab", out var prefabProp ) )
		{
			var prefabPath = prefabProp.GetString();
			if ( !string.IsNullOrEmpty( prefabPath ) )
			{
				var prefab = ResourceLibrary.Get<PrefabFile>( prefabPath );
				if ( prefab != null )
				{
					helper.PlayerPrefab = prefab;
				}
			}
		}

		return Task.FromResult<object>( new
		{
			id = go.Id.ToString(),
			gameObject = go.Name,
			networkHelperAdded = added,
			configured = true,
		} );
	}
}
