import { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { lookupsApi } from '@/api/endpoints/explorer';
import { useAuthStore } from '@/store/authStore';
import { useFilterStore } from '@/store/filterStore';

/** True when the signed-in user is bound to a single hospital (non-admin with an organization). */
export function useHospitalLocked(): string | undefined {
  const user = useAuthStore((s) => s.user);
  if (!user?.organizationId || user.roles?.includes('Admin')) return undefined;
  return user.organizationId;
}

/**
 * The effective global filters. `rcm` is shaped for /api/rcm queries (providerId/from/to),
 * `explorer` for /api/data queries (hospitalId/from/to).
 */
export function useGlobalFilters() {
  const { hospitalId, from, to } = useFilterStore();
  const locked = useHospitalLocked();
  const effectiveHospital = locked ?? hospitalId;

  return useMemo(
    () => ({
      hospitalId: effectiveHospital,
      from,
      to,
      rcm: { providerId: effectiveHospital, from, to },
      explorer: { hospitalId: effectiveHospital, from, to },
    }),
    [effectiveHospital, from, to],
  );
}

export function useHospitals() {
  return useQuery({ queryKey: ['lookups', 'hospitals'], queryFn: lookupsApi.hospitals, staleTime: 5 * 60_000 });
}

export function useInsurers() {
  return useQuery({ queryKey: ['lookups', 'insurers'], queryFn: lookupsApi.insurers, staleTime: 5 * 60_000 });
}
