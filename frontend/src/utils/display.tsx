import { Tag, Typography } from 'antd';
import dayjs from 'dayjs';
import type { ReactNode } from 'react';

const DASH = '\u2014';

export function fmtDate(value?: string | null, withTime = false): string {
  if (!value) return DASH;
  const d = dayjs(value);
  return d.isValid() ? d.format(withTime ? 'YYYY-MM-DD HH:mm' : 'YYYY-MM-DD') : String(value);
}

export function fmtMoney(value?: number | null, currency = 'SAR'): string {
  if (value === null || value === undefined) return DASH;
  return `${value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ${currency}`;
}

export function fmtPct(value?: number | null): string {
  return value === null || value === undefined ? DASH : `${value.toFixed(1)}%`;
}

const norm = (s: string) => s.toLowerCase().replace(/[^a-z]/g, '');
const GREEN = new Set(['approved', 'complete', 'completed', 'active', 'paid', 'matched', 'eligible', 'accepted', 'success', 'sent', 'acknowledged', 'resolved', 'won', 'in-force', 'true', 'overturned', 'decided'].map(norm));
const RED = new Set(['rejected', 'error', 'failed', 'denied', 'cancelled', 'entered-in-error', 'expired', 'lost', 'discrepancy', 'unmatched', 'false', 'not-eligible', 'validation-failed', 'nullified', 'upheld', 'written-off', 'not-sent'].map(norm));
const ORANGE = new Set(['partial', 'partially-approved', 'partially-paid', 'queued', 'pending', 'pended', 'in-progress', 'submitted', 'open', 'draft', 'timeout', 'under-review', 'appealed', 'partially-matched', 'cancel-requested', 'resubmitted'].map(norm));

/** Coloured tag for any status/outcome string. */
export function statusTag(value?: string | boolean | null): ReactNode {
  if (value === null || value === undefined || value === '') return <Typography.Text type="secondary">{DASH}</Typography.Text>;
  const text = String(value);
  const key = norm(text);
  const color = GREEN.has(key) ? 'success' : RED.has(key) ? 'error' : ORANGE.has(key) ? 'warning' : 'default';
  return <Tag color={color}>{text}</Tag>;
}

/** Generic cell renderer for untyped explorer values. */
export function renderValue(value: unknown, type?: string): ReactNode {
  if (value === null || value === undefined || value === '') return <Typography.Text type="secondary">{DASH}</Typography.Text>;
  if (type === 'boolean' || typeof value === 'boolean') return statusTag(value as boolean);
  if (type === 'date') return fmtDate(String(value), true);
  if (type === 'decimal' && typeof value === 'number')
    return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  if (type === 'enum') return <Tag>{String(value)}</Tag>;
  if (typeof value === 'object') return <code>{JSON.stringify(value)}</code>;
  return String(value);
}
