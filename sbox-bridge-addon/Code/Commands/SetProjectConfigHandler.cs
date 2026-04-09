using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Sandbox;

namespace SboxBridge;

/// <summary>
/// Updates project configuration fields (title, description, version, type, tags, metadata)
/// and saves the .sbproj file. Only provided fields are updated; omitted fields are unchanged.
/// </summary>
public class SetProjectConfigHandler : ICommandHandler
{
	public Task<object> Execute( JsonElement parameters )
	{
		var project = Project.Current;
		if ( project == null )
			throw new Exception( "No project is currently open" );

		var config = project.Config;
		var changed = new System.Collections.Generic.List<string>();

		// Update Title
		if ( parameters.TryGetProperty( "title", out var titleProp ) && titleProp.ValueKind == JsonValueKind.String )
		{
			config.Title = titleProp.GetString();
			changed.Add( "title" );
		}

		// Update Description
		if ( parameters.TryGetProperty( "description", out var descProp ) && descProp.ValueKind == JsonValueKind.String )
		{
			config.Description = descProp.GetString();
			changed.Add( "description" );
		}

		// Update Version — API-NOTE: ProjectConfig.Version may not exist; fallback to raw .sbproj edit
		if ( parameters.TryGetProperty( "version", out var verProp ) && verProp.ValueKind == JsonValueKind.String )
		{
			// Try setting via config object first
			try
			{
				config.Version = verProp.GetString();
				changed.Add( "version" );
			}
			catch
			{
				// Fallback: edit .sbproj JSON directly
				if ( UpdateSbprojField( project.GetRootPath(), "Version", verProp.GetString() ) )
					changed.Add( "version (raw)" );
			}
		}

		// Update Type
		if ( parameters.TryGetProperty( "type", out var typeProp ) && typeProp.ValueKind == JsonValueKind.String )
		{
			var typeStr = typeProp.GetString();
			if ( Enum.TryParse<ProjectConfig.ProjectType>( typeStr, true, out var projectType ) )
			{
				config.Type = projectType;
				changed.Add( "type" );
			}
		}

		// Update Metadata fields
		if ( parameters.TryGetProperty( "summary", out var summaryProp ) && summaryProp.ValueKind == JsonValueKind.String )
		{
			config.Metadata ??= new System.Collections.Generic.Dictionary<string, string>();
			config.Metadata["Summary"] = summaryProp.GetString();
			changed.Add( "metadata.summary" );
		}

		if ( parameters.TryGetProperty( "isPublic", out var publicProp ) )
		{
			config.Metadata ??= new System.Collections.Generic.Dictionary<string, string>();
			config.Metadata["Public"] = publicProp.GetBoolean() ? "true" : "false";
			changed.Add( "metadata.public" );
		}

		// Update PackageIdent — API-NOTE: May be read-only on some SDK versions
		if ( parameters.TryGetProperty( "packageIdent", out var identProp ) && identProp.ValueKind == JsonValueKind.String )
		{
			try
			{
				config.PackageIdent = identProp.GetString();
				changed.Add( "packageIdent" );
			}
			catch
			{
				if ( UpdateSbprojField( project.GetRootPath(), "Ident", identProp.GetString() ) )
					changed.Add( "packageIdent (raw)" );
			}
		}

		// Save the config
		project.SaveConfig();

		return Task.FromResult<object>( new
		{
			success = true,
			updatedFields = changed,
			title = config.Title,
			description = config.Description,
			packageIdent = config.PackageIdent,
			type = config.Type.ToString(),
		} );
	}

	/// <summary>
	/// Fallback: directly edit the .sbproj JSON file for fields not exposed via ProjectConfig.
	/// </summary>
	private static bool UpdateSbprojField( string projectRoot, string fieldName, string value )
	{
		try
		{
			var sbprojFiles = Directory.GetFiles( projectRoot, "*.sbproj", SearchOption.TopDirectoryOnly );
			if ( sbprojFiles.Length == 0 ) return false;

			var json = File.ReadAllText( sbprojFiles[0] );
			var node = JsonNode.Parse( json );
			if ( node == null ) return false;

			node[fieldName] = value;
			File.WriteAllText( sbprojFiles[0], node.ToJsonString( new JsonSerializerOptions { WriteIndented = true } ) );
			return true;
		}
		catch
		{
			return false;
		}
	}
}
