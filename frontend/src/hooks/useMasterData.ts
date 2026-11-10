import { useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { createMasterDataApi } from '@/api/endpoints/masterData';
import { useDebounce } from '@/hooks/useDebounce';
import type { PagedResult } from '@/types/masterData';

/**
 * Encapsulates server-side pagination + debounced search for a master-data resource.
 * Returns everything a DataTable needs: rows, total, loading/error, and control setters.
 */
export function useMasterData<T>(resource: string) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebounce(search);

  const query = useQuery<PagedResult<T>>({
    queryKey: [resource, page, pageSize, debouncedSearch],
    queryFn: () =>
      createMasterDataApi<T>(resource).getAll({
        skip: (page - 1) * pageSize,
 take: pageSize,
        searchTerm: debouncedSearch,
    }),
    placeholderData: keepPreviousData, // avoids grid flicker while paging
  });

  return {
    rows: query.data?.items ?? [],
    total: query.data?.total ?? 0,
    isLoading: query.isLoading,
    isFetching: query.isFetching,
    error: query.error as Error | null,
    refetch: query.refetch,
    page,
    pageSize,
    search,
    setSearch: (value: string) => {
      setSearch(value);
      setPage(1); // reset to first page whenever the search term changes
    },
    setPage,
    setPageSize,
  };
}
