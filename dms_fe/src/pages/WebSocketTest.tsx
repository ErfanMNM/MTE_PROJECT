import { useState, useRef, useEffect } from 'react';
import { Button } from '@/components/ui/Button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Wifi, WifiOff, Send, Trash2, Copy } from 'lucide-react';

interface LogEntry {
  time: string;
  type: 'sent' | 'received' | 'info' | 'error';
  message: string;
}

export function WebSocketTest() {
  const [wsUrl, setWsUrl] = useState('ws://localhost:51883/ws/c1');
  const [isConnected, setIsConnected] = useState(false);
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [message, setMessage] = useState('');
  const wsRef = useRef<WebSocket | null>(null);

  const addLog = (type: LogEntry['type'], message: string) => {
    const time = new Date().toLocaleTimeString('en-US', { hour12: false });
    setLogs(prev => [{ time, type, message }, ...prev].slice(0, 200));
  };

  const connect = () => {
    if (wsRef.current) {
      wsRef.current.close();
    }

    addLog('info', `Connecting to ${wsUrl}...`);
    const ws = new WebSocket(wsUrl);

    ws.onopen = () => {
      setIsConnected(true);
      addLog('info', 'Connected!');
    };

    ws.onmessage = (event) => {
      addLog('received', event.data);
    };

    ws.onerror = (event) => {
      addLog('error', 'WebSocket error occurred');
    };

    ws.onclose = (event) => {
      setIsConnected(false);
      addLog('info', `Disconnected (code: ${event.code}, reason: ${event.reason || 'none'})`);
    };

    wsRef.current = ws;
  };

  const disconnect = () => {
    if (wsRef.current) {
      wsRef.current.close(1000, 'User disconnected');
      wsRef.current = null;
    }
  };

  const sendMessage = () => {
    if (!wsRef.current || wsRef.current.readyState !== WebSocket.OPEN) {
      addLog('error', 'Not connected');
      return;
    }

    const msg = message.trim() || JSON.stringify({ action: 'ping', timestamp: Date.now() });
    wsRef.current.send(msg);
    addLog('sent', msg);
    setMessage('');
  };

  const clearLogs = () => setLogs([]);

  const copyLogs = () => {
    const text = logs.map(l => `[${l.time}] ${l.type.toUpperCase()}: ${l.message}`).join('\n');
    navigator.clipboard.writeText(text);
  };

  const sendPing = () => {
    const msg = JSON.stringify({ action: 'ping', timestamp: Date.now() });
    if (wsRef.current?.readyState === WebSocket.OPEN) {
      wsRef.current.send(msg);
      addLog('sent', msg);
    }
  };

  useEffect(() => {
    return () => {
      if (wsRef.current) {
        wsRef.current.close();
      }
    };
  }, []);

  const getLogColor = (type: LogEntry['type']) => {
    switch (type) {
      case 'sent': return 'text-blue-400';
      case 'received': return 'text-green-400';
      case 'error': return 'text-red-400';
      default: return 'text-gray-400';
    }
  };

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>WebSocket Test</CardTitle>
            <Badge variant={isConnected ? 'success' : 'danger'}>
              {isConnected ? (
                <><Wifi className="w-3 h-3 mr-1" /> Connected</>
              ) : (
                <><WifiOff className="w-3 h-3 mr-1" /> Disconnected</>
              )}
            </Badge>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Connection URL */}
          <div className="flex gap-2">
            <input
              type="text"
              value={wsUrl}
              onChange={(e) => setWsUrl(e.target.value)}
              placeholder="ws://localhost:51883/ws/c1"
              className="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              disabled={isConnected}
            />
            {!isConnected ? (
              <Button onClick={connect}>Connect</Button>
            ) : (
              <Button variant="danger" onClick={disconnect}>Disconnect</Button>
            )}
          </div>

          {/* Quick Actions */}
          <div className="flex gap-2">
            <Button variant="secondary" onClick={sendPing}>
              Send Ping
            </Button>
            <Button variant="secondary" onClick={sendMessage} disabled={!isConnected}>
              <Send className="w-4 h-4 mr-1" /> Send
            </Button>
            <div className="flex-1">
              <input
                type="text"
                value={message}
                onChange={(e) => setMessage(e.target.value)}
                placeholder='{"action": "ping"}'
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                onKeyDown={(e) => e.key === 'Enter' && sendMessage()}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Log Panel */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Logs ({logs.length})</CardTitle>
            <div className="flex gap-2">
              <Button variant="ghost" size="sm" onClick={copyLogs}>
                <Copy className="w-4 h-4" />
              </Button>
              <Button variant="ghost" size="sm" onClick={clearLogs}>
                <Trash2 className="w-4 h-4" />
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="bg-gray-900 rounded-md p-4 h-80 overflow-y-auto font-mono text-sm">
            {logs.length === 0 ? (
              <p className="text-gray-500">No logs yet. Connect to start...</p>
            ) : (
              logs.map((log, i) => (
                <div key={i} className={`${getLogColor(log.type)} mb-1`}>
                  <span className="text-gray-500">[{log.time}]</span>{' '}
                  <span className="text-gray-600">[{log.type.toUpperCase()}]</span>{' '}
                  {log.message}
                </div>
              ))
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
