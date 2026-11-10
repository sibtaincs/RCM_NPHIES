/** Format a number as a currency amount (defaults to SAR). */
export function formatCurrency(value?: number, currency = 'SAR'): string {
  if (value === null || value === undefined) return '—';
  return `${value.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })} ${currency}`;
}

/** Render a possibly-empty value with an em dash fallback. */
export function orDash(value?: string | number | null): string {
  if (value === null || value === undefined || value === '') return '—';
  return String(value);
}
