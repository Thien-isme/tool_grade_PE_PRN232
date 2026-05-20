import React, { useEffect, useRef } from 'react';
import { Terminal, Trash2 } from 'lucide-react';

interface LogViewerProps {
  logs: string[];
  onClear: () => void;
}

export const LogViewer: React.FC<LogViewerProps> = ({ logs, onClear }) => {
  const terminalEndRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    terminalEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [logs]);

  const parseLogLine = (line: string) => {
    if (line.includes('[SUCCESS]')) {
      return { className: 'log-success', text: line };
    }
    if (line.includes('[ERROR]') || line.includes('[TEST_FAILED]')) {
      return { className: 'log-error', text: line };
    }
    if (line.includes('[WARN]')) {
      return { className: 'log-warn', text: line };
    }
    if (line.includes('[TESTING]')) {
      return { className: 'log-testing', text: line };
    }
    if (line.includes('[TESTED]')) {
      return { className: 'log-tested', text: line };
    }
    return { className: 'log-info', text: line };
  };

  return (
    <div className="card log-viewer-card">
      <div className="card-header terminal-header">
        <div className="terminal-title">
          <Terminal className="icon" />
          <span>Luồng Log Chạy Thực Tế (Real-time Developer Console)</span>
        </div>
        <button onClick={onClear} className="btn btn-icon-only clear-btn" title="Xoá logs">
          <Trash2 className="icon" />
        </button>
      </div>

      <div className="terminal-body">
        {logs.length === 0 ? (
          <div className="terminal-empty">
            <span>&gt;_ Hệ thống đang chờ dự án được kích hoạt...</span>
          </div>
        ) : (
          <div className="terminal-lines">
            {logs.map((line, idx) => {
              const { className, text } = parseLogLine(line);
              return (
                <div key={idx} className={`terminal-line ${className}`}>
                  <span className="line-number">{(idx + 1).toString().padStart(3, '0')}</span>
                  <span className="line-content">{text}</span>
                </div>
              );
            })}
            <div ref={terminalEndRef} />
          </div>
        )}
      </div>
    </div>
  );
};
