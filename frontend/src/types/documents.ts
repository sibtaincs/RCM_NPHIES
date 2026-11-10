/** Mirrors ApiService RcmDocumentsController (`api/rcm/documents`, `api/rcm/claims/{id}/documents`). */
import type { RcmQuery } from './rcm';

export interface DocumentQuery extends RcmQuery {
  claimId?: string;
  encounterId?: string;
  encounterNumber?: string;
  encounterType?: string;
  invoiceNumber?: string;
  invoiceFrom?: string;
  invoiceTo?: string;
  documentType?: string;
  unattached?: boolean;
  search?: string;
}

export interface DocumentClaimLink {
  claimId: string;
  claimNumber?: string | null;
  nphiesStatus?: string | null;
  sequence: number;
  sent: boolean;
}

export interface ClaimDocument {
  id: string;
  providerId: string;
  patientId: string;
  patientName?: string | null;
  encounterId?: string | null;
  encounterNumber?: string | null;
  encounterType?: string | null;
  invoiceNumber?: string | null;
  invoiceDate?: string | null;
  serviceDate?: string | null;
  documentType: string;
  title?: string | null;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  originalSizeBytes: number;
  encodedSizeBytes: number;
  isCompressed: boolean;
  sha256: string;
  storageProvider: string;
  source: string;
  externalId?: string | null;
  notes?: string | null;
  createdBy?: string | null;
  createdAt: string;
  claims: DocumentClaimLink[];
  inClaimPeriod?: boolean | null;
  duplicate: boolean;
}

export interface DocumentUploadMetadata {
  providerId?: string;
  patientId: string;
  encounterId?: string;
  encounterNumber?: string;
  encounterType?: string;
  invoiceNumber?: string;
  invoiceDate?: string;
  serviceDate?: string;
  documentType?: string;
  title?: string;
  notes?: string;
  claimId?: string;
}

export interface ClaimSizeBudgetItem {
  sequence: number;
  documentId?: string | null;
  title?: string | null;
  rawBytes: number;
  encodedBytes: number;
  source: 'document' | 'inline';
}

export interface ClaimSizeBudget {
  claimId: string;
  attachmentCount: number;
  rawAttachmentBytes: number;
  encodedAttachmentBytes: number;
  estimatedMessageBytes: number;
  warnBytes: number;
  limitBytes: number;
  remainingBytes: number;
  status: 'ok' | 'warning' | 'over-limit';
  attachments: ClaimSizeBudgetItem[];
}

export interface DocumentMigrationResult {
  scanned: number;
  migrated: number;
  skipped: number;
  bytesMoved: number;
  errors: string[];
}
