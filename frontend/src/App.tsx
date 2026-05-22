import { useState, useEffect, useRef } from 'react';
import { Activity, Award, FolderSearch, RefreshCw, Square, Play, Users, ChevronRight, Loader } from 'lucide-react';
import { batchApi, pe5Api } from './services/api';
import { useLogStream } from './hooks/useLogStream';
import { LogViewer } from './components/LogViewer';
import { EndpointList } from './components/EndpointList';
import { ResponseViewer } from './components/ResponseViewer';
import { PeGradingPanel } from './components/PeGradingPanel';
import type { BatchSessionStatus, StudentProjectStatus, EndpointInfo, ApiResponse, StudentTestResult, PeGradingResult } from './types';

function App() {
  const [session, setSession] = useState<BatchSessionStatus>({
    sessionFolderPath: '',
    sessionStatus: 'Idle',
    startedAt: null,
    students: [],
  });

  const [folderPath, setFolderPath] = useState('');
  const [browsing, setBrowsing] = useState(false);
  const [starting, setStarting] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  // Per-student data (keyed by studentName)
  const [selectedStudent, setSelectedStudent] = useState<StudentProjectStatus | null>(null);
  const [studentEndpoints, setStudentEndpoints] = useState<Record<string, EndpointInfo[]>>({});
  const [studentResults, setStudentResults] = useState<Record<string, StudentTestResult>>({});
  const [studentTestResults, setStudentTestResults] = useState<Record<string, Record<string, ApiResponse>>>({});
  const [scanningFor, setScanningFor] = useState<string | null>(null);
  const [testingFor, setTestingFor] = useState<string | null>(null);
  const [selectedEndpoint, setSelectedEndpoint] = useState<EndpointInfo | null>(null);
  const [peGrades, setPeGrades] = useState<Record<string, PeGradingResult>>({});
  const [gradingPe, setGradingPe] = useState(false);
  const [gradingPeAll, setGradingPeAll] = useState(false);

  const sessionActive = session.sessionStatus === 'Running' || session.sessionStatus === 'Starting';
  const { logs, clearLogs } = useLogStream(sessionActive || starting);
  const prevStatusRef = useRef(session.sessionStatus);

  // Poll session status
  useEffect(() => {
    const fetch = async () => {
      try {
        const s = await batchApi.getSession();
        setSession(s);
      } catch { }
    };
    fetch();
    const interval = setInterval(fetch, 2500);
    return () => clearInterval(interval);
  }, []);

  // Auto-scan when a student transitions to Running
  useEffect(() => {
    if (session.sessionStatus === 'Running') {
      session.students.forEach(async (s) => {
        if (s.status === 'Running' && !studentEndpoints[s.studentName]) {
          await handleScanStudent(s.studentName);
        }
      });
    }
    prevStatusRef.current = session.sessionStatus;
  }, [session.sessionStatus, session.students]);

  const handleBrowse = async () => {
    setBrowsing(true);
    setErrorMsg('');
    try {
      const res = await batchApi.browseFolder();
      if (!res.cancelled && res.path) {
        setFolderPath(res.path);
      } else if (res.error) {
        setErrorMsg(res.error);
      }
    } catch {
      setErrorMsg('Không thể mở hộp thoại hoặc hết thời gian chờ. Bạn có thể dán đường dẫn thư mục vào ô bên cạnh.');
    } finally {
      setBrowsing(false);
    }
  };

  const handleStart = async () => {
    if (!folderPath.trim()) return;
    setStarting(true);
    setErrorMsg('');
    setStudentEndpoints({});
    setStudentResults({});
    setStudentTestResults({});
    setPeGrades({});
    setSelectedStudent(null);
    try {
      const s = await batchApi.startBatch(folderPath.trim());
      setSession(s);
    } catch {
      setErrorMsg('Không thể kết nối đến backend service.');
    } finally {
      setStarting(false);
    }
  };

  const handleStopAll = async () => {
    try {
      const s = await batchApi.stopAll();
      setSession(s);
    } catch { }
  };

  const handleScanStudent = async (studentName: string) => {
    setScanningFor(studentName);
    try {
      const eps = await batchApi.scanStudent(studentName);
      setStudentEndpoints(prev => ({ ...prev, [studentName]: eps }));
    } catch { }
    finally { setScanningFor(null); }
  };

  const handleTestStudent = async (studentName: string) => {
    const eps = studentEndpoints[studentName] || [];
    if (!eps.length) return;
    setTestingFor(studentName);
    try {
      const result = await batchApi.testStudent(studentName, eps);
      setStudentResults(prev => ({ ...prev, [studentName]: result }));
      const map: Record<string, ApiResponse> = {};
      result.results.forEach(r => { map[r.endpointPath] = r; });
      setStudentTestResults(prev => ({ ...prev, [studentName]: map }));
    } catch { }
    finally { setTestingFor(null); }
  };

  const activeStudentName = selectedStudent?.studentName ?? '';
  const activeEndpoints = studentEndpoints[activeStudentName] ?? [];
  const activeTestResults = studentTestResults[activeStudentName] ?? {};
  const activeResult = selectedEndpoint ? activeTestResults[selectedEndpoint.path] : null;
  const activeScore = studentResults[activeStudentName];
  const activePeGrade = selectedStudent ? peGrades[activeStudentName] : null;

  const handleGradePe5 = async () => {
    if (!activeStudentName) return;
    setGradingPe(true);
    setErrorMsg('');
    try {
      const r = await pe5Api.gradeStudent(activeStudentName);
      setPeGrades(prev => ({ ...prev, [activeStudentName]: r }));
    } catch {
      setErrorMsg('Không thể chấm PE — kiểm tra backend.');
    } finally {
      setGradingPe(false);
    }
  };

  const handleGradePe5All = async () => {
    setGradingPeAll(true);
    setErrorMsg('');
    try {
      const summary = await pe5Api.gradeAll();
      const map: Record<string, PeGradingResult> = {};
      summary.results.forEach(r => { map[r.studentName] = r; });
      setPeGrades(map);
    } catch {
      setErrorMsg('Không thể chấm tất cả — kiểm tra backend.');
    } finally {
      setGradingPeAll(false);
    }
  };

  const handleExportPe5 = async () => {
    try {
      const blob = await pe5Api.exportCsv();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `PE_Paper5_Grades_${new Date().toISOString().slice(0, 10)}.csv`;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      setErrorMsg('Export CSV thất bại.');
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Running': return 'status-running';
      case 'Starting': return 'status-starting';
      case 'Failed': return 'status-failed';
      default: return 'status-pending';
    }
  };



  return (
    <div className="app-container">
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

      <main className="batch-layout">
        {/* ── LEFT PANEL: session control + student list ── */}
        <aside className="batch-sidebar">
          {/* Folder selection */}
          <div className="card">
            <div className="card-header">
              <h2>📂 Chọn Thư Mục Chứa Bài Học Sinh</h2>
              <p className="card-subtitle">Chọn thư mục cha (mỗi thư mục con = 1 SV) hoặc chọn trực tiếp 1 project (vd: Q1_API). Browse: kiểm tra taskbar nếu không thấy hộp thoại.</p>
            </div>
            <div className="input-group">
              <input
                type="text"
                placeholder="Ví dụ: G:\PRN232\Submissions"
                value={folderPath}
                onChange={e => setFolderPath(e.target.value)}
                disabled={sessionActive || starting}
                className="folder-input"
              />
              <button
                type="button"
                onClick={handleBrowse}
                disabled={sessionActive || starting || browsing}
                className="btn btn-secondary browse-btn"
              >
                {browsing ? <RefreshCw className="icon spinner" /> : <FolderSearch className="icon" />}
                <span>{browsing ? 'Chọn thư mục...' : 'Browse'}</span>
              </button>
            </div>
            {errorMsg && <div className="error-alert">{errorMsg}</div>}
            <div className="button-group">
              {sessionActive ? (
                <button onClick={handleStopAll} className="btn btn-danger stop-btn flex-center">
                  <Square className="icon-solid" />
                  <span>Dừng Tất Cả</span>
                </button>
              ) : (
                <button onClick={handleStart} disabled={!folderPath.trim() || starting} className="btn btn-primary start-btn flex-center">
                  {starting ? <Loader className="icon spinner" /> : <Play className="icon-solid" />}
                  <span>{starting ? 'Đang khởi động...' : 'Chạy Tất Cả Dự Án'}</span>
                </button>
              )}
            </div>
          </div>

          {/* Student list */}
          {session.students.length > 0 && (
            <div className="card student-list-card">
              <div className="card-header flex-between">
                <div>
                  <h2><Users className="inline-icon" /> Danh Sách Học Sinh</h2>
                  <p className="card-subtitle">{session.students.length} bài nộp được phát hiện</p>
                </div>
              </div>
              <div className="student-list">
                {session.students.map(s => {
                  const isSelected = selectedStudent?.studentName === s.studentName;
                  const result = studentResults[s.studentName];
                  const pe = peGrades[s.studentName];
                  return (
                    <div
                      key={s.studentName}
                      onClick={() => { setSelectedStudent(s); setSelectedEndpoint(null); }}
                      className={`student-item ${isSelected ? 'selected' : ''}`}
                    >
                      <div className="student-item-top flex-between">
                        <div>
                          <span className="student-name">{s.studentName}</span>
                          <span className="student-project">{s.projectName}</span>
                        </div>
                        <span className={`status-pill ${getStatusColor(s.status)}`}>{s.status}</span>
                      </div>
                      {(s.status === 'Running' || pe || result) && (
                        <div className="student-item-meta flex-between">
                          {s.activePort > 0 && <span className="port-info">:{s.activePort}</span>}
                          {pe ? (
                            <div className="student-score-badge pe-mini">
                              <span className="score-num">{pe.totalScore.toFixed(1)}</span>
                              <span className="score-den">/10</span>
                              <span className="score-detail"> Q1:{pe.q1Score} Q2:{pe.q2Score}</span>
                            </div>
                          ) : result ? (
                            <div className="student-score-badge">
                              <span className="score-num">{result.score.toFixed(1)}</span>
                              <span className="score-den">/10</span>
                              <span className="score-detail"> ({result.passed}/{result.total})</span>
                            </div>
                          ) : (
                            <div className="student-stats">
                              {studentEndpoints[s.studentName]?.length > 0 && (
                                <span className="ep-count">{studentEndpoints[s.studentName].length} endpoints</span>
                              )}
                            </div>
                          )}
                        </div>
                      )}
                      {s.status === 'Failed' && <p className="student-error">{s.error}</p>}
                      {isSelected && <ChevronRight className="student-arrow" />}
                    </div>
                  );
                })}
              </div>
            </div>
          )}

          {/* Log viewer */}
          <LogViewer logs={logs} onClear={clearLogs} />
        </aside>

        {/* ── RIGHT PANEL: selected student detail ── */}
        <section className="batch-detail">
          {!selectedStudent ? (
            <div className="card batch-empty-state">
              <Users className="empty-icon-large" />
              <h3>Chọn một học sinh từ danh sách</h3>
              <p>Sau khi chọn, bạn có thể xem endpoints và chạy kiểm thử tự động.</p>
            </div>
          ) : (
            <>
              {/* Student header */}
              <div className="card student-detail-header flex-between">
                <div>
                  <h2>🎓 {selectedStudent.studentName}</h2>
                  <p className="card-subtitle">
                    Dự án: <b>{selectedStudent.projectName}</b>
                    {selectedStudent.activePort > 0 && <> · Port: <code>{selectedStudent.activePort}</code></>}
                  </p>
                </div>
                {activePeGrade ? (
                  <div className="score-badge-large">
                    <Award className="award-icon-sm" />
                    <span className="score-value">{activePeGrade.totalScore.toFixed(1)}</span>
                    <span className="score-scale">/10</span>
                  </div>
                ) : activeScore ? (
                  <div className="score-badge-large">
                    <Award className="award-icon-sm" />
                    <span className="score-value">{activeScore.score.toFixed(1)}</span>
                    <span className="score-scale">/10</span>
                  </div>
                ) : null}
              </div>

              <PeGradingPanel
                result={activePeGrade}
                grading={gradingPe}
                gradingAll={gradingPeAll}
                onGrade={handleGradePe5}
                onGradeAll={handleGradePe5All}
                onExport={handleExportPe5}
                canGrade={!!selectedStudent}
                hasStudents={session.students.length > 0}
              />

              {/* Endpoint list for selected student */}
              <EndpointList
                endpoints={activeEndpoints}
                scanning={scanningFor === activeStudentName}
                testing={testingFor === activeStudentName}
                onScan={() => handleScanStudent(activeStudentName)}
                onTest={() => handleTestStudent(activeStudentName)}
                results={activeTestResults}
                selectedEndpoint={selectedEndpoint}
                onSelectEndpoint={ep => setSelectedEndpoint(ep)}
              />

              {/* Response viewer */}
              <ResponseViewer result={activeResult ?? null} />
            </>
          )}
        </section>
      </main>

      <footer className="app-footer">
        <p>© 2026 API Runner & Tester Core. Thiết kế chuyên nghiệp cho FPT Academic Grading.</p>
      </footer>
    </div>
  );
}

export default App;
