import React, { useState } from 'react';
import { ShieldCheck, PlayCircle, Layers, CheckCircle2, XCircle, Clock } from 'lucide-react';
import type { EndpointInfo, ApiResponse } from '../types';

interface EndpointListProps {
  endpoints: EndpointInfo[];
  scanning: boolean;
  testing: boolean;
  onScan: () => void;
  onTest: () => void;
  results: Record<string, ApiResponse>;
  selectedEndpoint: EndpointInfo | null;
  onSelectEndpoint: (ep: EndpointInfo) => void;
}

export const EndpointList: React.FC<EndpointListProps> = ({
  endpoints,
  scanning,
  testing,
  onScan,
  onTest,
  results,
  selectedEndpoint,
  onSelectEndpoint,
}) => {
  const [activeTab, setActiveTab] = useState<string>('ALL');

  // Gom nhóm các endpoints theo Swagger Tag
  const tags = ['ALL', ...Array.from(new Set(endpoints.map((ep) => ep.tag)))];

  const filteredEndpoints =
    activeTab === 'ALL' ? endpoints : endpoints.filter((ep) => ep.tag === activeTab);

  const getStatusIcon = (epPath: string) => {
    const result = results[epPath];
    if (!result) return <span className="badge badge-pending">Sẵn sàng</span>;
    if (result.isSuccess) {
      return (
        <span className="badge badge-success flex-center">
          <CheckCircle2 className="badge-icon" />
          <span>{result.statusCode} OK</span>
        </span>
      );
    }
    return (
      <span className="badge badge-danger flex-center">
        <XCircle className="badge-icon" />
        <span>{result.statusCode || 'ERR'}</span>
      </span>
    );
  };

  return (
    <div className="card endpoint-list-card">
      <div className="card-header flex-between">
        <div>
          <h2>🔌 Danh Sách API Endpoints Quét Được</h2>
          <p className="card-subtitle">
            Nhận dạng tự động tất cả endpoints (GET, POST, PUT, DELETE) của học sinh từ file cấu hình Swagger.
          </p>
        </div>
        <div className="action-buttons">
          <button
            onClick={onScan}
            disabled={scanning || testing}
            className="btn btn-secondary scan-btn"
          >
            {scanning ? 'Đang Quét...' : 'Quét Lại Swagger'}
          </button>
          <button
            onClick={onTest}
            disabled={testing || endpoints.length === 0}
            className="btn btn-primary test-btn flex-center"
          >
            <PlayCircle className="icon" />
            <span>{testing ? 'Đang Test...' : 'Kiểm Thử Tự Động'}</span>
          </button>
        </div>
      </div>

      {endpoints.length > 0 && (
        <div className="tag-tabs">
          {tags.map((tag) => (
            <button
              key={tag}
              onClick={() => setActiveTab(tag)}
              className={`tag-tab ${activeTab === tag ? 'active' : ''}`}
            >
              <Layers className="tab-icon" />
              <span>{tag}</span>
            </button>
          ))}
        </div>
      )}

      <div className="endpoints-body">
        {endpoints.length === 0 ? (
          <div className="endpoints-empty">
            <ShieldCheck className="shield-icon" />
            <h3>Chưa có dữ liệu API</h3>
            <p>Nhấp vào nút "Chạy Dự Án & Quét Endpoint" phía trên để bắt đầu quét Swagger.</p>
          </div>
        ) : (
          <div className="endpoints-grid">
            {filteredEndpoints.map((ep, idx) => {
              const isSelected = selectedEndpoint?.path === ep.path;
              const hasResult = !!results[ep.path];
              const result = results[ep.path];

              return (
                <div
                  key={idx}
                  onClick={() => onSelectEndpoint(ep)}
                  className={`endpoint-item ${isSelected ? 'selected' : ''} ${
                    hasResult ? (result.isSuccess ? 'passed' : 'failed') : ''
                  }`}
                >
                  <div className="endpoint-meta">
                    <span className={`method-badge method-${(ep.method || 'GET').toLowerCase()}`}>{(ep.method || 'GET')}</span>
                    <code className="endpoint-path" title={ep.path}>
                      {ep.path}
                    </code>
                  </div>

                  {ep.summary && <p className="endpoint-desc">{ep.summary}</p>}

                  {ep.hasPathParams && (
                    <div className="param-helper">
                      <span>👉 Tự động giả định tham số path: </span>
                      <code>{ep.normalizedPath}</code>
                    </div>
                  )}

                  <div className="endpoint-footer">
                    <div className="endpoint-status">{getStatusIcon(ep.path)}</div>
                    {result && (
                      <div className="endpoint-time flex-center">
                        <Clock className="mini-icon" />
                        <span>{result.responseTimeMs}ms</span>
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
};
