using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxBridge;

/// <summary>
/// Generates a lobby manager script that handles creating, joining,
/// and managing multiplayer lobbies using s&box's Networking API.
/// </summary>
public class CreateLobbyManagerHandler : ICommandHandler
{
	public Task<object> Execute( JsonElement parameters )
	{
		var projectRoot = Project.Current?.GetRootPath();
		if ( string.IsNullOrEmpty( projectRoot ) )
			throw new Exception( "No project is currently open" );

		if ( !projectRoot.EndsWith( Path.DirectorySeparatorChar ) )
			projectRoot += Path.DirectorySeparatorChar;

		var name = parameters.TryGetProperty( "name", out var nameProp )
			? nameProp.GetString() ?? "LobbyManager" : "LobbyManager";

		var directory = parameters.TryGetProperty( "directory", out var dirProp )
			? dirProp.GetString() ?? "" : "";

		var maxPlayers = parameters.TryGetProperty( "maxPlayers", out var maxProp )
			? maxProp.GetInt32() : 8;

		var sb = new StringBuilder();
		sb.AppendLine( "using Sandbox;" );
		sb.AppendLine( "using System;" );
		sb.AppendLine( "using System.Threading.Tasks;" );
		sb.AppendLine();
		sb.AppendLine( "/// <summary>" );
		sb.AppendLine( "/// Manages multiplayer lobbies: creation, joining, and player tracking." );
		sb.AppendLine( "/// Uses s&box's Networking.CreateLobby API." );
		sb.AppendLine( "/// </summary>" );
		sb.AppendLine( $"public sealed class {name} : Component, Component.INetworkListener" );
		sb.AppendLine( "{" );
		sb.AppendLine( $"\t[Property] public int MaxPlayers {{ get; set; }} = {maxPlayers};" );
		sb.AppendLine( "\t[Property] public string LobbyName { get; set; } = \"My Game\";" );
		sb.AppendLine( "\t[Property] public GameObject PlayerPrefab { get; set; }" );
		sb.AppendLine();
		sb.AppendLine( "\tpublic bool IsInLobby => Networking.IsActive;" );
		sb.AppendLine( "\tpublic bool IsHost => Networking.IsHost;" );
		sb.AppendLine( "\tpublic int PlayerCount => Networking.Connections?.Count() ?? 0;" );
		sb.AppendLine();
		sb.AppendLine( "\t/// <summary>Create a new lobby and become the host.</summary>" );
		sb.AppendLine( "\tpublic async Task CreateLobby()" );
		sb.AppendLine( "\t{" );
		sb.AppendLine( "\t\tif ( Networking.IsActive )" );
		sb.AppendLine( "\t\t{" );
		sb.AppendLine( "\t\t\tLog.Warning( \"Already in a lobby\" );" );
		sb.AppendLine( "\t\t\treturn;" );
		sb.AppendLine( "\t\t}" );
		sb.AppendLine();
		sb.AppendLine( "\t\tawait Networking.CreateLobby( new LobbyConfig" );
		sb.AppendLine( "\t\t{" );
		sb.AppendLine( "\t\t\tMaxPlayers = MaxPlayers," );
		sb.AppendLine( "\t\t\tName = LobbyName," );
		sb.AppendLine( "\t\t\tPrivacy = LobbyPrivacy.Public," );
		sb.AppendLine( "\t\t} );" );
		sb.AppendLine();
		sb.AppendLine( "\t\tLog.Info( $\"Lobby created: {LobbyName} (max {MaxPlayers} players)\" );" );
		sb.AppendLine( "\t}" );
		sb.AppendLine();
		sb.AppendLine( "\t/// <summary>Disconnect from the current lobby.</summary>" );
		sb.AppendLine( "\tpublic void LeaveLobby()" );
		sb.AppendLine( "\t{" );
		sb.AppendLine( "\t\tNetworking.Disconnect();" );
		sb.AppendLine( "\t\tLog.Info( \"Left lobby\" );" );
		sb.AppendLine( "\t}" );
		sb.AppendLine();
		sb.AppendLine( "\t/// <summary>Called when a player connects to the lobby.</summary>" );
		sb.AppendLine( "\tpublic void OnActive( Connection channel )" );
		sb.AppendLine( "\t{" );
		sb.AppendLine( "\t\tLog.Info( $\"Player connected: {channel.DisplayName}\" );" );
		sb.AppendLine();
		sb.AppendLine( "\t\t// Spawn player prefab for the new connection" );
		sb.AppendLine( "\t\tif ( PlayerPrefab != null )" );
		sb.AppendLine( "\t\t{" );
		sb.AppendLine( "\t\t\tvar player = PlayerPrefab.Clone();" );
		sb.AppendLine( "\t\t\tplayer.NetworkSpawn( channel );" );
		sb.AppendLine( "\t\t}" );
		sb.AppendLine( "\t}" );
		sb.AppendLine();
		sb.AppendLine( "\t/// <summary>Called when a player disconnects.</summary>" );
		sb.AppendLine( "\tpublic void OnDisconnected( Connection channel )" );
		sb.AppendLine( "\t{" );
		sb.AppendLine( "\t\tLog.Info( $\"Player disconnected: {channel.DisplayName}\" );" );
		sb.AppendLine();
		sb.AppendLine( "\t\t// Clean up their objects" );
		sb.AppendLine( "\t\tforeach ( var go in Scene.GetAllObjects( true ) )" );
		sb.AppendLine( "\t\t{" );
		sb.AppendLine( "\t\t\tif ( go.Network?.Owner == channel )" );
		sb.AppendLine( "\t\t\t{" );
		sb.AppendLine( "\t\t\t\tgo.Destroy();" );
		sb.AppendLine( "\t\t\t}" );
		sb.AppendLine( "\t\t}" );
		sb.AppendLine( "\t}" );
		sb.AppendLine( "}" );

		var relPath = string.IsNullOrEmpty( directory )
			? $"code/{name}.cs"
			: $"code/{directory}/{name}.cs";

		var fullPath = Path.GetFullPath( Path.Combine( projectRoot, relPath ) );
		if ( !fullPath.StartsWith( projectRoot ) )
			throw new Exception( "Path must be within the project directory" );

		var dirStr = Path.GetDirectoryName( fullPath );
		if ( !string.IsNullOrEmpty( dirStr ) )
			Directory.CreateDirectory( dirStr );

		File.WriteAllText( fullPath, sb.ToString() );

		return Task.FromResult<object>( new
		{
			path = relPath,
			name,
			maxPlayers,
			created = true,
		} );
	}
}
