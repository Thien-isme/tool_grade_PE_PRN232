import React, { useState } from 'react';
import { Send, FileCode, CheckCircle, AlertTriangle, ChevronDown, ChevronRight } from 'lucide-react';
import type { ApiResponse } from '../types';

interface ResponseViewerProps {
  result: ApiResponse | null;
}

export const ResponseViewer: React.FC<ResponseViewerProps> = ({ result }) => {
  if (!result) {
    return (
      <div className="card response-viewer-card empty-state">
        <Send className="send-icon" />
        <h3>Khung Phản Hồi API</h3>
        <p>Chọn một endpoint đã test ở bên trái để xem kết quả phản hồi chi tiết từ học sinh.</p>
      </div>
    );
  }

  const formattedJson = () => {
    try {
      const parsed = JSON.parse(result.body);
      return parsed;
    } catch {
      return null;
    }
  };

  return (
    <div className="card response-viewer-card">
      <div className="card-header">
        <div className="flex-between">
          <h2>📊 Chi Tiết Phản Hồi API</h2>
          <div className="status-badge-container">
            {result.isSuccess ? (
              <span className="badge badge-success flex-center">
                <CheckCircle className="badge-icon" />
                <span>{result.statusCode} OK</span>
              </span>
            ) : (
              <span className="badge badge-danger flex-center">
                <AlertTriangle className="badge-icon" />
                <span>{result.statusCode || 'FAILED'}</span>
              </span>
            )}
          </div>
        </div>
        <div className="executed-url-banner">
          <span className={`method-badge method-${(result.method || 'GET').toLowerCase()}`}>{(result.method || 'GET')}</span>
          <code className="url-text">{result.executedUrl}</code>
        </div>
      </div>

      <div className="response-metrics">
        <div className="metric-item">
          <span className="metric-label">Thời gian phản hồi</span>
          <span className="metric-value">{result.responseTimeMs} ms</span>
        </div>
        <div className="metric-item">
          <span className="metric-label">Kích thước phản hồi</span>
          <span className="metric-value">
            {new Blob([result.body]).size.toLocaleString()} bytes
          </span>
        </div>
        <div className="metric-item">
          <span className="metric-label">Loại nội dung</span>
          <span className="metric-value">
            {result.headers['content-type'] || result.headers['Content-Type'] || 'Không rõ'}
          </span>
        </div>
      </div>

      {result.errorMessage && (
        <div className="exception-box">
          <h4>🔴 Lỗi Kết Nối Tiến Trình:</h4>
          <p>{result.errorMessage}</p>
        </div>
      )}

      <div className="tabs-container">
        <h3 className="tab-title">
          <FileCode className="tab-icon" />
          <span>Nội Dung Phản Hồi (Response Body)</span>
        </h3>

        <div className="tab-panel response-body-panel">
          {formattedJson() ? (
            <InteractiveJsonView data={formattedJson()} />
          ) : (
            <pre className="raw-response-text">{result.body || '[Không có nội dung trả về]'}</pre>
          )}
        </div>
      </div>

      <div className="tabs-container font-small">
        <h3 className="tab-title">Response Headers</h3>
        <div className="headers-table">
          {Object.entries(result.headers).map(([key, value]) => (
            <div key={key} className="header-row">
              <span className="header-name">{key}:</span>
              <span className="header-value">{value}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

// --- Component phụ cho việc render JSON thông minh (Collapsible JSON Tree) ---
interface JsonNodeProps {
  name?: string;
  value: any;
  depth?: number;
}

const InteractiveJsonNode: React.FC<JsonNodeProps> = ({ name, value, depth = 0 }) => {
  const [collapsed, setCollapsed] = useState(false);

  const isObject = value !== null && typeof value === 'object';
  const indent = { paddingLeft: `${depth * 16}px` };

  if (!isObject) {
    let valueStr = JSON.stringify(value);
    let valueType: string = typeof value;
    if (value === null) valueType = 'null';

    return (
      <div className="json-node-leaf" style={indent}>
        {name && <span className="json-key">"{name}": </span>}
        <span className={`json-value json-val-${valueType}`}>{valueStr}</span>
      </div>
    );
  }

  const isArray = Array.isArray(value);
  const keys = Object.keys(value);
  const childrenCount = keys.length;

  return (
    <div className="json-node-branch">
      <div
        className="json-branch-header clickable"
        style={indent}
        onClick={() => setCollapsed(!collapsed)}
      >
        {collapsed ? (
          <ChevronRight className="chevron-icon" />
        ) : (
          <ChevronDown className="chevron-icon" />
        )}
        {name && <span className="json-key">"{name}": </span>}
        <span className="json-brackets">
          {isArray ? `Array [${childrenCount}]` : `Object {${childrenCount}}`}
        </span>
      </div>

      {!collapsed && (
        <div className="json-branch-children">
          {keys.map((key) => (
            <InteractiveJsonNode
              key={key}
              name={key}
              value={value[key]}
              depth={depth + 1}
            />
          ))}
        </div>
      )}
    </div>
  );
};

const InteractiveJsonView: React.FC<{ data: any }> = ({ data }) => {
  return (
    <div className="interactive-json-viewer">
      <InteractiveJsonNode value={data} />
    </div>
  );
};
