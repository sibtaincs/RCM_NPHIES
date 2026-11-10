-- ============================================================================
-- NPHIES CodeableConcept Data Import Scripts
-- Purpose: Populate the database with all NPHIES terminology data
-- ============================================================================

-- ============================================================================
-- STEP 1: Seed CodeSystem Table
-- ============================================================================

INSERT INTO [dbo].[CodeSystem] ([Url], [Version], [Name], [Title], [Definition], [Committee], [Oid], [SourceResource], [IsActive])
VALUES
-- HL7 CodeSystems
('http://hl7.org/fhir/fm-status', '4.0.1', 'FinancialResourceStatusCodes', 'Financial Resource Status Codes', 
 'This set of codes includes Status codes.', 'Financial Management  Work Group', 'fm-status', 'codesystem-fm-status.json', 1),

('http://terminology.hl7.org/CodeSystem/claim-type', '4.0.0', 'ClaimTypeCodes', 'Claim Type Codes', 
 'This code set includes Claim Type codes.', 'Financial Management  Work Group', 'claim-type', 'codesystem-claim-type.json', 1),

('http://hl7.org/fhir/claim-use', '4.0.0', 'ClaimUse', 'Use', 
 'The purpose of the Claim: predetermination, preauthorization, claim.', 'Financial Management  Work Group', 'claim-use', 'codesystem-claim-use.json', 1),

-- NPHIES CodeSystems
('http://nphies.sa/terminology/CodeSystem/claim-subtype', '1.0.0', 'ClaimSubType', 'Claim SubType', 
 'Claim SubType codes which are used to identify different types of claims within broader claim type.', 'nphies profiles committee', 'claim-subtype', NULL, 1),

('http://nphies.sa/terminology/CodeSystem/diagnosis-type', '1.0.0', 'DiagnosisType', 'Diagnosis Type', 
 'This code set defines a set of codes that can be used to express the role of a diagnosis.', 'nphies profiles committee', 'diagnosis-type', NULL, 1),

('http://nphies.sa/terminology/CodeSystem/diagnosis-on-admission', '1.0.0', 'DiagnosisOnAdmission', 'Diagnosis on Admission', 
 'Code to indicate whether or not a diagnosis was present at time of inpatient admission.', 'nphies profiles committee', 'diagnosis-on-admission', NULL, 1),

('http://nphies.sa/terminology/CodeSystem/fdi-tooth-surface', '1.0.0', 'SurfaceCodes', 'Surface Codes', 
 'This value set includes a list of the FDI tooth surface codes.', 'nphies profiles committee', 'fdi-tooth-surface', NULL, 1),

('http://nphies.sa/terminology/CodeSystem/fdi-oral-region', '1.0.0', 'FDIOralRegion', 'FDI Oral Region', 
 'This code set contains codes to indicate the FDI tooth region.', 'nphies profiles committee', 'fdi-oral-region', NULL, 1),

('http://nphies.sa/terminology/CodeSystem/benefit-category', '1.0.0', 'BenefitCategory', 'Benefit Category', 
 'A code to identify the general type of benefits under which products and services are provided.', 'nphies profiles committee', 'benefit-category', NULL, 1),

('http://nphies.sa/terminology/CodeSystem/benefit-type', '1.0.0', 'BenefitType', 'Benefit Type', 
 'Identifies the type of benefits such as: deductible, copay, visit.', 'nphies profiles committee', 'benefit-type', NULL, 1),

('http://nphies.sa/terminology/CodeSystem/bodysite', '1.0.0', 'BodySite', 'Body Site', 
 'This code set includes Specific and identified anatomical location of the service provided to the patient.', 'nphies profiles committee', 'body-site', NULL, 1),

('http://nphies.sa/terminology/CodeSystem/adjudication-reason', '1.0.0', 'AdjudicationReason', 'Adjudication Reason', 
 'A code supporting the understanding of the adjudication result and explaining variance from expected amount.', 'nphies profiles committee', 'adjudication-reason', NULL, 1),

('http://nphies.sa/terminology/CodeSystem/adjudication-error', '1.0.0', 'AdjudicationError', 'Adjudication Error', 
 'Errors encountered during the processing of the adjudication.', 'nphies profiles committee', 'adjudication-error', NULL, 1),

('http://nphies.sa/terminology/CodeSystem/route-of-admin', '1.0.0', 'RouteOfAdmin', 'Route of Administration', 
 'This code set provides route to administer medicine.', 'nphies profiles committee', 'route-of-admin', NULL, 1),

('http://nphies.sa/terminology/CodeSystem/practice-codes', '1.0.0', 'PracticeCodes', 'PracticeCodes', 
 'A code specifying the practitioner specialty.', 'nphies profiles committee', 'practice-codes', NULL, 1),

