const BASE = 'http://localhost:5070/api';

export interface AppSettings {
  useAzure: boolean;
  dropFolderPath: string;
  filesFolderPath: string;
  anthropicApiKey: string;
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
  return res.json();
}

export const settingsApi = {
  get:    () => request<AppSettings>('/settings'),
  update: (data: AppSettings) => request<AppSettings>('/settings', { method: 'PUT', body: JSON.stringify(data) }),
};
