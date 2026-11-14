import type { RelatedClaim } from './rcm';

/** Mirrors ClaimEditDto (api/rcm/claims/{id}/edit). */
export interface ClaimEdit {
  id?: string | null;
  claimNumber?: string | null;
  use: string;
  claimType: string;
  claimSubType?: string | null;
  priority: string;
  payeeType?: string | null;
  patientId: string;
  coverageId: string;
  providerId: string;
  insurerId: string;
  practitionerId?: string | null;
  locationId?: string | null;
  encounterId?: string | null;
  billablePeriodStart?: string | null;
  billablePeriodEnd?: string | null;
  servicedPeriodStart?: string | null;
  servicedPeriodEnd?: string | null;
  preAuthorizationRef?: string | null;
  episodeIdentifierValue?: string | null;
  referralIdentifier?: string | null;
  prescriptionIdentifier?: string | null;
  accidentDate?: string | null;
  accidentType?: string | null;
  eligibilityOfflineReference?: string | null;
  eligibilityOfflineDate?: string | null;
  authorizationOfflineDate?: string | null;
  isShadowBilling: boolean;
  isTransfer?: boolean | null;
  /** Newborn using the mother's coverage (NPHIES extension-newborn). */
  isNewborn?: boolean;
  /** Read-only: the patient holding the selected coverage (the mother, for a newborn claim). */
  coverageHolderPatientId?: string | null;
  items: ClaimEditItem[];
  diagnoses: ClaimEditDiagnosis[];
  careTeam: ClaimEditCareTeam[];
  supportingInfo: ClaimEditSupportingInfo[];
  changeReason?: string | null;
  // read-only
  nphiesStatus?: string | null;
  editable?: boolean;
  lockedReason?: string | null;
  total?: number;
  totalTax?: number;
  totalPatientShare?: number;
  technicalReviewStatus?: string | null;
  medicalReviewStatus?: string | null;
  related?: RelatedClaim[];
}

export interface ClaimEditLens {
  product?: string | null;
  eye?: string | null;
  sphere?: number | null;
  cylinder?: number | null;
  axis?: number | null;
  prism?: number | null;
  prismBase?: string | null;
  add?: number | null;
  power?: number | null;
}

export interface ClaimEditItem {
  sequence: number;
  productOrServiceCode: string;
  productOrServiceSystem?: string | null;
  productOrServiceDisplay?: string | null;
  altProductOrServiceCode?: string | null;
  servicedDate?: string | null;
  quantity: number;
  unitPrice: number;
  factor?: number | null;
  taxRate?: number | null;
  patientShare?: number | null;
  careTeamSequence?: number | null;
  diagnosisSequences: number[];
  informationSequences: number[];
  bodySiteCode?: string | null;
  subSiteCode?: string | null;
  isPackage: boolean;
  isMaternity: boolean;
  patientInvoiceValue?: string | null;
  notes?: string | null;
  lenses: ClaimEditLens[];
  tax?: number;
  net?: number;
  detailCount?: number;
}

export interface ClaimEditDiagnosis {
  sequence: number;
  diagnosisCode: string;
  diagnosisDisplay?: string | null;
  diagnosisType?: string | null;
  onAdmissionCode?: string | null;
}

export interface ClaimEditCareTeam {
  sequence: number;
  practitionerId: string;
  practitionerName?: string | null;
  role?: string | null;
  qualification?: string | null;
}

export interface ClaimEditSupportingInfo {
  sequence: number;
  category: string;
  codeValue?: string | null;
  codeSystem?: string | null;
  stringValue?: string | null;
  quantityValue?: number | null;
  quantityUnit?: string | null;
  dateValue?: string | null;
  booleanValue?: boolean | null;
  attachmentTitle?: string | null;
  attachmentContentType?: string | null;
  attachmentData?: string | null;
  attachmentUrl?: string | null;
  hasAttachment?: boolean;
  removeAttachment?: boolean;
}

export interface ClaimHealthIssue {
  severity: 'Error' | 'Warning' | 'Info' | string;
  code: string;
  field?: string | null;
  message: string;
}

export interface ClaimHealth {
  claimId: string;
  claimNumber: string;
  ready: boolean;
  errorCount: number;
  warningCount: number;
  issues: ClaimHealthIssue[];
}

export type CorrectionAction =
  | 'edit'
  | 'preview'
  | 'status-check'
  | 'cancel'
  | 'nullify'
  | 'communicate'
  | 'resubmit'
  | 'update'
  | 'extend'
  | 'create-claim'
  | 'appeal';

export interface ClaimCorrectionOption {
  action: CorrectionAction;
  label: string;
  description: string;
  recommended: boolean;
}

export interface ClaimCorrectionOptions {
  claimId: string;
  claimNumber: string;
  nphiesStatus: string;
  editable: boolean;
  guidance: string;
  options: ClaimCorrectionOption[];
}

export interface ClaimFieldChange {
  field: string;
  before?: string | null;
  after?: string | null;
}

export interface ClaimAuditEntry {
  id: string;
  at: string;
  user?: string | null;
  action: string;
  reason?: string | null;
  changes: ClaimFieldChange[];
}

export interface ClaimVersionCompare {
  claimId: string;
  claimNumber: string;
  previousClaimId?: string | null;
  previousClaimNumber?: string | null;
  relationship?: string | null;
  changes: ClaimFieldChange[];
}
