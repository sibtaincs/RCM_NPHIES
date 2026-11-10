import { apiClient } from '@/api/client';
import type {
  ClaimDocument,
  ClaimSizeBudget,
  DocumentMigrationResult,
  DocumentQuery,
  DocumentUploadMetadata,
} from '@/types/documents';
import type { RcmPagedResult } from '@/types/rcm';

function clean(params: object): Record<string, unknown> {
  return Object.fromEntries(Object.entries(params).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export const documentsApi = {
  list: (q: DocumentQuery) =>
    apiClient.get<RcmPagedResult<ClaimDocument>>('/rcm/documents', { params: clean(q) }).then((r) => r.data),

  types: () => apiClient.get<string[]>('/rcm/documents/types').then((r) => r.data),

  get: (id: string) => apiClient.get<ClaimDocument>(`/rcm/documents/${id}`).then((r) => r.data),

  upload: (file: File, metadata: DocumentUploadMetadata) => {
    const form = new FormData();
    form.append('file', file);
    for (const [k, v] of Object.entries(clean(metadata))) form.append(k, String(v));
    return apiClient
      .post<ClaimDocument>('/rcm/documents', form, { headers: { 'Content-Type': 'multipart/form-data' } })
      .then((r) => r.data);
  },

  remove: (id: string) => apiClient.delete(`/rcm/documents/${id}`).then(() => undefined),

  migrateInline: (max = 200) =>
    apiClient.post<DocumentMigrationResult>('/rcm/documents/migrate-inline', {}, { params: { max } }).then((r) => r.data),

  /** Fetches the file with the bearer token and opens it in a new tab (or downloads it). */
  open: async (doc: Pick<ClaimDocument, 'id' | 'fileName'>, opts: { original?: boolean; download?: boolean } = {}) => {
    const res = await apiClient.get<Blob>(`/rcm/documents/${doc.id}/content`, {
      params: { original: opts.original || undefined, inline: !opts.download || undefined },
      responseType: 'blob',
    });
    const url = URL.createObjectURL(res.data);
    if (opts.download) {
      const a = document.createElement('a');
      a.href = url;
      a.download = doc.fileName;
      a.click();
    } else {
      window.open(url, '_blank', 'noopener');
    }
    setTimeout(() => URL.revokeObjectURL(url), 60_000);
  },

  claim: {
    list: (claimId: string) =>
      apiClient.get<RcmPagedResult<ClaimDocument>>(`/rcm/claims/${claimId}/documents`).then((r) => r.data),
    attach: (claimId: string, documentIds: string[], category?: string) =>
      apiClient.post<ClaimSizeBudget>(`/rcm/claims/${claimId}/documents`, { documentIds, category }).then((r) => r.data),
    detach: (claimId: string, documentId: string) =>
      apiClient.delete<ClaimSizeBudget>(`/rcm/claims/${claimId}/documents/${documentId}`).then((r) => r.data),
    candidates: (claimId: string) =>
      apiClient.get<ClaimDocument[]>(`/rcm/claims/${claimId}/document-candidates`).then((r) => r.data),
    sizeBudget: (claimId: string) =>
      apiClient.get<ClaimSizeBudget>(`/rcm/claims/${claimId}/size-budget`).then((r) => r.data),
  },
};
