import { QueryClient } from '@tanstack/react-query';

/**
 * Central TanStack Query client.
 * Sensible defaults for an admin console: retry once, 30s stale time,
 * and no refetch on window focus (avoids surprise reloads in data grids).
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
   refetchOnWindowFocus: false,
    },
  },
});
