const BASE = 'http://localhost:5070/api';

export interface Document {
  id: number;
  originalFileName: string;
  renamedFileName: string;
  documentType: string;
  status: string;
  aiClassificationNotes?: string;
  documentDate?: string;
  sourcePath: string;
  filedPath: string;
  borrowerId?: number;
  borrowerName?: string;
  loanNumber?: string;
  createdDate: string;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  });
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    throw new Error(`${res.status}: ${text}`);
  }
  if (res.status === 204) return undefined as T;
  return res.json();
}

export const documentsApi = {
  getAll:       (status?: string, search?: string) => {
    const params = new URLSearchParams();
    if (status) params.set('status', status);
    if (search) params.set('search', search);
    const qs = params.toString() ? `?${params}` : '';
    return request<Document[]>(`/documents${qs}`);
  },
  getById:      (id: number) => request<Document>(`/documents/${id}`),
  correctType:  (id: number, documentType: string) =>
    request<Document>(`/documents/${id}/correct-type`, { method: 'PUT', body: JSON.stringify({ documentType }) }),
  assign:       (id: number, borrowerId: number) =>
    request<Document>(`/documents/${id}/assign`, { method: 'POST', body: JSON.stringify({ borrowerId }) }),
  getTypes:     () => request<string[]>('/documents/types'),
};
