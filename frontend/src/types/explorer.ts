/** Mirrors ApiService/DataExplorer (generic table explorer). */

export type ExplorerColumnType = 'string' | 'enum' | 'number' | 'decimal' | 'boolean' | 'date' | 'guid';

export interface ExplorerColumn {
  key: string;
  label: string;
  type: ExplorerColumnType;
  large: boolean;
  isKey: boolean;
}

export interface ExplorerChild {
  table: string;
  label: string;
  /** Column on the child table. */
  foreignKey: string;
  /** Column on this (parent) table. */
  principalKey: string;
}

export interface ExplorerTable {
  name: string;
  label: string;
  category: string;
  permission: string;
  keyField: string;
  columns: ExplorerColumn[];
  dateFields: string[];
  defaultDateField?: string | null;
  hospitalScoped: boolean;
  hospitalPath?: string | null;
  children: ExplorerChild[];
}

export type ExplorerRow = Record<string, unknown>;

export interface ExplorerPage {
  items: ExplorerRow[];
  total: number;
  page: number;
  pageSize: number;
}

export interface ExplorerChildCount extends ExplorerChild {
  value: unknown;
  count: number;
}

export interface ExplorerRecord {
  row: ExplorerRow;
  children: ExplorerChildCount[];
}

export interface ExplorerQueryParams {
  hospitalId?: string;
  from?: string;
  to?: string;
  dateField?: string;
  search?: string;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
  /** Exact-match column filters, sent as `f.{column}`. */
  filters?: Record<string, string | undefined>;
}

export interface LookupOrganization {
  id: string;
  name: string;
  licenseNumber?: string;
  nphiesId?: string | null;
  city?: string;
  status?: string;
  isActive?: boolean;
}
