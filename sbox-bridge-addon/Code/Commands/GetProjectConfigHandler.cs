using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxBridge;

/// <summary>
/// Returns the full project configuration from the .sbproj file.
/// Includes title, description, version, type, package references,
/// metadata, resources, code files, and all other config fields.
/// </summary>
public class GetProjectConfigHandler : ICommandHandler
{
	public Task<object> Execute( JsonElement parameters )
	{
		var project = Project.Current;
		if ( project == null )
			throw new System.Exception( "No project is currently open" );

		var config = project.Config;
		var projectRoot = project.GetRootPath();

		// Read raw .sbproj JSON for any fields not exposed via Config
		string rawJson = null;
		var sbprojFiles = Directory.GetFiles( projectRoot, "*.sbproj", SearchOption.TopDirectoryOnly );
		if ( sbprojFiles.Length > 0 )
		{
			rawJson = File.ReadAllText( sbprojFiles[0] );
		}

		var result = new
		{
			path = projectRoot,
			sbprojFile = sbprojFiles.Length > 0 ? Path.GetFileName( sbprojFiles[0] ) : null,
			title = config?.Title,
			description = config?.Description,
			packageIdent = config?.PackageIdent,
			type = config?.Type.ToString(),
			version = config?.Version,
			defines = config?.Defines,
			packageReferences = config?.PackageReferences,
			metadata = new
			{
				summary = config?.Metadata != null && config.Metadata.ContainsKey( "Summary" )
					? config.Metadata["Summary"] : null,
				isPublic = config?.Metadata != null && config.Metadata.ContainsKey( "Public" )
					? config.Metadata["Public"] : null,
			},
			rawConfig = rawJson,
		};

		return Task.FromResult<object>( result );
	}
}
