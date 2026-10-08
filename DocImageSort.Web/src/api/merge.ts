import type { Document } from './documents';

const BASE = 'http://localhost:5070/api';

export const mergeApi = {
  /**
   * Uploads multiple files, merges them into one ordered PDF,
   * classifies the result, and returns the created Document.
   */
  merge: async (files: File[]): Promise<Document> => {
    const form = new FormData();
    for (const file of files) {
      form.append('files', file);
    }
    // Do NOT set Content-Type — browser sets multipart/form-data + boundary automatically
    const res = await fetch(`${BASE}/merge`, { method: 'POST', body: form });
    if (!res.ok) {
      const text = await res.text().catch(() => res.statusText);
      throw new Error(`${res.status}: ${text}`);
    }
    return res.json();
  },
};
