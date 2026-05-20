export interface ParameterInfo {
  name: string;
  in: string;
  type: string;
  required: boolean;
  defaultValue: string;
}

export interface EndpointInfo {
  path: string;
  normalizedPath: string;
  method: string;
  summary: string;
  operationId: string;
  tag: string;
  parameters: ParameterInfo[];
  hasPathParams: boolean;
}

export interface ApiResponse {
  endpointPath: string;
  executedUrl: string;
  method: string;
  statusCode: number;
  body: string;
  responseTimeMs: number;
  headers: Record<string, string>;
  isSuccess: boolean;
  errorMessage: string | null;
  executedAt: string;
}

export interface RunHistoryEntity {
  id: string;
  projectPath: string;
  projectName: string;
  port: number;
  startedAt: string;
  stoppedAt: string | null;
  results: ApiResponse[];
}

export interface ProjectConfigEntity {
  folderPath: string;
  projectName: string;
  lastUsed: string;
}

export interface ProjectStatus {
  folderPath: string;
  clonedPath: string;
  projectName: string;
  status: 'Stopped' | 'Starting' | 'Running' | 'Failed';
  activePort: number;
  message: string;
  error: string;
  startedAt: string | null;
}

export interface StudentProjectStatus {
  studentName: string;
  folderPath: string;
  clonedPath: string;
  projectName: string;
  status: 'Pending' | 'Starting' | 'Running' | 'Failed' | 'Stopped';
  activePort: number;
  message: string;
  error: string;
  startedAt: string | null;
}

export interface BatchSessionStatus {
  sessionFolderPath: string;
  sessionStatus: 'Idle' | 'Starting' | 'Running' | 'Stopped';
  startedAt: string | null;
  students: StudentProjectStatus[];
}

export interface StudentTestResult {
  studentName: string;
  projectName: string;
  port: number;
  results: ApiResponse[];
  passed: number;
  failed: number;
  total: number;
  score: number;
}
