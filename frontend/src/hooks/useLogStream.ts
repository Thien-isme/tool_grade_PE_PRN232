import { useState, useEffect, useRef } from 'react';

export function useLogStream(active: boolean) {
  const [logs, setLogs] = useState<string[]>([]);
  const eventSourceRef = useRef<EventSource | null>(null);

  const clearLogs = () => setLogs([]);

  useEffect(() => {
    if (!active) {
      if (eventSourceRef.current) {
        eventSourceRef.current.close();
        eventSourceRef.current = null;
      }
      return;
    }

    // Kết nối đến SSE Endpoint của C# Backend
    const es = new EventSource('http://localhost:5155/api/logs/stream');
    eventSourceRef.current = es;

    es.onmessage = (event) => {
      setLogs((prev) => [...prev, event.data]);
    };

    es.onerror = () => {
      // Tự động đóng nếu có lỗi kết nối (Backend sập hoặc dừng)
      if (eventSourceRef.current) {
        eventSourceRef.current.close();
        eventSourceRef.current = null;
      }
    };

    return () => {
      if (eventSourceRef.current) {
        eventSourceRef.current.close();
        eventSourceRef.current = null;
      }
    };
  }, [active]);

  return { logs, clearLogs };
}
