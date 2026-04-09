using System.Text.Json;
using System.Threading.Tasks;

namespace SboxClaude;

/// <summary>
/// Implement this interface to handle one MCP tool command.
/// Register the handler in BridgeServer.RegisterHandlers().
/// See CLAUDE.md for the full "How to Add a New Tool" walkthrough.
/// </summary>
public interface IToolHandler
{
    /// <summary>
    /// The command name exactly as it appears in the MCP tool definition
    /// (e.g. "create_gameobject"). BridgeServer routes by this key.
    /// </summary>
    string Command { get; }

    /// <summary>
    /// Execute the command. <paramref name="parameters"/> is the "params" object
    /// from the WebSocket request — use TryGetProperty to read fields safely.
    /// Return any JSON-serializable object; it becomes the "result" field.
    /// Throw an exception to return a HANDLER_ERROR response.
    /// </summary>
    Task<object> ExecuteAsync(JsonElement parameters);
}