('http://nphies.sa/terminology/CodeSystem/rta-diagnosis', '1.0.0', 'RTADiagnosis', 'RTA Diagnosis', 
 'Road Traffic Accident diagnosis codes used for claims and authorizations.', 'nphies profiles committee', 'rta-diagnosis', NULL, 1),

('http://nphies.sa/terminology/CodeSystem/cause-of-death', '1.0.0', 'CauseOfDeath', 'Cause of Death', 
 'Codes identifying the cause of death for reporting and claim processing.', 'WHO', 'cause-of-death', NULL, 1),

('http://nphies.sa/terminology/CodeSystem/ksa-adjudication', '1.0.0', 'KSAAdjudicationCodes', 'KSAAdjudicationCodes', 
 'KSA adjudication codes used in addition to the HL7 code system codes.', 'nphies profiles committee', 'ksa-adjudication', NULL, 1),

('http://nphies.sa/terminology/CodeSystem/ksa-message-events', '1.0.0', 'KSAMessageEvents', 'KSAMessage Event', 
 'The Saudi codeset for FHIR message events.', 'nphies profiles committee', 'ksa-message-events', NULL, 1),

('http://nphies.sa/terminology/CodeSystem/adjudication-outcome', '1.0.0', 'AdjudicationOutcome', 'Adjudication Outcome', 
 'A code indicating the outcome of the adjudication such as rejected, partially approved/paid or approved/paid as submitted.', 'nphies profiles committee', 'adjudication-outcome', NULL, 1)
-- Add more CodeSystems as needed
;

-- ============================================================================
-- STEP 2: Seed ValueSet Table
-- ============================================================================

INSERT INTO [dbo].[ValueSet] ([Url], [Version], [Name], [Title], [Definition], [Committee], [Oid], [SourceResource], [IsActive])
VALUES
('http://nphies.sa/terminology/ValueSet/claim-type', '1.0.0', 'ClaimType', 'Claim Type', 
 'Claim Type codes used to indicate the style of claim to support the requirements of different discipline.', 'nphies profiles committee', 'claim-type', 'valueset-claim-type.json', 1),

('http://nphies.sa/terminology/ValueSet/claim-subtype', '1.0.0', 'ClaimSubType', 'Claim SubType', 
 'Claim SubType codes which are used to identify different types of claims within broader claim type.', 'nphies profiles committee', 'claim-subtype', NULL, 1),

('http://nphies.sa/terminology/ValueSet/diagnosis-type', '1.0.0', 'DiagnosisType', 'Diagnosis Type', 
 'This value set defines a set of codes that can be used to express the role of a diagnosis.', 'nphies profiles committee', 'diagnosis-type', NULL, 1),

('http://nphies.sa/terminology/ValueSet/benefit-category', '1.0.0', 'BenefitCategory', 'Benefit Category', 
 'Code to identify the business for which general type of benefits under which products and services are provided.', 'nphies profiles committee', 'benefit-category', NULL, 1),

('http://nphies.sa/terminology/ValueSet/benefit-type', '1.0.0', 'BenefitType', 'Benefit Type', 
 'Identifies the type of benefits such as: deductible, copay, visit.', 'nphies profiles committee', 'benefit-type', NULL, 1),

('http://nphies.sa/terminology/ValueSet/body-site', '1.0.0', 'BodySite', 'Body Site', 
 'This code set includes Specific and identified anatomical location of the service provided to the patient.', 'nphies profiles committee', 'body-site', NULL, 1),

('http://nphies.sa/terminology/ValueSet/fdi-tooth-surface', '1.0.0', 'FDISurfaceCodes', 'FDI Surface Codes', 
 'Tooth surface codes.', 'nphies profiles committee', 'fdi-tooth-surface', NULL, 1),

('http://nphies.sa/terminology/ValueSet/fdi-oral-region', '1.0.0', 'FDIToothRegion', 'FDI Tooth and Region', 
 'Codes indicating tooth numbers, quadrants, sextants, and arches.', 'nphies profiles committee', 'fdi-oral-region', NULL, 1),

('http://nphies.sa/terminology/ValueSet/practice-codes', '1.0.0', 'PracticeCodes', 'PracticeCodes', 
 'Clinical specialty of the clinician or provider who interacted with, treated, or provided a service to/for the patient.', 'nphies profiles committee', 'practice-codes', NULL, 1),

('http://nphies.sa/terminology/ValueSet/adjudication-reason', '1.0.0', 'AdjudicationReason', 'Adjudication Reason', 
 'A code supporting the understanding of the adjudication result and explaining variance from expected amount.', 'nphies profiles committee', 'adjudication-reason', NULL, 1),

('http://nphies.sa/terminology/ValueSet/adjudication-error', '1.0.0', 'AdjudicationError', 'Adjudication Error Codes', 
 'Errors encountered during the processing of the adjudication.', 'nphies profiles committee', 'adjudication-error', NULL, 1),

