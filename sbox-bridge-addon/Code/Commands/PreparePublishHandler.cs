using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxBridge;

/// <summary>
/// Comprehensive publish preparation: validates the project, compiles,
/// checks metadata, and generates a detailed publishing readiness report.
/// Combines validation + build + metadata check into one tool.
/// </summary>
public class PreparePublishHandler : ICommandHandler
{
	public Task<object> Execute( JsonElement parameters )
	{
		var project = Project.Current;
		if ( project == null )
			throw new Exception( "No project is currently open" );

		var config = project.Config;
		var projectRoot = project.GetRootPath();
		if ( !projectRoot.EndsWith( Path.DirectorySeparatorChar ) )
			projectRoot += Path.DirectorySeparatorChar;

		var issues = new List<object>();
		var warnings = new List<object>();
		var info = new List<object>();

		// ── Step 1: Trigger compilation ──
		try
		{
			EditorUtility.Projects.Compile();
			info.Add( new { step = "compile", message = "Compilation triggered" } );
		}
		catch
		{
			try
			{
				project.Compile();
				info.Add( new { step = "compile", message = "Compilation triggered (fallback)" } );
			}
			catch ( Exception ex )
			{
				warnings.Add( new { step = "compile", message = $"Could not trigger compile: {ex.Message}" } );
			}
		}

		// ── Step 2: Check compile errors ──
		var errorCount = 0;
		var warningCount = 0;
		try
		{
			var diagnostics = project.GetCompileDiagnostics();
			if ( diagnostics != null )
			{
				errorCount = diagnostics.Count( d => d.Severity == CompileDiagnostic.SeverityLevel.Error );
				warningCount = diagnostics.Count( d => d.Severity == CompileDiagnostic.SeverityLevel.Warning );
			}
		}
		catch { }

		if ( errorCount > 0 )
			issues.Add( new { field = "compilation", message = $"{errorCount} compile error(s) must be fixed" } );
		if ( warningCount > 0 )
			warnings.Add( new { field = "compilation", message = $"{warningCount} compile warning(s)" } );

		// ── Step 3: Check metadata ──
		if ( string.IsNullOrWhiteSpace( config?.Title ) )
			issues.Add( new { field = "title", message = "Project title is required" } );

		if ( string.IsNullOrWhiteSpace( config?.Description ) )
			issues.Add( new { field = "description", message = "Project description is required" } );

		if ( string.IsNullOrWhiteSpace( config?.PackageIdent ) )
			issues.Add( new { field = "packageIdent", message = "Package identifier is required (e.g. 'myorg.mygame')" } );

		// ── Step 4: Check content ──
		var sceneCount = Directory.GetFiles( projectRoot, "*.scene", SearchOption.AllDirectories ).Length;
		if ( sceneCount == 0 )
			issues.Add( new { field = "scenes", message = "No scene files found — at least one scene is needed" } );
		else
			info.Add( new { step = "scenes", message = $"{sceneCount} scene(s) found" } );

		var codeDir = Path.Combine( projectRoot, "code" );
		var scriptCount = Directory.Exists( codeDir )
			? Directory.GetFiles( codeDir, "*.cs", SearchOption.AllDirectories ).Length
			: 0;
		info.Add( new { step = "scripts", message = $"{scriptCount} script(s) found" } );

		// ── Step 5: Check thumbnail ──
		var thumbnailPaths = new[] { "thumb.png", "thumb.jpg", "icon.png", "thumbnail.png" };
		var hasThumbnail = thumbnailPaths.Any( t => File.Exists( Path.Combine( projectRoot, t ) ) );
		if ( !hasThumbnail )
			warnings.Add( new { field = "thumbnail", message = "No thumbnail found — add thumb.png for better visibility on asset.party" } );

		// ── Step 6: Check project type ──
		if ( config?.Type == null )
			warnings.Add( new { field = "projectType", message = "Project type not set — set to 'game', 'addon', or 'library'" } );

		// ── Summary ──
		var ready = issues.Count == 0;
		var readiness = ready ? "ready" : "not_ready";

		// Calculate project size
		long totalSize = 0;
		try
		{
			foreach ( var file in Directory.GetFiles( projectRoot, "*", SearchOption.AllDirectories ) )
			{
				totalSize += new FileInfo( file ).Length;
			}
		}
		catch { }

		return Task.FromResult<object>( new
		{
			ready,
			readiness,
			project = new
			{
				title = config?.Title,
				description = config?.Description,
				ident = config?.PackageIdent,
				type = config?.Type?.ToString(),
			},
			stats = new
			{
				sceneCount,
				scriptCount,
				compileErrors = errorCount,
				compileWarnings = warningCount,
				hasThumbnail,
				projectSizeBytes = totalSize,
				projectSizeMB = Math.Round( totalSize / 1024.0 / 1024.0, 2 ),
			},
			issueCount = issues.Count,
			warningCount = warnings.Count,
			issues,
			warnings,
			info,
			nextSteps = ready
				? new[] {
					"Project is ready to publish!",
					"Use the s&box editor: Edit → Publish Project",
					"Or visit sbox.game → My Creations → Upload Project",
				}
				: new[] {
					"Fix the issues listed above before publishing",
					"Use set_project_config to update metadata",
					"Use build_project to recompile after fixing code",
				},
		} );
	}
}
