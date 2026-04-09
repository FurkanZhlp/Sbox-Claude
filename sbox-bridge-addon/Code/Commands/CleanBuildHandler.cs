using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxBridge;

/// <summary>
/// Cleans compiled output and triggers a fresh rebuild of the project.
/// Removes intermediate build artifacts and forces a full recompilation.
/// </summary>
public class CleanBuildHandler : ICommandHandler
{
	public Task<object> Execute( JsonElement parameters )
	{
		var project = Project.Current;
		if ( project == null )
			throw new Exception( "No project is currently open" );

		var projectRoot = project.GetRootPath();
		if ( !projectRoot.EndsWith( Path.DirectorySeparatorChar ) )
			projectRoot += Path.DirectorySeparatorChar;

		var cleanedPaths = new System.Collections.Generic.List<string>();

		// Clean common build output directories
		var buildDirs = new[] { "bin", "obj", ".build", ".compiled" };
		foreach ( var dir in buildDirs )
		{
			var fullPath = Path.Combine( projectRoot, dir );
			if ( Directory.Exists( fullPath ) )
			{
				try
				{
					Directory.Delete( fullPath, true );
					cleanedPaths.Add( dir );
				}
				catch ( Exception ex )
				{
					Log.Warning( $"[SboxBridge] Could not clean {dir}: {ex.Message}" );
				}
			}
		}

		// Trigger rebuild after cleaning
		try
		{
			EditorUtility.Projects.Compile();
		}
		catch
		{
			try
			{
				project.Compile();
			}
			catch ( Exception ex )
			{
				Log.Warning( $"[SboxBridge] Rebuild after clean failed: {ex.Message}" );
			}
		}

		// Check results
		var errorCount = 0;
		try
		{
			var diagnostics = project.GetCompileDiagnostics();
			if ( diagnostics != null )
			{
				errorCount = diagnostics.Count( d => d.Severity == CompileDiagnostic.SeverityLevel.Error );
			}
		}
		catch { }

		return Task.FromResult<object>( new
		{
			cleaned = true,
			cleanedDirectories = cleanedPaths,
			rebuilt = true,
			success = errorCount == 0,
			errorCount,
			message = cleanedPaths.Count > 0
				? $"Cleaned {cleanedPaths.Count} directory(ies) and rebuilt. {errorCount} error(s)"
				: $"No build directories to clean. Rebuilt with {errorCount} error(s)",
		} );
	}
}
