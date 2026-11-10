import { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { explorerApi } from '@/api/endpoints/explorer';
import type { ExplorerTable } from '@/types/explorer';

/** Table metadata the current user may read (cached for the session). */
export function useExplorerTables() {
  const query = useQuery({ queryKey: ['explorer-tables'], queryFn: explorerApi.tables, staleTime: Infinity });
  const byName = useMemo(
    () => new Map<string, ExplorerTable>((query.data ?? []).map((t) => [t.name, t])),
    [query.data],
  );
  const byCategory = useMemo(() => {
    const groups = new Map<string, ExplorerTable[]>();
    for (const t of query.data ?? []) {
      if (!groups.has(t.category)) groups.set(t.category, []);
      groups.get(t.category)!.push(t);
    }
    return [...groups.entries()].sort(([a], [b]) => a.localeCompare(b));
  }, [query.data]);
  return { ...query, byName, byCategory };
}
