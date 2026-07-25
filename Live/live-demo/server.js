const express = require('express');
const path = require('path');
const http = require('http');
const WebSocket = require('ws');

const PORT = 3000;
const app = express();
app.use(express.static(path.join(__dirname, 'public')));

const server = http.createServer(app);
let publisher = null;
const viewers = new Map();

const wss = new WebSocket.Server({ server, path: '/ws' });

function send(ws, data) {
  if (ws.readyState === WebSocket.OPEN) ws.send(JSON.stringify(data));
}

wss.on('connection', (ws) => {
  ws.on('message', (raw) => {
    let msg;
    try { msg = JSON.parse(raw); } catch { return; }

    if (msg.type === 'join') {
      if (msg.role === 'publisher') {
        publisher = ws;
        ws.role = 'publisher';
        viewers.forEach((v, id) => {
          send(v, { type: 'publisher-ready' });
          send(publisher, { type: 'viewer-joined', viewerId: id });
        });
      } else {
        ws.role = 'viewer';
        ws.clientId = msg.id;
        viewers.set(msg.id, ws);
        if (publisher) {
          send(ws, { type: 'publisher-ready' });
          send(publisher, { type: 'viewer-joined', viewerId: msg.id });
        } else {
          send(ws, { type: 'waiting' });
        }
      }
      return;
    }

    if (msg.type === 'offer') {
      const v = viewers.get(msg.viewerId);
      if (v) send(v, msg);
    } else if (msg.type === 'answer') {
      if (publisher) send(publisher, msg);
    } else if (msg.type === 'candidate') {
      if (msg.target === 'publisher' && publisher) send(publisher, msg);
      else if (msg.target === 'viewer') {
        const v = viewers.get(msg.viewerId);
        if (v) send(v, msg);
      }
    } else if (msg.type === 'danmaku' && msg.text) {
      const text = String(msg.text).slice(0, 50);
      const data = { type: 'danmaku', text, color: msg.color || '#fff' };
      viewers.forEach((v) => send(v, data));
    }
  });

  ws.on('close', () => {
    if (ws.role === 'publisher') {
      publisher = null;
      viewers.forEach((v) => send(v, { type: 'publisher-left' }));
    }
    if (ws.role === 'viewer' && ws.clientId) {
      viewers.delete(ws.clientId);
      if (publisher) send(publisher, { type: 'viewer-left', viewerId: ws.clientId });
    }
  });
});

server.listen(PORT, () => {
  console.log(`直播服务: http://localhost:${PORT}`);
});
