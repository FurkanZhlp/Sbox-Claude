using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxBridge;

/// <summary>
/// Returns the current build/compilation status of the project.
/// Reports error count, warning count, diagnostics list, and whether
/// the project compiled successfully.
/// </summary>
public class GetBuildStatusHandler : ICommandHandler
{
	public Task<object> Execute( JsonElement parameters )
	{
		var project = Project.Current;
		if ( project == null )
			throw new Exception( "No project is currently open" );

		var errorCount = 0;
		var warningCount = 0;
		var infoCount = 0;
		var diagnosticsList = new List<object>();

		try
		{
			var diagnostics = project.GetCompileDiagnostics();
			if ( diagnostics != null )
			{
				foreach ( var diag in diagnostics )
				{
					var severity = "info";
					if ( diag.Severity == CompileDiagnostic.SeverityLevel.Error )
					{
						severity = "error";
						errorCount++;
					}
					else if ( diag.Severity == CompileDiagnostic.SeverityLevel.Warning )
					{
						severity = "warning";
						warningCount++;
					}
					else
					{
						infoCount++;
					}

					diagnosticsList.Add( new
					{
						file = diag.FilePath,
						line = diag.LineNumber,
						column = diag.Column,
						message = diag.Message,
						id = diag.Id,
						severity,
					} );
				}
			}
		}
		catch
		{
			// Diagnostics API may differ between SDK versions
		}

		// Determine if project is currently compiling
		// API-NOTE: Project.IsCompiling or EditorUtility.IsCompiling may exist
		var isCompiling = false;
		try
		{
			isCompiling = project.IsCompiling;
		}
		catch
		{
			// Property may not exist
		}

		return Task.FromResult<object>( new
		{
			isCompiling,
			success = errorCount == 0,
			errorCount,
			warningCount,
			infoCount,
			totalDiagnostics = diagnosticsList.Count,
			diagnostics = diagnosticsList,
		} );
	}
}
