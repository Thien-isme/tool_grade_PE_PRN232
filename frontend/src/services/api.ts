import axios from 'axios';
import type { ProjectStatus, ProjectConfigEntity, EndpointInfo, RunHistoryEntity } from '../types';

const client = axios.create({
  baseURL: 'http://localhost:5155',
  headers: {
    'Content-Type': 'application/json',
  },
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
    const res = await client.get('/api/project/browse');
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
