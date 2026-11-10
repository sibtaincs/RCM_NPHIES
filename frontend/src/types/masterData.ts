/** Shape returned by every master-data GetAll endpoint: `{ items, total }`. */
export interface PagedResult<T> {
  items: T[];
  total: number;
}

/** Query parameters accepted by every master-data GetAll endpoint. */
export interface PagedQuery {
  skip: number;
  take: number;
  searchTerm?: string;
}

// ---- Master data DTOs (mirror Application/DTOs/MasterDataDtos.cs) ----

export interface ServiceCodeMaster {
  id?: string;
  serviceCode: string;
  serviceName: string;
  serviceDescription?: string;
  serviceCategory: string;
  nphiesServiceCode?: string;
  isNphiesMapped: boolean;
  defaultPrice: number;
  currencyCode: string;
  isRequiresAuthorization: boolean;
  isActive: boolean;
}

export interface DiagnosisCodeMaster {
  id?: string;
  diagnosisCode: string;
  diagnosisName: string;
  diagnosisCategory?: string;
  diagnosisType?: string;
  nphiesDiagnosisCode?: string;
  isNphiesMapped: boolean;
  severity?: string;
  isActive: boolean;
}

export interface MedicationCodeMaster {
  id?: string;
  medicationCode: string;
  medicationName: string;
  activeIngredient?: string;
  strength?: string;
  form?: string;
  unitPrice: number;
  currencyCode: string;
  isControlledSubstance: boolean;
  isActive: boolean;
}

export interface PayerMaster {
  id?: string;
  payerId: string;
  payerName: string;
  payerType?: string;
  nphiesConnectionStatus?: string;
  isNphiesMember: boolean;
  maxClaimsPerDay: number;
  maxClaimAmount?: number;
  currencyCode: string;
  isActive: boolean;
}

export interface DoctorMaster {
  id?: string;
  practitionerId: string;
  doctorCode: string;
  doctorName: string;
  specialization?: string;
qualificationDegree?: string;
  yearsOfExperience?: number;
  isConsultant: boolean;
  consultationFee?: number;
  currencyCode: string;
  isActive: boolean;
}
