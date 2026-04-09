using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sandbox;

namespace SboxBridge;

/// <summary>
/// Adds a [Sync] networked property to an existing C# script.
/// Inserts a property with the [Sync] attribute that automatically
/// replicates across the network.
/// </summary>
public class AddSyncPropertyHandler : ICommandHandler
{
	public Task<object> Execute( JsonElement parameters )
	{
		var projectRoot = Project.Current?.GetRootPath();
		if ( string.IsNullOrEmpty( projectRoot ) )
			throw new Exception( "No project is currently open" );

		if ( !projectRoot.EndsWith( Path.DirectorySeparatorChar ) )
			projectRoot += Path.DirectorySeparatorChar;

		var scriptPath = parameters.GetProperty( "path" ).GetString()
			?? throw new Exception( "Missing required parameter: path" );
		var propertyName = parameters.GetProperty( "propertyName" ).GetString()
			?? throw new Exception( "Missing required parameter: propertyName" );
		var propertyType = parameters.TryGetProperty( "propertyType", out var typeProp )
			? typeProp.GetString() ?? "float" : "float";

		var fullPath = Path.GetFullPath( Path.Combine( projectRoot, scriptPath ) );
		if ( !fullPath.StartsWith( projectRoot ) )
			throw new Exception( "Path must be within the project directory" );

		if ( !File.Exists( fullPath ) )
			throw new Exception( $"Script not found: {scriptPath}" );

		var content = File.ReadAllText( fullPath );

		// Build the sync property line
		var syncFlags = parameters.TryGetProperty( "syncFlags", out var flagsProp )
			? flagsProp.GetString() ?? "" : "";

		var defaultValue = parameters.TryGetProperty( "defaultValue", out var defProp )
			? defProp.GetString() : null;

		string syncAttr;
		if ( !string.IsNullOrEmpty( syncFlags ) )
			syncAttr = $"[Sync( SyncFlags.{syncFlags} )]";
		else
			syncAttr = "[Sync]";

		string propLine;
		if ( !string.IsNullOrEmpty( defaultValue ) )
			propLine = $"\t{syncAttr} public {propertyType} {propertyName} {{ get; set; }} = {defaultValue};";
		else
			propLine = $"\t{syncAttr} public {propertyType} {propertyName} {{ get; set; }}";

		// Insert before the first method or at the end of properties section
		// Find the last property or field line, or the class opening brace
		var classMatch = Regex.Match( content, @"\bclass\s+\w+[^{]*\{" );
		if ( !classMatch.Success )
			throw new Exception( "Could not find class declaration in script" );

		var insertPos = classMatch.Index + classMatch.Length;
		content = content.Insert( insertPos, "\n" + propLine + "\n" );

		File.WriteAllText( fullPath, content );

		return Task.FromResult<object>( new
		{
			path = scriptPath,
			propertyName,
			propertyType,
			syncAttribute = syncAttr,
			added = true,
		} );
	}
}
