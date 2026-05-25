import { Award, Download, FileSpreadsheet, Info, Loader, ClipboardCheck } from 'lucide-react';
import type { PeGradingResult } from '../types';

interface PeGradingPanelProps {
  result: PeGradingResult | null;
  grading: boolean;
  gradingAll: boolean;
  onGrade: () => void;
  onGradeAll: () => void;
  onExport: () => void;
  rubricExcelPath: string;
  onRubricExcelPathChange: (path: string) => void;
  onDownloadTemplate: () => void;
  canGrade: boolean;
  hasStudents: boolean;
}

export const PeGradingPanel: React.FC<PeGradingPanelProps> = ({
  result,
  grading,
  gradingAll,
  onGrade,
  onGradeAll,
  onExport,
  rubricExcelPath,
  onRubricExcelPathChange,
  onDownloadTemplate,
  canGrade,
  hasStudents,
}) => {
  const canRunQ1Rubric = !!rubricExcelPath.trim();

  return (
    <div className="card pe-grading-card">
      <div className="card-header flex-between">
        <div>
          <h2><ClipboardCheck className="inline-icon" /> Chấm PE — Paper No. 5</h2>
          <p className="card-subtitle">Q1 API = 5 điểm · Q2 MVC = 5 điểm · Tổng 10 điểm</p>
        </div>
        {result && (
          <div className="score-badge-large pe-total-badge">
            <Award className="award-icon-sm" />
            <span className="score-value">{result.totalScore.toFixed(1)}</span>
            <span className="score-scale">/10</span>
          </div>
        )}
      </div>

      <div className="q1-rubric-box">
        <div className="q1-rubric-top flex-between">
          <div>
            <h3><FileSpreadsheet className="inline-icon" /> Rubric Excel Q1</h3>
            <p className="card-subtitle">Tool se doc Settings, inject MyCnn, reset SQL va so sanh response voi ExpectedJson.</p>
          </div>
          <button
            type="button"
            onClick={onDownloadTemplate}
            className="btn btn-secondary flex-center"
          >
            <Download className="icon" />
            <span>Lay template</span>
          </button>
        </div>
        <input
          type="text"
          className="folder-input rubric-input"
          value={rubricExcelPath}
          onChange={e => onRubricExcelPathChange(e.target.value)}
          placeholder="Vi du: D:\PRN232\Rubrics\PE5\Q1_Rubric.xlsx"
        />
        <div className="usage-guide">
          <Info className="inline-icon" />
          <span>Sheet Settings chi can ConnectionString va SqlScriptPath. Sheet Q1_TestCases khai bao Method, Path, Body, ExpectedStatus, ExpectedJson. Moi sinh vien duoc reset DB truoc khi cham.</span>
        </div>
      </div>

      <div className="pe-actions button-group">
        <button
          type="button"
          onClick={onGrade}
          disabled={!canGrade || !canRunQ1Rubric || grading || gradingAll}
          className="btn btn-primary flex-center"
        >
          {grading ? <Loader className="icon spinner" /> : <ClipboardCheck className="icon" />}
          <span>{grading ? 'Đang chấm...' : 'Chấm SV đang chọn'}</span>
        </button>
        <button
          type="button"
          onClick={onGradeAll}
          disabled={!hasStudents || !canRunQ1Rubric || grading || gradingAll}
          className="btn btn-secondary flex-center"
        >
          {gradingAll ? <Loader className="icon spinner" /> : <ClipboardCheck className="icon" />}
          <span>{gradingAll ? 'Đang chấm tất cả...' : 'Chấm tất cả'}</span>
        </button>
        <button
          type="button"
          onClick={onExport}
          disabled={!hasStudents || !canRunQ1Rubric || gradingAll}
          className="btn btn-secondary flex-center"
        >
          <Download className="icon" />
          <span>Export CSV</span>
        </button>
      </div>

      {result && (
        <div className="pe-scores-row">
          <div className="pe-score-box">
            <span className="pe-score-label">Q1 — Web API</span>
            <span className="pe-score-val">{result.q1Score.toFixed(1)} / {result.q1Max}</span>
          </div>
          <div className="pe-score-box">
            <span className="pe-score-label">Q2 — MVC/Razor</span>
            <span className="pe-score-val">{result.q2Score.toFixed(1)} / {result.q2Max}</span>
          </div>
        </div>
      )}

      {result && (
        <div className="pe-criteria">
          <h3>Tiêu chí Q1</h3>
          <ul className="criteria-list">
            {result.q1Criteria.map(c => (
              <li key={c.id} className={c.passed ? 'crit-pass' : 'crit-fail'}>
                <span className="crit-id">{c.id}</span>
                <span className="crit-desc">{c.description}</span>
                <span className="crit-pts">{c.earnedPoints}/{c.maxPoints}</span>
                <span className="crit-detail">{c.detail}</span>
              </li>
            ))}
          </ul>
          <h3>Tiêu chí Q2</h3>
          <ul className="criteria-list">
            {result.q2Criteria.map(c => (
              <li key={c.id} className={c.passed ? 'crit-pass' : 'crit-fail'}>
                <span className="crit-id">{c.id}</span>
                <span className="crit-desc">{c.description}</span>
                <span className="crit-pts">{c.earnedPoints}/{c.maxPoints}</span>
                <span className="crit-detail">{c.detail}</span>
              </li>
            ))}
          </ul>
        </div>
      )}

      {result?.message && <p className="error-alert">{result.message}</p>}
    </div>
  );
};
