using System;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxBridge;

/// <summary>
/// Undo the last editor action.
///
/// API-NOTE: The exact undo API needs verification against the s&box SDK.
/// Candidate approaches:
///   Option A: Undo.PerformUndo() / Undo.PerformRedo()
///   Option B: EditorScene.Undo() / EditorScene.Redo()
///   Option C: EditorUtility.Undo() / EditorUtility.Redo()
/// Currently uses Option A. Uncomment the correct one after SDK verification.
/// </summary>
public class UndoHandler : ICommandHandler
{
	public Task<object> Execute( JsonElement parameters )
	{
		try
		{
			// Option A — most likely API path
			Undo.PerformUndo();

			return Task.FromResult<object>( new
			{
				action = "undo",
				success = true,
			} );
		}
		catch ( Exception ex )
		{
			return Task.FromResult<object>( new
			{
				action = "undo",
				success = false,
				error = $"Undo failed: {ex.Message}. Check UndoHandler.cs API-NOTE for alternative APIs.",
			} );
		}
	}
}

/// <summary>
/// Redo the last undone editor action. See UndoHandler for API notes.
/// </summary>
public class RedoHandler : ICommandHandler
{
	public Task<object> Execute( JsonElement parameters )
	{
		try
		{
			Undo.PerformRedo();

			return Task.FromResult<object>( new
			{
				action = "redo",
				success = true,
			} );
		}
		catch ( Exception ex )
		{
			return Task.FromResult<object>( new
			{
				action = "redo",
				success = false,
				error = $"Redo failed: {ex.Message}. Check UndoHandler.cs API-NOTE for alternative APIs.",
			} );
		}
	}
}
