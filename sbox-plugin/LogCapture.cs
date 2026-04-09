using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Sandbox;

namespace SboxClaude;

/// <summary>
/// Severity levels that mirror s&box log levels.
/// Order matters — filtering uses >= comparison.
/// </summary>
public enum LogSeverity
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warning = 3,
    Error = 4,
}

/// <summary>
/// One captured log entry.
/// </summary>
public sealed record LogEntry(DateTime Timestamp, LogSeverity Severity, string Message);

/// <summary>
/// Captures log messages produced inside the s&box editor so they can be
/// retrieved by the get_console_output tool.
///
/// IMPLEMENTATION NOTE (Task 11):
/// The exact s&box logging callback API needs to be verified against
/// https://github.com/Facepunch/sbox-public before shipping.
///
/// Candidate APIs to try (check which one exists in your SDK version):
///   Option A — static event on Log:
///       Log.OnMessage += (string text, LogLevel level) => ...
///
///   Option B — InternalExtensions.Logging callback:
///       InternalExtensions.Logging.OnConsoleMessage += (channel, text, level) => ...
///
///   Option C — Override OnLog in an EditorPlugin subclass:
///       protected override void OnLog(string channel, string text, int level) { ... }
///
/// The stub below implements the circular buffer and severity mapping; wire up
/// whichever hook compiles, then remove this comment.
/// </summary>
public static class LogCapture
{
    private const int MaxEntries = 1_000;
    private static readonly ConcurrentQueue<LogEntry> _entries = new();
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        // TODO: uncomment the correct hook for your s&box SDK version.
        //
        // Option A:
        // Log.OnMessage += HandleLogMessage;
        //
        // Option B:
        // InternalExtensions.Logging.OnConsoleMessage += HandleInternalMessage;

        Log.Info("[Claude Bridge] Log capture initialized");
    }

    public static void Shutdown()
    {
        // TODO: unsubscribe the same event you subscribed above.
        // Log.OnMessage -= HandleLogMessage;
        _initialized = false;
        while (_entries.TryDequeue(out _)) { }
    }

    // ── Hook implementations ─────────────────────────────────────────────

    // Option A callback signature (verify against s&box source):
    // private static void HandleLogMessage(string text, LogLevel level)
    //     => Append(MapLevel(level), text);

    // Option B callback signature (verify against s&box source):
    // private static void HandleInternalMessage(string channel, string text, int level)
    //     => Append(MapRawLevel(level), $"[{channel}] {text}");

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Append a message directly. Useful for testing and for the Option C
    /// (override) approach where you call this from OnLog.
    /// </summary>
    public static void Append(LogSeverity severity, string message)
    {
        _entries.Enqueue(new LogEntry(DateTime.UtcNow, severity, message));
        // Keep the buffer bounded
        while (_entries.Count > MaxEntries)
            _entries.TryDequeue(out _);
    }

    /// <summary>
    /// Retrieve recent entries, optionally filtered.
    /// </summary>
    public static IReadOnlyList<LogEntry> GetEntries(
        LogSeverity minSeverity = LogSeverity.Info,
        int limit = 100,
        string? filter = null)
    {
        var filtered = new List<LogEntry>();

        foreach (var entry in _entries)
        {
            if (entry.Severity < minSeverity) continue;
            if (filter != null && !entry.Message.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;
            filtered.Add(entry);
        }

        // Return the most recent `limit` entries
        var start = Math.Max(0, filtered.Count - limit);
        return filtered.GetRange(start, filtered.Count - start);
    }

    // ── Severity mapping helpers ──────────────────────────────────────────

    /// <summary>
    /// Map a raw integer log level to LogSeverity.
    /// Typical s&box convention: 0=Trace 1=Debug 2=Info 3=Warn 4=Error
    /// Adjust if the real values differ.
    /// </summary>
    public static LogSeverity MapRawLevel(int level) => level switch
    {
        0 => LogSeverity.Trace,
        1 => LogSeverity.Debug,
        2 => LogSeverity.Info,
        3 => LogSeverity.Warning,
        4 => LogSeverity.Error,
        _ => LogSeverity.Info,
    };

    /// <summary>Parse a severity string from the MCP params into LogSeverity.</summary>
    public static LogSeverity ParseSeverityString(string? s) => s?.ToLowerInvariant() switch
    {
        "trace" => LogSeverity.Trace,
        "debug" => LogSeverity.Debug,
        "warning" or "warn" => LogSeverity.Warning,
        "error" => LogSeverity.Error,
        _ => LogSeverity.Info,
    };
}
