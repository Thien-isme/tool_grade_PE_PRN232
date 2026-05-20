import { useState, useEffect, useRef } from 'react';
import { Activity, Award } from 'lucide-react';
import { ProjectSelector } from './components/ProjectSelector';
import { LogViewer } from './components/LogViewer';
import { EndpointList } from './components/EndpointList';
import { ResponseViewer } from './components/ResponseViewer';
import { HistoryPanel } from './components/HistoryPanel';
import { projectApi, endpointApi } from './services/api';
import { useLogStream } from './hooks/useLogStream';
import type { ProjectStatus, EndpointInfo, ApiResponse, RunHistoryEntity } from './types';

function App() {
  // State quản lý dự án target
  const [status, setStatus] = useState<ProjectStatus>({
    folderPath: '',
    clonedPath: '',
    projectName: '',
    status: 'Stopped',
    activePort: 0,
    message: '',
    error: '',
    startedAt: null,
  });

  // State danh sách endpoints quét được
  const [endpoints, setEndpoints] = useState<EndpointInfo[]>([]);
  // State kết quả test hiện tại: key = endpointPath, value = ApiResponse
  const [testResults, setTestResults] = useState<Record<string, ApiResponse>>({});
  // State endpoint đang được chọn xem chi tiết
  const [selectedEndpoint, setSelectedEndpoint] = useState<EndpointInfo | null>(null);
  // Lịch sử toàn bộ bài test
  const [historyList, setHistoryList] = useState<RunHistoryEntity[]>([]);
  const [selectedHistoryId, setSelectedHistoryId] = useState<string | undefined>(undefined);

  // Trạng thái loading phụ trợ
  const [scanning, setScanning] = useState(false);
  const [testing, setTesting] = useState(false);

  // Hook SSE Luồng log
  const { logs, clearLogs } = useLogStream(status.status === 'Running' || status.status === 'Starting');

  // Lưu trạng thái trước đó để nhận biết sự kiện chuyển tiếp trạng thái
  const prevStatusRef = useRef(status.status);

  // Lấy dữ liệu status & lịch sử ban đầu
  const fetchStatus = async () => {
    try {
      const res = await projectApi.getStatus();
      setStatus(res);
    } catch {
      // Fail silently
    }
  };

  const fetchHistory = async () => {
    try {
      const list = await endpointApi.getHistory();
      setHistoryList(list);
    } catch {
      // Fail silently
    }
  };

  useEffect(() => {
    fetchStatus();
    fetchHistory();

    const interval = setInterval(() => {
      fetchStatus();
    }, 2500);

    return () => clearInterval(interval);
  }, []);

  // Tự động quét Endpoint ngay sau khi dự án khởi động thành công (từ Starting -> Running)
  useEffect(() => {
    if (prevStatusRef.current !== 'Running' && status.status === 'Running') {
      // Đợi 1 giây để đảm bảo swagger endpoint hoàn toàn sẵn sàng trên IIS/Kestrel
      setTimeout(() => {
        handleScan();
      }, 1000);
    }
    prevStatusRef.current = status.status;
  }, [status.status]);

  const handleScan = async () => {
    setScanning(true);
    setEndpoints([]);
    setTestResults({});
    setSelectedEndpoint(null);
    try {
      const list = await endpointApi.scan();
      setEndpoints(list);
    } catch {
      // Fail silently
    } finally {
      setScanning(false);
    }
  };

  const handleTestAll = async () => {
    if (endpoints.length === 0) return;
    setTesting(true);
    setTestResults({});
    try {
      const history = await endpointApi.test(endpoints);
      // Chuyển đổi mảng kết quả test thành Map lưu state
      const resultsMap: Record<string, ApiResponse> = {};
      history.results.forEach((res) => {
        resultsMap[res.endpointPath] = res;
      });
      setTestResults(resultsMap);
      fetchHistory(); // tải lại lịch sử mới
    } catch {
      // Fail silently
    } finally {
      setTesting(false);
    }
  };

  const handleSelectHistory = (run: RunHistoryEntity) => {
    setSelectedHistoryId(run.id);
    // Khôi phục danh sách endpoints từ lịch sử
    const restoredEndpoints = run.results.map((r) => {
      // Đoán parameter từ URL thực tế
      const hasPathParams = r.executedUrl !== `http://localhost:${run.port}${r.endpointPath}`;
      return {
        path: r.endpointPath,
        normalizedPath: r.executedUrl.replace(`http://localhost:${run.port}`, ''),
        method: r.method || 'GET',
        summary: '',
        operationId: '',
        tag: 'Restored',
        parameters: [],
        hasPathParams,
      } as EndpointInfo;
    });

    setEndpoints(restoredEndpoints);

    // Khôi phục kết quả test
    const resultsMap: Record<string, ApiResponse> = {};
    run.results.forEach((r) => {
      resultsMap[r.endpointPath] = r;
    });
    setTestResults(resultsMap);
    setSelectedEndpoint(null);
  };

  const handleDeleteHistory = async (id: string) => {
    if (!window.confirm('Bạn có chắc chắn muốn xoá lịch sử chấm bài này?')) return;
    try {
      await endpointApi.deleteHistory(id);
      if (selectedHistoryId === id) {
        setSelectedHistoryId(undefined);
        setEndpoints([]);
        setTestResults({});
        setSelectedEndpoint(null);
      }
      fetchHistory();
    } catch {
      alert('Không thể xoá lịch sử.');
    }
  };

  // Tính điểm tổng hợp nhanh (để chấm bài học sinh)
  const getScoringStats = () => {
    if (endpoints.length === 0) return null;
    const total = endpoints.length;
    const passed = Object.values(testResults).filter((r) => r.isSuccess).length;
    const failed = total - passed;
    const score = ((passed / total) * 10).toFixed(1);
    return { total, passed, failed, score };
  };

  const scoreStats = getScoringStats();

  return (
    <div className="app-container">
      {/* Navigation Header */}
      <header className="app-header">
        <div className="brand flex-center">
          <Activity className="brand-logo" />
          <h1>API Runner & Autograder Tool</h1>
        </div>
        <div className="header-meta flex-center">
          <span className="tech-badge csharp">C# Backend</span>
          <span className="tech-badge react">React Frontend</span>
        </div>
      </header>

      {/* Main Grid Layout */}
      <main className="main-content">
        <div className="grid-left flex-column">
          {/* Chọn dự án */}
          <ProjectSelector
            status={status}
            onStatusChange={(next) => {
              setStatus(next);
            }}
            onStartLoading={() => {}}
          />

          {/* Điểm số nhanh (nếu đã test) */}
          {scoreStats && (
            <div className="card score-card flex-between">
              <div className="flex-center">
                <Award className="award-icon" />
                <div>
                  <h3>Kết Quả Chấm Bài (Tạm Tính)</h3>
                  <p className="card-subtitle">
                    Đạt <b>{scoreStats.passed}</b> trên tổng số <b>{scoreStats.total}</b> endpoint.
                  </p>
                </div>
              </div>
              <div className="score-badge">
                <span className="score-value">{scoreStats.score}</span>
                <span className="score-scale">/10</span>
              </div>
            </div>
          )}

          {/* Danh sách Endpoints */}
          <EndpointList
            endpoints={endpoints}
            scanning={scanning}
            testing={testing}
            onScan={handleScan}
            onTest={handleTestAll}
            results={testResults}
            selectedEndpoint={selectedEndpoint}
            onSelectEndpoint={(ep) => setSelectedEndpoint(ep)}
          />

          {/* Lịch sử */}
          <HistoryPanel
            historyList={historyList}
            onSelectRun={handleSelectHistory}
            onDeleteRun={handleDeleteHistory}
            selectedRunId={selectedHistoryId}
          />
        </div>

        <div className="grid-right flex-column">
          {/* Viewer kết quả phản hồi */}
          <ResponseViewer
            result={selectedEndpoint ? testResults[selectedEndpoint.path] : null}
          />

          {/* Terminal Logs */}
          <LogViewer logs={logs} onClear={clearLogs} />
        </div>
      </main>

      {/* Footer */}
      <footer className="app-footer">
        <p>© 2026 API Runner & Tester Core. Thiết kế chuyên nghiệp cho FPT Academic Grading.</p>
      </footer>
    </div>
  );
}

export default App;
