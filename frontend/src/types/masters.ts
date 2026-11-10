/** Mirrors ApiService/MasterData (generic master-data maintenance, `api/masters`). */
import type { ExplorerChild, ExplorerColumnType } from './explorer';

export interface MasterField {
  key: string;
  label: string;
  type: ExplorerColumnType;
  required: boolean;
  readOnly: boolean;
  large: boolean;
  maxLength?: number | null;
  options?: string[] | null;
  /** Master table whose id this column stores (rendered as a searchable select). */
  lookup?: string | null;
  /** Extra lookup filter as "column=value", e.g. organizationType=ins. */
  lookupFilter?: string | null;
  summary: boolean;
}

export interface MasterTableInfo {
  table: string;
  group: string;
  label: string;
  /** Which NPHIES request element(s) this master feeds. */
  feeds: string;
  purpose: string;
  keyField: string;
  hospitalField?: string | null;
  defaultDateField?: string | null;
  dateFields: string[];
  display: string[];
  uniqueKeys: string[][];
  fields: MasterField[];
  children: ExplorerChild[];
  count?: number | null;
  canCreate: boolean;
  canUpdate: boolean;
  canDelete: boolean;
}

export interface MasterLookupItem {
  value: string;
  label: string;
}

export interface MasterDeleteResult {
  deleted: boolean;
  deactivated: boolean;
  referencedBy: string[];
}

export type MasterValues = Record<string, unknown>;
