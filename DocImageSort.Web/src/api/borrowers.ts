const BASE = 'http://localhost:5070/api';

export interface Borrower {
  id: number;
  lastName: string;
  firstName: string;
  loanNumber: string;
  isPrimaryBorrower: boolean;
  folderName: string;
  createdDate: string;
  updatedDate: string;
}

export interface BorrowerRequest {
  lastName: string;
  firstName: string;
  loanNumber: string;
  isPrimaryBorrower: boolean;
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

export const borrowersApi = {
  getAll: (search?: string) => {
    const qs = search ? `?search=${encodeURIComponent(search)}` : '';
    return request<Borrower[]>(`/borrowers${qs}`);
  },
  getById: (id: number) => request<Borrower>(`/borrowers/${id}`),
  create:  (data: BorrowerRequest) => request<Borrower>('/borrowers', { method: 'POST', body: JSON.stringify(data) }),
  update:  (id: number, data: BorrowerRequest) => request<Borrower>(`/borrowers/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  delete:  (id: number) => request<void>(`/borrowers/${id}`, { method: 'DELETE' }),
};
