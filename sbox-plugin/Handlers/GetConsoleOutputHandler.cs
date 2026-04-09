using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SboxClaude;

/// <summary>
/// Command: get_console_output
///
/// Returns recent log messages captured by <see cref="LogCapture"/>.
///
/// Params:
///   severity  string  Minimum level: "trace"|"debug"|"info"|"warning"|"error"
///                     Default: "info"
///   limit     int     Maximum number of entries to return. Default: 100.
///   filter    string  Optional substring filter (case-insensitive).
///
/// NOTE (Task 11): The LogCapture.cs stub needs the correct s&box logging
/// hook wired up before this handler returns real data. See LogCapture.cs
/// for details on which callback to subscribe.
/// </summary>
public sealed class GetConsoleOutputHandler : IToolHandler
{
    public string Command => "get_console_output";

    public Task<object> ExecuteAsync(JsonElement parameters)
    {
        var severity = LogCapture.ParseSeverityString(
            parameters.TryGetProperty("severity", out var sevEl) ? sevEl.GetString() : null);

        var limit = parameters.TryGetProperty("limit", out var limEl)
            ? limEl.GetInt32()
            : 100;

        var filter = parameters.TryGetProperty("filter", out var filtEl)
            ? filtEl.GetString()
            : null;

        var entries = LogCapture.GetEntries(severity, limit, filter);

        return Task.FromResult<object>(new
        {
            count = entries.Count,
            entries = entries.Select(e => new
            {
                timestamp = e.Timestamp.ToString("O"),
                severity = e.Severity.ToString().ToLowerInvariant(),
                message = e.Message,
            }).ToList(),
        });
    }
}
