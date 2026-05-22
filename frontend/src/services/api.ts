import axios from 'axios';
import type { ProjectStatus, ProjectConfigEntity, EndpointInfo, RunHistoryEntity, BatchSessionStatus, StudentTestResult, PeGradingResult, PeBatchGradingSummary } from '../types';

const client = axios.create({
  baseURL: 'http://localhost:5155',
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000,
});

const browseClient = axios.create({
  baseURL: 'http://localhost:5155',
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 130000,
});

const longClient = axios.create({
  baseURL: 'http://localhost:5155',
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 600000, // 10 minutes
});

export const projectApi = {
  getStatus: async (): Promise<ProjectStatus> => {
    const res = await client.get<ProjectStatus>('/api/project/status');
    return res.data;
  },
  start: async (folderPath: string): Promise<ProjectStatus> => {
    const res = await client.post<ProjectStatus>('/api/project/start', { folderPath });
    return res.data;
  },
  stop: async (): Promise<ProjectStatus> => {
    const res = await client.post<ProjectStatus>('/api/project/stop');
    return res.data;
  },
  getRecent: async (): Promise<ProjectConfigEntity[]> => {
    const res = await client.get<ProjectConfigEntity[]>('/api/project/recent');
    return res.data;
  },
  browseFolder: async (): Promise<{ path?: string; cancelled: boolean; error?: string }> => {
    const res = await browseClient.get('/api/project/browse');
    return res.data;
  },
};

export const endpointApi = {
  scan: async (): Promise<EndpointInfo[]> => {
    const res = await client.post<EndpointInfo[]>('/api/endpoints/scan');
    return res.data;
  },
  test: async (endpoints: EndpointInfo[]): Promise<RunHistoryEntity> => {
    const res = await client.post<RunHistoryEntity>('/api/endpoints/test', endpoints);
    return res.data;
  },
  getHistory: async (): Promise<RunHistoryEntity[]> => {
    const res = await client.get<RunHistoryEntity[]>('/api/endpoints/history');
    return res.data;
  },
  deleteHistory: async (id: string): Promise<void> => {
    await client.delete(`/api/endpoints/history/${id}`);
  },
};

export const batchApi = {
  browseFolder: async (): Promise<{ path?: string; cancelled: boolean; error?: string }> => {
    const res = await browseClient.get('/api/batch/browse');
    return res.data;
  },
  getSession: async (): Promise<BatchSessionStatus> => {
    const res = await client.get<BatchSessionStatus>('/api/batch/session');
    return res.data;
  },
  startBatch: async (parentFolderPath: string): Promise<BatchSessionStatus> => {
    const res = await client.post<BatchSessionStatus>('/api/batch/start', { parentFolderPath });
    return res.data;
  },
  stopAll: async (): Promise<BatchSessionStatus> => {
    const res = await client.post<BatchSessionStatus>('/api/batch/stop');
    return res.data;
  },
  stopStudent: async (studentName: string): Promise<BatchSessionStatus> => {
    const res = await client.post<BatchSessionStatus>(`/api/batch/stop/${encodeURIComponent(studentName)}`);
    return res.data;
  },
  scanStudent: async (studentName: string): Promise<EndpointInfo[]> => {
    const res = await client.post<EndpointInfo[]>(`/api/batch/scan/${encodeURIComponent(studentName)}`);
    return res.data;
  },
  testStudent: async (studentName: string, endpoints: EndpointInfo[]): Promise<StudentTestResult> => {
    const res = await client.post<StudentTestResult>(`/api/batch/test/${encodeURIComponent(studentName)}`, endpoints);
    return res.data;
  },
};

export const pe5Api = {
  gradeStudent: async (studentName: string): Promise<PeGradingResult> => {
    const res = await longClient.post<PeGradingResult>(`/api/batch/pe5/grade/${encodeURIComponent(studentName)}`);
    return res.data;
  },
  gradeAll: async (): Promise<PeBatchGradingSummary> => {
    const res = await longClient.post<PeBatchGradingSummary>('/api/batch/pe5/grade-all');
    return res.data;
  },
  exportCsv: async (): Promise<Blob> => {
    const res = await longClient.post('/api/batch/pe5/export', {}, { responseType: 'blob' });
    return res.data;
  },
};
