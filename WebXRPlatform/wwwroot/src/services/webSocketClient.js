/**
 * Low-latency real-time telemetry and state synchronization client
 */
export class WebSocketClient {
  constructor() {
    this.ws = null;
    this.listeners = new Map();
    this.isConnected = false;
  }

  connect(url = null) {
    const wsProtocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    const wsHost = url || `${wsProtocol}//${window.location.host}/ws/telemetry`;

    try {
      this.ws = new WebSocket(wsHost);

      this.ws.onopen = () => {
        this.isConnected = true;
        console.log('[WebSocketClient] Connected to telepresence telemetry stream.');
        this.emit('connectionChange', { connected: true });
      };

      this.ws.onmessage = (event) => {
        try {
          const msg = JSON.parse(event.data);
          this.emit(msg.type || 'message', msg.payload || msg);
        } catch (e) {
          this.emit('raw', event.data);
        }
      };

      this.ws.onclose = () => {
        this.isConnected = false;
        console.log('[WebSocketClient] Disconnected.');
        this.emit('connectionChange', { connected: false });
      };

      this.ws.onerror = (err) => {
        console.warn('[WebSocketClient] WebSocket error:', err);
      };
    } catch (err) {
      console.warn('[WebSocketClient] Could not establish WebSocket:', err.message);
    }
  }

  send(type, payload) {
    if (this.ws && this.isConnected && this.ws.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify({ type, payload }));
    }
  }

  on(event, callback) {
    if (!this.listeners.has(event)) {
      this.listeners.set(event, []);
    }
    this.listeners.get(event).push(callback);
  }

  emit(event, data) {
    const handlers = this.listeners.get(event) || [];
    for (const h of handlers) h(data);
  }
}

export const webSocketClient = new WebSocketClient();
export default webSocketClient;
