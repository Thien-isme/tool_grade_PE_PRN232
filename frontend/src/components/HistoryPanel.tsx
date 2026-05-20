import React from 'react';
import { History, Calendar, Trash2, ChevronRight, Award, AlertCircle } from 'lucide-react';
import type { RunHistoryEntity } from '../types';

interface HistoryPanelProps {
  historyList: RunHistoryEntity[];
  onSelectRun: (run: RunHistoryEntity) => void;
  onDeleteRun: (id: string) => void;
  selectedRunId?: string;
}

export const HistoryPanel: React.FC<HistoryPanelProps> = ({
  historyList,
  onSelectRun,
  onDeleteRun,
  selectedRunId,
}) => {
  const getPassFailStats = (run: RunHistoryEntity) => {
    const total = run.results.length;
    const passed = run.results.filter((r) => r.isSuccess).length;
    const failed = total - passed;
    return { total, passed, failed };
  };

  const formatDate = (dateStr: string) => {
    const d = new Date(dateStr);
    return d.toLocaleString('vi-VN', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    });
  };

  return (
    <div className="card history-panel-card">
      <div className="card-header">
        <h2>📜 Lịch Sử Kiểm Thử Học Sinh</h2>
        <p className="card-subtitle">
          Danh sách các lần chạy thử và kết quả chấm điểm các bài đã nộp trước đó.
        </p>
      </div>

      <div className="history-body">
        {historyList.length === 0 ? (
          <div className="history-empty">
            <History className="empty-icon" />
            <p>Chưa có lịch sử chấm bài nào được lưu.</p>
          </div>
        ) : (
          <div className="history-list">
            {historyList.map((run) => {
              const { total, passed, failed } = getPassFailStats(run);
              const isSelected = selectedRunId === run.id;

              return (
                <div
                  key={run.id}
                  className={`history-item ${isSelected ? 'active' : ''}`}
                  onClick={() => onSelectRun(run)}
                >
                  <div className="history-main">
                    <div className="history-header">
                      <strong className="project-title" title={run.projectPath}>
                        {run.projectName}
                      </strong>
                      <span className="port-badge">Port {run.port}</span>
                    </div>

                    <div className="history-meta flex-center">
                      <Calendar className="mini-icon" />
                      <span>{formatDate(run.startedAt)}</span>
                    </div>

                    <div className="stats-indicator">
                      <span className="stat-passed flex-center">
                        <Award className="mini-icon text-success" />
                        <span>Đạt: {passed}/{total}</span>
                      </span>
                      {failed > 0 && (
                        <span className="stat-failed flex-center">
                          <AlertCircle className="mini-icon text-danger" />
                          <span>Lỗi: {failed}</span>
                        </span>
                      )}
                    </div>
                  </div>

                  <div className="history-actions" onClick={(e) => e.stopPropagation()}>
                    <button
                      onClick={() => onDeleteRun(run.id)}
                      className="btn btn-icon-only btn-danger-ghost delete-btn"
                      title="Xoá lịch sử này"
                    >
                      <Trash2 className="icon" />
                    </button>
                    <ChevronRight className="chevron-nav" />
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
