import axios from 'axios';
import { apiClient } from '@/api/client';
import type { ExplorerRecord, ExplorerRow } from '@/types/explorer';
import type { MasterDeleteResult, MasterLookupItem, MasterTableInfo, MasterValues } from '@/types/masters';

/** Rows are listed through `explorerApi.query` (`GET api/data/{table}`); this covers metadata, lookups and writes. */
export const mastersApi = {
  catalog: (hospitalId?: string) =>
    apiClient
      .get<MasterTableInfo[]>('/masters/catalog', { params: { hospitalId: hospitalId || undefined } })
      .then((r) => r.data),

  meta: (table: string) => apiClient.get<MasterTableInfo>(`/masters/${table}/meta`).then((r) => r.data),

  get: (table: string, id: string) =>
    apiClient.get<ExplorerRecord>(`/masters/${table}/${encodeURIComponent(id)}`).then((r) => r.data),

  create: (table: string, values: MasterValues) =>
    apiClient.post<ExplorerRow>(`/masters/${table}`, values).then((r) => r.data),

  update: (table: string, id: string, values: MasterValues) =>
    apiClient.put<ExplorerRow>(`/masters/${table}/${encodeURIComponent(id)}`, values).then((r) => r.data),

  remove: (table: string, id: string) =>
    apiClient.delete<MasterDeleteResult>(`/masters/${table}/${encodeURIComponent(id)}`).then((r) => r.data),

  lookup: (table: string, opts: { search?: string; hospitalId?: string; take?: number; filters?: Record<string, string> }) => {
    const params: Record<string, string | number> = {};
    if (opts.search) params.search = opts.search;
    if (opts.hospitalId) params.hospitalId = opts.hospitalId;
    if (opts.take) params.take = opts.take;
    for (const [k, v] of Object.entries(opts.filters ?? {})) if (v) params[`f.${k}`] = v;
    return apiClient.get<MasterLookupItem[]>(`/masters/lookup/${table}`, { params }).then((r) => r.data);
  },
};

/** Per-field messages from a 400 `{ message, errors: { column: text } }` response. */
export function getMasterFieldErrors(error: unknown): Record<string, string> {
  if (!axios.isAxiosError(error) || error.response?.status !== 400) return {};
  const data = error.response.data as { errors?: Record<string, string | string[]> } | undefined;
  const result: Record<string, string> = {};
  for (const [k, v] of Object.entries(data?.errors ?? {})) result[k] = Array.isArray(v) ? v.join(' ') : String(v);
  return result;
}
