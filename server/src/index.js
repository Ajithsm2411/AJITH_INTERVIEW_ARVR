import express from 'express';
import cors from 'cors';
import http from 'http';
import https from 'https';
import fs from 'fs';
import path from 'path';
import dotenv from 'dotenv';
import { fileURLToPath } from 'url';

import { dbManager } from './database/database.js';
import interviewRoutes from './routes/interviewRoutes.js';
import speechRoutes from './routes/speechRoutes.js';
import reportRoutes from './routes/reportRoutes.js';
import { rateLimiter } from './middleware/rateLimiter.js';
import { requestTimeout, globalErrorHandler } from './middleware/errorHandler.js';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Load environment variables
dotenv.config({ path: path.join(__dirname, '../.env') });

const app = express();
const PORT = parseInt(process.env.PORT || '5000', 10);
const USE_HTTPS = process.env.USE_HTTPS === 'true';

// CORS configuration for client WebXR origin
const allowedOrigins = (process.env.ALLOWED_ORIGINS || 'http://localhost:3000,http://localhost:5000,https://localhost:5001')
  .split(',')
  .map(o => o.trim());

app.use(cors({
  origin: (origin, callback) => {
    if (!origin || allowedOrigins.includes(origin) || origin.startsWith('http://localhost') || origin.startsWith('https://localhost')) {
      callback(null, true);
    } else {
      callback(null, true); // Permissive in development
    }
  },
  credentials: true
}));

// Body parsers with payload limits
app.use(express.json({ limit: '30mb' }));
app.use(express.urlencoded({ extended: true, limit: '30mb' }));

// Security & Timeout Middleware
app.use(rateLimiter);
app.use(requestTimeout(parseInt(process.env.REQUEST_TIMEOUT_MS || '15000', 10)));

// Health check endpoint
app.get('/api/health', (req, res) => {
  res.json({
    status: 'healthy',
    timestamp: new Date().toISOString(),
    database: dbManager.initialized ? 'connected' : 'initializing',
    protocol: USE_HTTPS ? 'https' : 'http'
  });
});

// Mount Routes
app.use('/api/interviews', interviewRoutes);
app.use('/api/speech', speechRoutes);
app.use('/api/reports', reportRoutes);

// Global Error Handler
app.use(globalErrorHandler);

import { WebSocketServer, WebSocket } from 'ws';

// In-memory rooms for WebRTC audio signaling & interview telepresence
const interviewRooms = new Map(); // roomId -> Set<{ ws, role, id }>

function handleWebSocketConnection(ws, req) {
  const url = new URL(req.url, 'http://localhost');
  const roomId = url.searchParams.get('room') || 'default-room';
  const role = url.searchParams.get('role') || 'candidate';
  const participantId = Math.random().toString(36).substring(2, 10);

  if (!interviewRooms.has(roomId)) {
    interviewRooms.set(roomId, new Set());
  }
  const room = interviewRooms.get(roomId);
  const clientInfo = { ws, role, id: participantId };
  room.add(clientInfo);

  console.log(`[WebSocket] Participant ${participantId} (${role}) joined room: ${roomId}. Total: ${room.size}`);

  // Notify client of successful connection
  ws.send(JSON.stringify({
    type: 'room-joined',
    payload: { roomId, participantId, role, participantCount: room.size }
  }));

  // Broadcast peer join to other clients in room
  for (const client of room) {
    if (client.ws !== ws && client.ws.readyState === WebSocket.OPEN) {
      client.ws.send(JSON.stringify({
        type: 'peer-joined',
        payload: { participantId, role }
      }));
    }
  }

  ws.on('message', (data) => {
    try {
      const msg = JSON.parse(data.toString());
      const { type, payload } = msg;

      // Broadcast WebRTC signaling (offer, answer, candidate) or interview events to room peers
      for (const client of room) {
        if (client.ws !== ws && client.ws.readyState === WebSocket.OPEN) {
          client.ws.send(JSON.stringify({
            type,
            senderId: participantId,
            senderRole: role,
            payload
          }));
        }
      }
    } catch (err) {
      console.warn('[WebSocket] Malformed message:', err.message);
    }
  });

  ws.on('close', () => {
    room.delete(clientInfo);
    if (room.size === 0) {
      interviewRooms.delete(roomId);
    } else {
      for (const client of room) {
        if (client.ws.readyState === WebSocket.OPEN) {
          client.ws.send(JSON.stringify({
            type: 'peer-left',
            payload: { participantId, role }
          }));
        }
      }
    }
    console.log(`[WebSocket] Participant ${participantId} left room: ${roomId}. Remaining: ${room.size}`);
  });
}

// Server startup
async function startServer() {
  try {
    await dbManager.connect();

    let server;
    if (USE_HTTPS) {
      const keyPath = process.env.SSL_KEY_PATH || path.join(__dirname, '../certs/key.pem');
      const certPath = process.env.SSL_CERT_PATH || path.join(__dirname, '../certs/cert.pem');
      if (fs.existsSync(keyPath) && fs.existsSync(certPath)) {
        const credentials = {
          key: fs.readFileSync(keyPath),
          cert: fs.readFileSync(certPath)
        };
        server = https.createServer(credentials, app);
        console.log('[Server] SSL certificates loaded. Running in secure HTTPS mode.');
      } else {
        console.warn('[Server] SSL certificates not found. Falling back to HTTP.');
        server = http.createServer(app);
      }
    } else {
      server = http.createServer(app);
    }

    // Attach WebSocket server for real-time WebRTC audio & state signaling
    const wss = new WebSocketServer({ noServer: true });
    wss.on('connection', handleWebSocketConnection);

    server.on('upgrade', (request, socket, head) => {
      const pathname = new URL(request.url, 'http://localhost').pathname;
      if (pathname === '/ws/interview' || pathname === '/ws/telemetry') {
        wss.handleUpgrade(request, socket, head, (ws) => {
          wss.emit('connection', ws, request);
        });
      } else {
        socket.destroy();
      }
    });

    server.listen(PORT, () => {
      console.log(`=======================================================`);
      console.log(`🚀 VR Telepresence AI Interview Server running on port: ${PORT}`);
      console.log(`🔗 REST API Base: ${USE_HTTPS ? 'https' : 'http'}://localhost:${PORT}/api`);
      console.log(`📡 WebSocket URL: ${USE_HTTPS ? 'wss' : 'ws'}://localhost:${PORT}/ws/interview`);
      console.log(`📋 Health Check:  ${USE_HTTPS ? 'https' : 'http'}://localhost:${PORT}/api/health`);
      console.log(`=======================================================`);
    });
  } catch (err) {
    console.error('[Server] Fatal startup failure:', err);
    process.exit(1);
  }
}

startServer();

export { app };
