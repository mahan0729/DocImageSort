const BASE = 'http://localhost:5070/api';

export interface ProcessingLog {
  id: number;
  documentId?: number;
  fileName: string;
  action: string;
  outcome: string;
  message: string;
  level: string;
  createdDate: string;
}

export const processingLogsApi = {
  getAll: (level?: string, search?: string) => {
    const params = new URLSearchParams();
    if (level)  params.set('level', level);
    if (search) params.set('search', search);
    const qs = params.toString() ? `?${params}` : '';
    return fetch(`${BASE}/processinglogs${qs}`)
      .then(r => r.ok ? r.json() as Promise<ProcessingLog[]> : Promise.reject(r.statusText));
  },
};
