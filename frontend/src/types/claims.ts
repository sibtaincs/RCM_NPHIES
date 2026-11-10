export type ClaimReviewStage = 'technical' | 'medical';
export type ClaimQueueStage = ClaimReviewStage | 'ready';
export type ClaimReviewDecision = 'Approved' | 'Rejected';

export interface ClaimRecord {
  id: number | string;
  claimNumber: string;
  status: string;
  claimType: string;
  claimSubType?: string | null;
  use: string;
  priority?: string | null;
  total?: number | null;
  totalCurrency?: string | null;
  patientId: string;
  providerId?: string | null;
  insurerId?: string | null;
  coverageId?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface ClaimsReviewQueue {
  stage: ClaimReviewStage;
  count: number;
  claims: ClaimRecord[];
}

export interface ClaimReviewStatus {
  claimId: string;
  claimNumber: string;
  technicalReviewStatus: string;
  technicalReviewerId?: string | null;
  technicalReviewedAt?: string | null;
  technicalReviewNotes?: string | null;
  medicalReviewStatus: string;
  medicalReviewerId?: string | null;
  medicalReviewedAt?: string | null;
  medicalReviewNotes?: string | null;
  isApprovedForSubmission: boolean;
}

export interface ClaimReviewPayload {
  decision: ClaimReviewDecision;
  notes?: string;
}

export interface ClaimWorkflowDetails {
  id: string;
  claimNumber: string;
  status: string;
  claimType: string;
  claimSubType?: string | null;
  priority?: string | null;
  total?: number | null;
  totalCurrency?: string | null;
  patientId: string;
  patientName?: string | null;
  patientNameArabic?: string | null;
  familyNameArabic?: string | null;
  patientMrn?: string | null;
  patientNationalId?: string | null;
  patientNationalIdType?: string | null;
  patientDateOfBirth?: string | null;
  patientGender?: string | null;
  patientAgeYears?: number | null;
  createdAt: string;
  clinicName?: string | null;
  doctorReference?: string | null;
  doctorName?: string | null;
  doctorLicenseNumber?: string | null;
  coveragePolicyNumber?: string | null;
  coverageMemberId?: string | null;
  coverageStatus?: string | null;
  payerName?: string | null;
  payerPolicyName?: string | null;
  policyHolderNumber?: string | null;
  policyHolderName?: string | null;
  identificationNumber?: string | null;
  nationalityId?: string | null;
  coveragePlanCode?: string | null;
  coverageType?: string | null;
  coverageStartDate?: string | null;
  coverageEndDate?: string | null;
  preAuthorizationReference?: string | null;
  offlineApprovalDate?: string | null;
  offlineApprovalReference?: string | null;
  eligibilityOfflineReference?: string | null;
  nphiesApprovalIdentifier?: string | null;
  approvalUrl?: string | null;
  encounterInvoiceDate?: string | null;
  isNewborn?: boolean | null;
  encounterId?: string | null;
  encounterIdentifierValue?: string | null;
  encounterClass?: string | null;
  encounterStatus?: string | null;
  encounterAdmitSource?: string | null;
  encounterPeriodStart?: string | null;
  encounterPeriodEnd?: string | null;
  emergencyArrivalCode?: string | null;
  triageCategory?: string | null;
  dischargeDisposition?: string | null;
  emergencyDepartmentDisposition?: string | null;
  triageDate?: string | null;
  dischargeSpecialty?: string | null;
  chiefComplaint?: string | null;
  significantSigns?: string | null;
  investigationResult?: string | null;
  treatmentPlan?: string | null;
  patientHistory?: string | null;
  physicalExamination?: string | null;
  historyOfPresentIllness?: string | null;
  vitalSigns: ClaimWorkflowVital[];
  services: ClaimWorkflowService[];
  diagnoses: ClaimWorkflowDiagnosis[];
}

export interface ClaimWorkflowVital {
  name: string;
  value: string;
  unit?: string | null;
}

export interface ClaimWorkflowService {
  sequence: number;
  serviceCode: string;
  serviceName?: string | null;
  quantity?: number | null;
  serviceDate?: string | null;
  unitPrice?: number | null;
  net?: number | null;
  referenceInvoice?: string | null;
  approvalNumber?: string | null;
  approvedAmount?: number | null;
  approvedQuantity?: number | null;
  isApproved?: boolean | null;
  details: ClaimWorkflowServiceDetail[];
}

export interface ClaimWorkflowServiceDetail {
  sequence: number;
  serviceCode: string;
  serviceName?: string | null;
  quantity?: number | null;
  unitPrice?: number | null;
  net?: number | null;
}

export interface ClaimWorkflowDiagnosis {
  sequence: number;
  diagnosisCode: string;
  diagnosisDisplay?: string | null;
  diagnosisType?: string | null;
}

export interface ClaimsSubmissionQueue {
  count: number;
  claims: ClaimRecord[];
}