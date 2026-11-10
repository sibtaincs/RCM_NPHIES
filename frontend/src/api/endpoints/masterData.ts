import { apiClient } from '@/api/client';
import type { PagedQuery, PagedResult } from '@/types/masterData';

/**
 * Factory that builds a typed read client for any master-data controller.
 * All master-data endpoints share the same contract:
 *   GET /api/{resource}?skip=&take=&searchTerm=  ->  { items, total }
 */
export function createMasterDataApi<T>(resource: string) {
  return {
    resource,
    getAll: async (query: PagedQuery): Promise<PagedResult<T>> => {
      const { data } = await apiClient.get<PagedResult<T>>(`/${resource}`, {
 params: {
   skip: query.skip,
   take: query.take,
   searchTerm: query.searchTerm || undefined,
        },
      });
      // Defensive: guarantee a well-formed result even if the API omits fields.
      return { items: data?.items ?? [], total: data?.total ?? 0 };
    },
    getById: async (id: string): Promise<T> => {
      const { data } = await apiClient.get<T>(`/${resource}/${id}`);
      return data;
    },
  };
}

/** Route segments for each master-data controller (matches [Route("api/[controller]")]). */
export const masterDataResources = {
  serviceCodes: 'ServiceCodeMasters',
  diagnosisCodes: 'DiagnosisCodeMasters',
  medicationCodes: 'MedicationCodeMasters',
  payers: 'PayerMasters',
  doctors: 'DoctorMasters',
} as const;
