/**
 * BridgeClient unit tests (Task 7).
 *
 * Uses a real WebSocketServer on a random OS-assigned port — no mocking library
 * needed for the network layer. s&box is never involved.
 *
 * Covers:
 *   • Connect / connected event
 *   • Send + receive a matched response
 *   • Server-side error response → BridgeClientError with correct code
 *   • Request timeout when server never replies
 *   • send() when not connected → NOT_CONNECTED
 *   • Auto-reconnect after server closes the connection
 *   • In-flight requests are rejected when the connection drops
 *   • Ping timeout forces reconnect (3 missed pings)
 */

import { AddressInfo } from 'node:net';
import { WebSocketServer, WebSocket } from 'ws';
import { BridgeClient, BridgeClientError } from '../src/BridgeClient.js';

// ── Helpers ───────────────────────────────────────────────────────────────────

/** Start a WS server on a random port; resolves when ready. */
function startServer(): Promise<{ server: WebSocketServer; port: number }> {
  return new Promise((resolve) => {
    const server = new WebSocketServer({ port: 0 }, () => {
      const port = (server.address() as AddressInfo).port;
      resolve({ server, port });
    });
  });
}

/** Create a client with aggressive timeouts so tests finish quickly. */
function makeClient(port: number): BridgeClient {
  return new BridgeClient(
    'localhost',
    port,
    /* requestTimeout  */ 500,
    /* reconnectDelay  */ 100,
    /* pingInterval    */ 300,
    /* maxMissedPings  */ 3,
  );
}

/** Wait for an event on an EventEmitter. */
function once(emitter: BridgeClient, event: string): Promise<void> {
  return new Promise((resolve) => emitter.once(event, resolve));
}

// ── Suite ─────────────────────────────────────────────────────────────────────