('http://nphies.sa/terminology/ValueSet/route-of-admin', '1.0.0', 'RouteOfAdmin', 'Route of Admin', 
 'This code set provides route to administer medicine.', 'nphies profiles committee', 'route-of-admin', NULL, 1),

('http://nphies.sa/terminology/ValueSet/institutional-billing', '1.0.0', 'InstitutionalBilling', 'Institutional Billing', 
 'The value set containing the codes used for Institutional billing.', 'nphies profiles committee', 'institutional-billing', NULL, 1),

('http://nphies.sa/terminology/ValueSet/professional-billing', '1.0.0', 'ProfessionalBilling', 'Professional Billing', 
 'The value set containing the codes used for Professional billing.', 'nphies profiles committee', 'professional-billing', NULL, 1),

('http://nphies.sa/terminology/ValueSet/pharmacy-billing', '1.0.0', 'PharmacyBilling', 'Pharmacy Billing', 
 'The value set containing the codes used for Pharmacy billing period.', 'nphies profiles committee', 'pharmacy-billing', NULL, 1),

('http://nphies.sa/terminology/ValueSet/dental-billing', '1.0.0', 'DentalBilling', 'Dental Billing', 
 'The value set containing the codes used for Dental billing.', 'nphies profiles committee', 'dental-billing', NULL, 1),

('http://nphies.sa/terminology/ValueSet/vision-billing', '1.0.0', 'VisionBilling', 'Vision Billing', 
 'The value set containing the codes used for Vision billing period.', 'nphies profiles committee', 'vision-billing', NULL, 1),

('http://nphies.sa/terminology/ValueSet/cause-of-death', '1.0.0', 'CauseOfdeath', 'Cause of death', 
 'This Valueset includes Encounter Cause of Death.', 'nphies profiles committee', 'cause-of-death', NULL, 1),

('http://nphies.sa/terminology/ValueSet/rta-diagnosis', '1.0.0', 'RtaDiagnosis', 'Rta Diagnosis', 
 'This value set includes RTA diagnosis.', 'nphies profiles committee', 'Rta-Diagnosis', NULL, 1),

('http://nphies.sa/terminology/ValueSet/ksa-message-events', '1.0.0', 'KSAMessageEvents', 'KSA Message Events', 
 'The Saudi codeset for FHIR message events.', 'nphies profiles committee', 'ksa-message-events', NULL, 1),

('http://nphies.sa/terminology/ValueSet/adjudication-outcome', '1.0.0', 'AdjudicationOutcome', 'Adjudication Outcome', 
 'The processing status of the claim or preauthorization.', 'nphies profiles committee', 'adjudication-outcome', NULL, 1)
-- Add more ValueSets as needed
;

-- ============================================================================
-- STEP 3: Seed ValueSet_CodeSystem_Map (linking ValueSets to CodeSystems)
-- ============================================================================

INSERT INTO [dbo].[ValueSetCodeSystemMap] ([ValueSetId], [CodeSystemId], [Sequence], [IsActive])
SELECT 
    vs.[ValueSetId],
    cs.[CodeSystemId],
    1,
    1
FROM [dbo].[ValueSet] vs
INNER JOIN [dbo].[CodeSystem] cs ON 
    (vs.[Name] = cs.[Name] OR vs.[Url] LIKE '%' + cs.[Name] + '%')
WHERE vs.[IsActive] = 1 AND cs.[IsActive] = 1;

-- ============================================================================
-- STEP 4: Seed NphiesMessageType Table
-- ============================================================================

INSERT INTO [dbo].[NphiesMessageType] ([MessageType], [FhirResourceType], [Description], [IsActive])
VALUES
('eligibility-request', 'CoverageEligibilityRequest', 'Request for member eligibility verification', 1),
('eligibility-response', 'CoverageEligibilityResponse', 'Response with member eligibility information', 1),
('claim-request', 'Claim', 'Request for claim adjudication (institutional, professional, pharmacy, dental, vision)', 1),
('claim-response', 'ClaimResponse', 'Response with claim adjudication details', 1),
('priorauth-request', 'Claim', 'Request for prior authorization of services', 1),
('priorauth-response', 'ClaimResponse', 'Response with prior authorization decision', 1),
('cancel-request', 'Task', 'Request to cancel a previously submitted transaction', 1),
('cancel-response', 'Task', 'Response to cancel request', 1),
('communication-request', 'CommunicationRequest', 'Request for supporting information from provider', 1),
('communication', 'Communication', 'Provision of supporting information by provider', 1),
('payment-notice', 'PaymentNotice', 'Notice providing payment status', 1),
('payment-reconciliation', 'PaymentReconciliation', 'Reconciliation of payments to claims', 1),
('status-check', 'Task', 'Request to check status of prior submission', 1),
('status-response', 'Task', 'Response with status information', 1);

PRINT 'Initial data seeded successfully.';
