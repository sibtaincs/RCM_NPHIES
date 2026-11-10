import { apiClient } from '@/api/client';
import type {
  ExplorerPage,
  ExplorerQueryParams,
  ExplorerRecord,
  ExplorerTable,
  LookupOrganization,
} from '@/types/explorer';

function toParams(q: ExplorerQueryParams): Record<string, string | number> {
  const params: Record<string, string | number> = {};
  const { filters, ...rest } = q;
  for (const [k, v] of Object.entries(rest)) {
    if (v !== undefined && v !== null && v !== '') params[k] = v as string | number;
  }
  for (const [k, v] of Object.entries(filters ?? {})) {
    if (v !== undefined && v !== '') params[`f.${k}`] = v;
  }
  return params;
}

export const explorerApi = {
  tables: () => apiClient.get<ExplorerTable[]>('/data/tables').then((r) => r.data),

  query: (table: string, q: ExplorerQueryParams) =>
    apiClient.get<ExplorerPage>(`/data/${table}`, { params: toParams(q) }).then((r) => r.data),

  get: (table: string, id: string) =>
    apiClient.get<ExplorerRecord>(`/data/${table}/${encodeURIComponent(id)}`).then((r) => r.data),

  /** Downloads the filtered table (up to 50k rows) as CSV. */
  exportCsv: async (table: string, q: ExplorerQueryParams) => {
    const res = await apiClient.get<Blob>(`/data/${table}/export`, {
      params: toParams({ ...q, page: undefined, pageSize: undefined }),
      responseType: 'blob',
    });
    const url = URL.createObjectURL(res.data);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${table}-${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  },
};

export const lookupsApi = {
  hospitals: () => apiClient.get<LookupOrganization[]>('/lookups/hospitals').then((r) => r.data),
  insurers: () => apiClient.get<LookupOrganization[]>('/lookups/insurers').then((r) => r.data),
};