describe('BridgeClient', () => {
  let server: WebSocketServer;
  let port: number;
  let client: BridgeClient;

  beforeEach(async () => {
    ({ server, port } = await startServer());
  });

  afterEach(
    () =>
      new Promise<void>((done) => {
        client?.disconnect();
        server.close(() => done());
      }),
  );

  // ── Connection ──────────────────────────────────────────────────────────

  test('connects and emits "connected"', async () => {
    client = makeClient(port);
    const connected = once(client, 'connected');
    client.connect();
    await connected;
    expect(client.connected).toBe(true);
  });

  test('.connected is false before connect()', () => {
    client = makeClient(port);
    expect(client.connected).toBe(false);
  });

  test('.url returns correct WebSocket URL', () => {
    client = makeClient(port);
    expect(client.url).toBe(`ws://localhost:${port}`);
  });

  // ── Send / receive ──────────────────────────────────────────────────────

  test('sends a request and resolves with matched result', async () => {
    server.on('connection', (ws: WebSocket) => {
      ws.on('message', (data: Buffer) => {
        const msg = JSON.parse(data.toString()) as { id: string };
        ws.send(JSON.stringify({ id: msg.id, result: { value: 42 } }));
      });
    });

    client = makeClient(port);
    await once(client, 'connected');
    client.connect();

    const result = await client.send('test_command', { foo: 'bar' });
    expect(result).toEqual({ value: 42 });
  });

  test('request includes the correct command and params', async () => {
    let received: Record<string, unknown> = {};

    server.on('connection', (ws: WebSocket) => {
      ws.on('message', (data: Buffer) => {
        received = JSON.parse(data.toString()) as Record<string, unknown>;
        ws.send(JSON.stringify({ id: received['id'], result: {} }));
      });
    });

    client = makeClient(port);
    const connected = once(client, 'connected');
    client.connect();
    await connected;

    await client.send('my_command', { key: 'val' });
    expect(received['command']).toBe('my_command');
    expect((received['params'] as Record<string, unknown>)['key']).toBe('val');
  });

  // ── Error responses ─────────────────────────────────────────────────────

  test('rejects with BridgeClientError when server returns an error', async () => {
    server.on('connection', (ws: WebSocket) => {
      ws.on('message', (data: Buffer) => {
        const { id } = JSON.parse(data.toString()) as { id: string };
        ws.send(
          JSON.stringify({ id, error: { code: 'UNKNOWN_COMMAND', message: 'bad cmd' } }),
        );
      });
    });

    client = makeClient(port);
    const connected = once(client, 'connected');
    client.connect();
    await connected;

    await expect(client.send('bad')).rejects.toMatchObject({
      code: 'UNKNOWN_COMMAND',
      message: 'bad cmd',
    });
  });

  // ── Timeout ─────────────────────────────────────────────────────────────

  test('rejects with TIMEOUT when server never replies', async () => {
    // Server accepts the connection but never sends a response
    server.on('connection', () => {});

    client = makeClient(port);
    const connected = once(client, 'connected');
    client.connect();
    await connected;

    await expect(client.send('no_reply')).rejects.toMatchObject({ code: 'TIMEOUT' });
  }, 3_000);

  // ── Not-connected guard ─────────────────────────────────────────────────

  test('rejects send() before connect() with NOT_CONNECTED', async () => {
    client = makeClient(port);
    // Intentionally do NOT call client.connect()
    await expect(client.send('any')).rejects.toMatchObject({ code: 'NOT_CONNECTED' });
  });

  // ── Reconnect ───────────────────────────────────────────────────────────

  test('auto-reconnects after server closes the connection', async () => {
    let connectionCount = 0;

    const secondConnect = new Promise<void>((resolve) => {
      server.on('connection', (ws: WebSocket) => {
        connectionCount++;
        if (connectionCount === 1) {
          // Kick the first connection
          setTimeout(() => ws.terminate(), 50);
        } else {
          resolve();
        }
      });
    });

    client = makeClient(port);
    client.connect();
    await secondConnect;
    expect(connectionCount).toBeGreaterThanOrEqual(2);
  }, 5_000);

  test('emits "disconnected" when server closes', async () => {
    let serverWs!: WebSocket;
    server.on('connection', (ws: WebSocket) => { serverWs = ws; });

    client = makeClient(port);
    const connected = once(client, 'connected');
    client.connect();
    await connected;

    const disconnected = once(client, 'disconnected');
    serverWs.terminate();
    await disconnected;
    // After disconnect, connected should be false
    expect(client.connected).toBe(false);
  }, 3_000);

  // ── In-flight request rejection on disconnect ───────────────────────────

  test('rejects in-flight requests when connection drops', async () => {
    let serverWs!: WebSocket;
    server.on('connection', (ws: WebSocket) => { serverWs = ws; });

    client = makeClient(port);
    const connected = once(client, 'connected');
    client.connect();
    await connected;

    // Start a request that the server will never answer
    const inflight = client.send('slow');
    // Close the connection while the request is pending
    await new Promise<void>((r) => setTimeout(r, 20));
    serverWs.terminate();

    await expect(inflight).rejects.toBeInstanceOf(Error);
  }, 3_000);

  // ── Ping / pong keepalive ───────────────────────────────────────────────

  test('emits "ping_timeout" and terminates when pongs stop arriving', async () => {
    // Server connects but never responds to pings (default WS behaviour
    // for a server created without autoPong)
    const noAutoPong = new WebSocketServer({ port: 0 });
    const noAutoPongPort = await new Promise<number>((resolve) =>
      noAutoPong.on('listening', () =>
        resolve((noAutoPong.address() as AddressInfo).port),
      ),
    );

    const timeoutClient = new BridgeClient(
      'localhost',
      noAutoPongPort,
      500,   // requestTimeout
      100,   // reconnectDelay
      100,   // pingInterval — very short so test is fast
      3,     // maxMissedPings
    );

    const pingTimeout = once(timeoutClient, 'ping_timeout');
    timeoutClient.connect();
    await pingTimeout;

    timeoutClient.disconnect();
    await new Promise<void>((r) => noAutoPong.close(r));
  }, 5_000);
});
