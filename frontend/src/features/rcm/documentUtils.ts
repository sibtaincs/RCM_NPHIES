import { useQuery } from '@tanstack/react-query';
import { documentsApi } from '@/api/endpoints/documents';

export const ENCOUNTER_TYPES = [
  { value: 'AMB', label: 'AMB · Outpatient' },
  { value: 'IMP', label: 'IMP · Inpatient' },
  { value: 'EMER', label: 'EMER · Emergency' },
  { value: 'SS', label: 'SS · Day case' },
  { value: 'HH', label: 'HH · Home health' },
  { value: 'VR', label: 'VR · Telemedicine' },
];

export const MAX_UPLOAD_MB = 25;

export const fmtBytes = (n?: number | null) => {
  if (!n) return '0 B';
  if (n < 1024) return `${n} B`;
  if (n < 1024 * 1024) return `${(n / 1024).toFixed(0)} KB`;
  return `${(n / 1024 / 1024).toFixed(2)} MB`;
};

export function useDocumentTypes() {
  return useQuery({ queryKey: ['rcm', 'document-types'], queryFn: documentsApi.types, staleTime: 60 * 60_000 });
}
