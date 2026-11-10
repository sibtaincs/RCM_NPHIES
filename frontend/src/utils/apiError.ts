import axios from 'axios';

/**
 * Extracts a human-readable message from an API error.
 * The backend returns `{ message: "..." }` for most failures.
 */
export function getApiErrorMessage(error: unknown, fallback = 'Something went wrong'): string {
  if (axios.isAxiosError(error)) {
 const data = error.response?.data as { message?: string; title?: string } | undefined;
    if (data?.message) return data.message;
    if (data?.title) return data.title;
    if (error.response?.status === 403) return 'You are not authorized to perform this action.';
return error.message || fallback;
  }
  if (error instanceof Error) return error.message;
  return fallback;
}
