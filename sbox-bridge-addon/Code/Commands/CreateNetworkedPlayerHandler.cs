using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxBridge;

/// <summary>
/// Generates a network-aware player controller script.
/// Creates a component with [Sync] properties for position/health,
/// [Rpc.Broadcast] for actions, and ownership-based input handling.
/// </summary>
public class CreateNetworkedPlayerHandler : ICommandHandler
{
	public Task<object> Execute( JsonElement parameters )
	{
		var projectRoot = Project.Current?.GetRootPath();
		if ( string.IsNullOrEmpty( projectRoot ) )
			throw new Exception( "No project is currently open" );

		if ( !projectRoot.EndsWith( Path.DirectorySeparatorChar ) )
			projectRoot += Path.DirectorySeparatorChar;

		var name = parameters.TryGetProperty( "name", out var nameProp )
			? nameProp.GetString() ?? "NetworkedPlayer" : "NetworkedPlayer";

		var directory = parameters.TryGetProperty( "directory", out var dirProp )
			? dirProp.GetString() ?? "" : "";

		var moveSpeed = parameters.TryGetProperty( "moveSpeed", out var speedProp )
			? speedProp.GetSingle() : 300f;

		var includeHealth = !parameters.TryGetProperty( "includeHealth", out var healthProp )
			|| healthProp.GetBoolean();

		var sb = new StringBuilder();
		sb.AppendLine( "using Sandbox;" );
		sb.AppendLine();
		sb.AppendLine( "/// <summary>" );
		sb.AppendLine( "/// Network-aware player controller. Only processes input for the owning client." );
		sb.AppendLine( "/// Uses [Sync] for replicated state and [Rpc.Broadcast] for networked actions." );
		sb.AppendLine( "/// Requires a CharacterController component." );
		sb.AppendLine( "/// </summary>" );
		sb.AppendLine( $"public sealed class {name} : Component, Component.INetworkListener" );
		sb.AppendLine( "{" );
		sb.AppendLine( $"\t[Property] public float MoveSpeed {{ get; set; }} = {moveSpeed}f;" );
		sb.AppendLine( "\t[Property] public float JumpForce { get; set; } = 350f;" );
		sb.AppendLine( "\t[Property] public float MouseSensitivity { get; set; } = 2.0f;" );
		sb.AppendLine( "\t[Property] public float EyeHeight { get; set; } = 64f;" );
		sb.AppendLine();

		if ( includeHealth )
		{
			sb.AppendLine( "\t[Sync] public float Health { get; set; } = 100f;" );
			sb.AppendLine( "\t[Sync] public bool IsAlive { get; set; } = true;" );
			sb.AppendLine();
		}

		sb.AppendLine( "\t[Sync] public Angles EyeAngles { get; set; }" );
		sb.AppendLine();
		sb.AppendLine( "\tprivate CharacterController _cc;" );
		sb.AppendLine();
		sb.AppendLine( "\tprotected override void OnStart()" );
		sb.AppendLine( "\t{" );
		sb.AppendLine( "\t\t_cc = Components.Get<CharacterController>();" );
		sb.AppendLine( "\t}" );
		sb.AppendLine();
		sb.AppendLine( "\tprotected override void OnUpdate()" );
		sb.AppendLine( "\t{" );
		sb.AppendLine( "\t\tif ( _cc == null ) return;" );
		sb.AppendLine();
		sb.AppendLine( "\t\t// Only the owner processes input" );
		sb.AppendLine( "\t\tif ( !Network.IsOwner ) return;" );
		sb.AppendLine();

		if ( includeHealth )
		{
			sb.AppendLine( "\t\tif ( !IsAlive ) return;" );
			sb.AppendLine();
		}

		sb.AppendLine( "\t\t// Mouse look" );
		sb.AppendLine( "\t\tvar angles = EyeAngles;" );
		sb.AppendLine( "\t\tangles.pitch += Input.MouseDelta.y * MouseSensitivity * -0.1f;" );
		sb.AppendLine( "\t\tangles.yaw -= Input.MouseDelta.x * MouseSensitivity * 0.1f;" );
		sb.AppendLine( "\t\tangles.pitch = angles.pitch.Clamp( -89f, 89f );" );
		sb.AppendLine( "\t\tEyeAngles = angles;" );
		sb.AppendLine();
		sb.AppendLine( "\t\tWorldRotation = Rotation.From( 0, angles.yaw, 0 );" );
		sb.AppendLine();
		sb.AppendLine( "\t\t// Movement" );
		sb.AppendLine( "\t\tvar input = Input.AnalogMove;" );
		sb.AppendLine( "\t\tvar moveDir = WorldRotation * new Vector3( input.x, input.y, 0 ).Normal;" );
		sb.AppendLine( "\t\t_cc.Accelerate( moveDir * MoveSpeed );" );
		sb.AppendLine();
		sb.AppendLine( "\t\tif ( _cc.IsOnGround )" );
		sb.AppendLine( "\t\t{" );
		sb.AppendLine( "\t\t\t_cc.Velocity = _cc.Velocity.WithZ( 0 );" );
		sb.AppendLine( "\t\t\t_cc.ApplyFriction( 4.0f );" );
		sb.AppendLine( "\t\t\tif ( Input.Pressed( \"jump\" ) )" );
		sb.AppendLine( "\t\t\t{" );
		sb.AppendLine( "\t\t\t\t_cc.Punch( Vector3.Up * JumpForce );" );
		sb.AppendLine( "\t\t\t\tOnJump();" );
		sb.AppendLine( "\t\t\t}" );
		sb.AppendLine( "\t\t}" );
		sb.AppendLine( "\t\telse" );
		sb.AppendLine( "\t\t{" );
		sb.AppendLine( "\t\t\t_cc.Velocity += Vector3.Down * 800f * Time.Delta;" );
		sb.AppendLine( "\t\t}" );
		sb.AppendLine();
		sb.AppendLine( "\t\t_cc.Move();" );
		sb.AppendLine();
		sb.AppendLine( "\t\t// Camera" );
		sb.AppendLine( "\t\tvar cam = Scene.Camera;" );
		sb.AppendLine( "\t\tif ( cam != null )" );
		sb.AppendLine( "\t\t{" );
		sb.AppendLine( "\t\t\tcam.WorldPosition = WorldPosition + Vector3.Up * EyeHeight;" );
		sb.AppendLine( "\t\t\tcam.WorldRotation = Rotation.From( EyeAngles );" );
		sb.AppendLine( "\t\t}" );
		sb.AppendLine( "\t}" );
		sb.AppendLine();
		sb.AppendLine( "\t/// <summary>Broadcast jump event to all clients for effects/sounds.</summary>" );
		sb.AppendLine( "\t[Rpc.Broadcast]" );
		sb.AppendLine( "\tpublic void OnJump()" );
		sb.AppendLine( "\t{" );
		sb.AppendLine( "\t\t// Play jump sound/animation here" );
		sb.AppendLine( "\t}" );

		if ( includeHealth )
		{
			sb.AppendLine();
			sb.AppendLine( "\t/// <summary>Apply damage (host-authoritative).</summary>" );
			sb.AppendLine( "\t[Rpc.Host]" );
			sb.AppendLine( "\tpublic void TakeDamage( float amount )" );
			sb.AppendLine( "\t{" );
			sb.AppendLine( "\t\tHealth -= amount;" );
			sb.AppendLine( "\t\tif ( Health <= 0 )" );
			sb.AppendLine( "\t\t{" );
			sb.AppendLine( "\t\t\tHealth = 0;" );
			sb.AppendLine( "\t\t\tIsAlive = false;" );
			sb.AppendLine( "\t\t\tOnDeath();" );
			sb.AppendLine( "\t\t}" );
			sb.AppendLine( "\t}" );
			sb.AppendLine();
			sb.AppendLine( "\t[Rpc.Broadcast]" );
			sb.AppendLine( "\tpublic void OnDeath()" );
			sb.AppendLine( "\t{" );
			sb.AppendLine( "\t\t// Play death animation/ragdoll here" );
			sb.AppendLine( "\t}" );
		}

		sb.AppendLine();
		sb.AppendLine( "\tpublic void OnConnected( Connection channel )" );
		sb.AppendLine( "\t{" );
		sb.AppendLine( "\t\tLog.Info( $\"Player connected: {channel.DisplayName}\" );" );
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
			includeHealth,
			features = new[] { "movement", "mouse_look", "jump", "network_sync", "rpc_broadcast",
				includeHealth ? "health_system" : null },
			created = true,
		} );
	}
}
