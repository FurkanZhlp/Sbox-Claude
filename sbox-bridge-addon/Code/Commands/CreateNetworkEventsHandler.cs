using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxBridge;

/// <summary>
/// Generates a script that implements INetworkListener for handling
/// network lifecycle events (connect, disconnect, become host, etc.).
/// </summary>
public class CreateNetworkEventsHandler : ICommandHandler
{
	public Task<object> Execute( JsonElement parameters )
	{
		var projectRoot = Project.Current?.GetRootPath();
		if ( string.IsNullOrEmpty( projectRoot ) )
			throw new Exception( "No project is currently open" );

		if ( !projectRoot.EndsWith( Path.DirectorySeparatorChar ) )
			projectRoot += Path.DirectorySeparatorChar;

		var name = parameters.TryGetProperty( "name", out var nameProp )
			? nameProp.GetString() ?? "NetworkEvents" : "NetworkEvents";

		var directory = parameters.TryGetProperty( "directory", out var dirProp )
			? dirProp.GetString() ?? "" : "";

		var includeChat = parameters.TryGetProperty( "includeChat", out var chatProp )
			&& chatProp.GetBoolean();

		var sb = new StringBuilder();
		sb.AppendLine( "using Sandbox;" );
		sb.AppendLine( "using System.Collections.Generic;" );
		sb.AppendLine();
		sb.AppendLine( "/// <summary>" );
		sb.AppendLine( "/// Handles network lifecycle events: connections, disconnections, host migration." );
		sb.AppendLine( "/// Implements INetworkListener for automatic event callbacks." );
		sb.AppendLine( "/// </summary>" );
		sb.AppendLine( $"public sealed class {name} : Component, Component.INetworkListener" );
		sb.AppendLine( "{" );
		sb.AppendLine( "\t/// <summary>List of currently connected players.</summary>" );
		sb.AppendLine( "\tpublic List<Connection> ConnectedPlayers { get; } = new();" );
		sb.AppendLine();
		sb.AppendLine( "\t/// <summary>Called when this component first becomes active on the network.</summary>" );
		sb.AppendLine( "\tpublic void OnActive( Connection channel )" );
		sb.AppendLine( "\t{" );
		sb.AppendLine( "\t\tLog.Info( $\"[Network] Player joined: {channel.DisplayName} (Host: {channel.IsHost})\" );" );
		sb.AppendLine( "\t\tConnectedPlayers.Add( channel );" );
		sb.AppendLine( "\t\tOnPlayerJoined( channel.DisplayName );" );
		sb.AppendLine( "\t}" );
		sb.AppendLine();
		sb.AppendLine( "\t/// <summary>Called when a connection is lost.</summary>" );
		sb.AppendLine( "\tpublic void OnDisconnected( Connection channel )" );
		sb.AppendLine( "\t{" );
		sb.AppendLine( "\t\tLog.Info( $\"[Network] Player left: {channel.DisplayName}\" );" );
		sb.AppendLine( "\t\tConnectedPlayers.Remove( channel );" );
		sb.AppendLine( "\t\tOnPlayerLeft( channel.DisplayName );" );
		sb.AppendLine( "\t}" );
		sb.AppendLine();
		sb.AppendLine( "\t/// <summary>Broadcast notification when a player joins.</summary>" );
		sb.AppendLine( "\t[Rpc.Broadcast]" );
		sb.AppendLine( "\tpublic void OnPlayerJoined( string playerName )" );
		sb.AppendLine( "\t{" );
		sb.AppendLine( "\t\tLog.Info( $\"{playerName} has joined the game\" );" );
		sb.AppendLine( "\t}" );
		sb.AppendLine();
		sb.AppendLine( "\t/// <summary>Broadcast notification when a player leaves.</summary>" );
		sb.AppendLine( "\t[Rpc.Broadcast]" );
		sb.AppendLine( "\tpublic void OnPlayerLeft( string playerName )" );
		sb.AppendLine( "\t{" );
		sb.AppendLine( "\t\tLog.Info( $\"{playerName} has left the game\" );" );
		sb.AppendLine( "\t}" );

		if ( includeChat )
		{
			sb.AppendLine();
			sb.AppendLine( "\t/// <summary>Send a chat message to all players.</summary>" );
			sb.AppendLine( "\t[Rpc.Broadcast]" );
			sb.AppendLine( "\tpublic void SendChatMessage( string sender, string message )" );
			sb.AppendLine( "\t{" );
			sb.AppendLine( "\t\tLog.Info( $\"[Chat] {sender}: {message}\" );" );
			sb.AppendLine( "\t}" );
		}

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
			includeChat,
			created = true,
		} );
	}
}
