using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Editor;

namespace SboxClaude;

/// <summary>
/// Editor plugin entry point. Starts/stops the WebSocket bridge alongside the editor.
/// </summary>
[EditorPlugin]
public class ClaudeBridgePlugin : EditorPlugin
{
    private BridgeServer? _server;

    public override void OnStart()
    {
        _server = new BridgeServer();
        _server.Start();
    }

    public override void OnShutdown()
    {
        _server?.Stop();
        _server = null;
    }
}

/// <summary>
/// HTTP/WebSocket server that accepts a single client connection from the MCP server
/// and dispatches JSON commands to registered <see cref="IToolHandler"/> instances.
///
/// Protocol (see CLAUDE.md):
///   Request  { "id": "uuid", "command": "...", "params": { ... } }
///   Response { "id": "uuid", "result":  { ... } }
///   Error    { "id": "uuid", "error":  { "code": "...", "message": "..." } }
///
/// Validation (Task 9):
///   • Messages larger than 1 MB are rejected with INVALID_REQUEST
///   • Missing / empty "id" → INVALID_REQUEST
///   • Missing / empty "command" → INVALID_REQUEST
///   • No registered handler → UNKNOWN_COMMAND
///   • Handler throws → HANDLER_ERROR
/// </summary>
public sealed class BridgeServer
{
    private const int MaxMessageBytes = 1024 * 1024; // 1 MB
    private const int DefaultPort = 8765;

    private readonly Dictionary<string, IToolHandler> _handlers = new();
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;

    public BridgeServer() => RegisterHandlers();

    // ── Handler registration ──────────────────────────────────────────────

    private void RegisterHandlers()
    {
        IToolHandler[] handlers =
        [
            new GetConsoleOutputHandler(),
            new CreateGameObjectHandler(),
            new DeleteGameObjectHandler(),
            new SetTransformHandler(),
            new GetSceneHierarchyHandler(),
            new GetAllPropertiesHandler(),
            new AddComponentWithPropertiesHandler(),
            new PingHandler(),
        ];

        foreach (var h in handlers)
            _handlers[h.Command] = h;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    public void Start(int port = DefaultPort)
    {
        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();

        LogCapture.Initialize();

        _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
        Log.Info($"[Claude Bridge] Listening on ws://localhost:{port}/");
    }

    public void Stop()
    {
        _cts?.Cancel();
        try { _listener?.Stop(); } catch { /* already stopped */ }
        LogCapture.Shutdown();
        Log.Info("[Claude Bridge] Stopped.");
    }

    // ── Accept loop ───────────────────────────────────────────────────────

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var ctx = await _listener!.GetContextAsync().WaitAsync(token);

                if (ctx.Request.IsWebSocketRequest)
                {
                    var wsCtx = await ctx.AcceptWebSocketAsync(subProtocol: null);
                    _ = Task.Run(() => HandleClientAsync(wsCtx.WebSocket, token), token);
                }
                else
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.Close();
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Log.Error($"[Claude Bridge] Accept error: {ex.Message}");
            }
        }
    }

    // ── Per-client receive loop ───────────────────────────────────────────

    private async Task HandleClientAsync(WebSocket ws, CancellationToken token)
    {
        // Use a 2 MB buffer — we'll enforce the 1 MB limit after the receive
        var buffer = new byte[MaxMessageBytes * 2];

        while (ws.State == WebSocketState.Open && !token.IsCancellationRequested)
        {
            try
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                switch (result.MessageType)
                {
                    case WebSocketMessageType.Close:
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, token);
                        return;

                    case WebSocketMessageType.Text:
                        if (result.Count > MaxMessageBytes)
                        {
                            await SendErrorAsync(ws, null, "INVALID_REQUEST",
                                "Message exceeds 1 MB limit", token);
                            continue;
                        }
                        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        // Fire-and-forget per request so slow handlers don't block receive
                        _ = Task.Run(() => ProcessRequestAsync(ws, json, token), token);
                        break;
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Log.Error($"[Claude Bridge] Receive error: {ex.Message}");
                break;
            }
        }
    }

    // ── Request processing ────────────────────────────────────────────────

    private async Task ProcessRequestAsync(WebSocket ws, string json, CancellationToken token)
    {
        string? requestId = null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Validate id
            if (!root.TryGetProperty("id", out var idEl) ||
                string.IsNullOrWhiteSpace(idEl.GetString()))
            {
                await SendErrorAsync(ws, null, "INVALID_REQUEST",
                    "Field 'id' is missing or empty", token);
                return;
            }
            requestId = idEl.GetString();

            // Validate command
            if (!root.TryGetProperty("command", out var cmdEl) ||
                string.IsNullOrWhiteSpace(cmdEl.GetString()))
            {
                await SendErrorAsync(ws, requestId, "INVALID_REQUEST",
                    "Field 'command' is missing or empty", token);
                return;
            }
            var command = cmdEl.GetString()!;

            // Resolve handler
            if (!_handlers.TryGetValue(command, out var handler))
            {
                await SendErrorAsync(ws, requestId, "UNKNOWN_COMMAND",
                    $"No handler registered for command '{command}'", token);
                return;
            }

            var parameters = root.TryGetProperty("params", out var pEl)
                ? pEl
                : default;

            var result = await handler.ExecuteAsync(parameters);
            await SendResultAsync(ws, requestId, result, token);
        }
        catch (JsonException je)
        {
            await SendErrorAsync(ws, requestId, "INVALID_REQUEST",
                $"Malformed JSON: {je.Message}", token);
        }
        catch (Exception ex)
        {
            Log.Error($"[Claude Bridge] Handler error for '{requestId}': {ex}");
            await SendErrorAsync(ws, requestId, "HANDLER_ERROR", ex.Message, token);
        }
    }

    // ── Send helpers ──────────────────────────────────────────────────────

    private static async Task SendResultAsync(
        WebSocket ws, string? id, object result, CancellationToken token)
    {
        var payload = JsonSerializer.Serialize(new { id, result });
        await SendRawAsync(ws, payload, token);
    }

    private static async Task SendErrorAsync(
        WebSocket ws, string? id, string code, string message, CancellationToken token)
    {
        var payload = JsonSerializer.Serialize(new { id, error = new { code, message } });
        await SendRawAsync(ws, payload, token);
    }

    private static async Task SendRawAsync(WebSocket ws, string text, CancellationToken token)
    {
        if (ws.State != WebSocketState.Open) return;
        var bytes = Encoding.UTF8.GetBytes(text);
        await ws.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            endOfMessage: true,
            token);
    }
}

/// <summary>
/// Built-in ping handler. Used by the get_bridge_status MCP tool to measure
/// round-trip latency and retrieve basic editor information.
/// </summary>
internal sealed class PingHandler : IToolHandler
{
    public string Command => "ping";

    public Task<object> ExecuteAsync(JsonElement parameters)
    {
        return Task.FromResult<object>(new
        {
            pong = true,
            // Expose the s&box editor version if the API is available.
            // Replace with the real API call once confirmed:
            // version = Editor.Application.Version ?? "unknown"
            version = "unknown",
        });
    }
}
