import React, { useState } from 'react';
import { Play, Square, FolderSearch, RefreshCw, AlertCircle } from 'lucide-react';
import type { ProjectStatus } from '../types';
import { projectApi } from '../services/api';

interface ProjectSelectorProps {
  status: ProjectStatus;
  onStatusChange: (status: ProjectStatus) => void;
  onStartLoading: () => void;
}

export const ProjectSelector: React.FC<ProjectSelectorProps> = ({
  status,
  onStatusChange,
  onStartLoading,
}) => {
  const [folderPath, setFolderPath] = useState(status.folderPath || '');
  const [browsing, setBrowsing] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  const handleBrowse = async () => {
    setBrowsing(true);
    setErrorMsg('');
    try {
      const res = await projectApi.browseFolder();
      if (!res.cancelled && res.path) {
        setFolderPath(res.path);
      } else if (res.error) {
        setErrorMsg('Lỗi mở hộp chọn thư mục.');
      }
    } catch {
      setErrorMsg('Không thể kết nối đến backend service.');
    } finally {
      setBrowsing(false);
    }
  };

  const handleRun = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!folderPath.trim()) return;

    onStartLoading();
    setErrorMsg('');
    try {
      const nextStatus = await projectApi.start(folderPath);
      onStatusChange(nextStatus);
      if (nextStatus.status === 'Failed') {
        setErrorMsg(nextStatus.error || 'Khởi động thất bại.');
      }
    } catch (err: any) {
      setErrorMsg('Không thể kết nối đến backend service.');
    }
  };

  const handleStop = async () => {
    onStartLoading();
    setErrorMsg('');
    try {
      const nextStatus = await projectApi.stop();
      onStatusChange(nextStatus);
    } catch {
      setErrorMsg('Không thể dừng dự án.');
    }
  };

  const isRunning = status.status === 'Running';
  const isStarting = status.status === 'Starting';

  return (
    <div className="card project-selector-card">
      <div className="card-header">
        <h2>📂 Chọn Dự Án Học Sinh Cần Kiểm Thử</h2>
        <p className="card-subtitle">
          Tool tự động clone dự án ra vùng tạm thời và đổi port chạy để bảo vệ bài gốc của học sinh.
        </p>
      </div>

      <form onSubmit={handleRun} className="selector-form">
        <div className="input-group">
          <input
            type="text"
            placeholder="Đường dẫn thư mục dự án (ví dụ: G:\PRN232\Q1_API)"
            value={folderPath}
            onChange={(e) => setFolderPath(e.target.value)}
            disabled={isRunning || isStarting}
            className="folder-input"
          />
          <button
            type="button"
            onClick={handleBrowse}
            disabled={isRunning || isStarting || browsing}
            className="btn btn-secondary browse-btn"
          >
            {browsing ? (
              <>
                <RefreshCw className="icon spinner" />
                <span>Đang mở...</span>
              </>
            ) : (
              <>
                <FolderSearch className="icon" />
                <span>Chọn Thư Mục</span>
              </>
            )}
          </button>
        </div>

        {errorMsg && (
          <div className="error-alert">
            <AlertCircle className="icon" />
            <span>{errorMsg}</span>
          </div>
        )}

        <div className="button-group">
          {isRunning || isStarting ? (
            <button
              type="button"
              onClick={handleStop}
              className="btn btn-danger stop-btn flex-center"
            >
              <Square className="icon-solid" />
              <span>Dừng Dự Án Hiện Tại</span>
            </button>
          ) : (
            <button
              type="submit"
              disabled={!folderPath.trim()}
              className="btn btn-primary start-btn flex-center"
            >
              <Play className="icon-solid" />
              <span>Chạy Dự Án & Quét Endpoint</span>
            </button>
          )}
        </div>
      </form>

      {status.status !== 'Stopped' && (
        <div className={`status-banner status-${status.status.toLowerCase()}`}>
          <div className="status-indicator">
            <span className="pulse-dot"></span>
            <strong>Trạng thái: {status.status}</strong>
          </div>
          {status.projectName && (
            <div className="status-detail">
              <span>Dự án: {status.projectName}</span>
              {status.activePort > 0 && (
                <span>
                  &nbsp;| Cổng mạng an toàn: <code>{status.activePort}</code>
                </span>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
};
