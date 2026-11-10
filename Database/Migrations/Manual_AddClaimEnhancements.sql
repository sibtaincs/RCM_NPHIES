-- ================================================================
-- Claims Table Enhancement Migration Script
-- Version: 1.0
-- Date: 2025-01-XX
-- Description: Adds accident info, funds reserve, referral/prescription 
--   references, pre-authorization ref, and billable period fields
--              to the Claims table
-- ================================================================

USE [NPhies_FHIR_Integration] -- Update with your database name
GO

BEGIN TRANSACTION;
GO

PRINT '========================================';
PRINT 'Starting Claims Table Enhancement';
PRINT '========================================';
PRINT '';

-- ================================================================
-- SECTION 1: ACCIDENT INFORMATION
-- ================================================================
PRINT 'Adding Accident Information fields...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Claims') AND name = 'AccidentDate')
BEGIN
    ALTER TABLE [dbo].[Claims] ADD [AccidentDate] DATETIME2 NULL;
    PRINT '  ? Added AccidentDate';
END
ELSE
    PRINT '  - AccidentDate already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Claims') AND name = 'AccidentType')
BEGIN
    ALTER TABLE [dbo].[Claims] ADD [AccidentType] NVARCHAR(50) NULL;
    PRINT '  ? Added AccidentType';
END
ELSE
    PRINT '  - AccidentType already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Claims') AND name = 'AccidentTypeSystem')
BEGIN
    ALTER TABLE [dbo].[Claims] ADD [AccidentTypeSystem] NVARCHAR(200) NULL;
    PRINT '  ? Added AccidentTypeSystem';
END
ELSE
    PRINT '  - AccidentTypeSystem already exists';

PRINT '';

-- ================================================================
-- SECTION 2: FUNDS RESERVE
-- ================================================================
PRINT 'Adding Funds Reserve fields...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Claims') AND name = 'FundsReserveCode')
BEGIN
    ALTER TABLE [dbo].[Claims] ADD [FundsReserveCode] NVARCHAR(50) NULL;
    PRINT '  ? Added FundsReserveCode';
END
ELSE
    PRINT '  - FundsReserveCode already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Claims') AND name = 'FundsReserveSystem')
BEGIN
    ALTER TABLE [dbo].[Claims] ADD [FundsReserveSystem] NVARCHAR(200) NULL;
    PRINT '  ? Added FundsReserveSystem';
END
ELSE
    PRINT '  - FundsReserveSystem already exists';

PRINT '';

-- ================================================================
-- SECTION 3: REFERRAL AND PRESCRIPTION REFERENCES
-- ================================================================
PRINT 'Adding Referral and Prescription Reference fields...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Claims') AND name = 'ReferralIdentifier')
BEGIN
    ALTER TABLE [dbo].[Claims] ADD [ReferralIdentifier] NVARCHAR(100) NULL;
    PRINT '  ? Added ReferralIdentifier';
END
ELSE
    PRINT '  - ReferralIdentifier already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Claims') AND name = 'PrescriptionIdentifier')
BEGIN
    ALTER TABLE [dbo].[Claims] ADD [PrescriptionIdentifier] NVARCHAR(100) NULL;
    PRINT '  ? Added PrescriptionIdentifier';
END
ELSE
    PRINT '  - PrescriptionIdentifier already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Claims') AND name = 'OriginalPrescriptionIdentifier')
BEGIN
    ALTER TABLE [dbo].[Claims] ADD [OriginalPrescriptionIdentifier] NVARCHAR(100) NULL;
    PRINT '  ? Added OriginalPrescriptionIdentifier';
END
ELSE
    PRINT '  - OriginalPrescriptionIdentifier already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Claims') AND name = 'PreAuthorizationRef')
BEGIN
    ALTER TABLE [dbo].[Claims] ADD [PreAuthorizationRef] NVARCHAR(100) NULL;
    PRINT '  ? Added PreAuthorizationRef';
END
ELSE
    PRINT '  - PreAuthorizationRef already exists';

PRINT '';

-- ================================================================
-- SECTION 4: BILLABLE PERIOD
-- ================================================================
PRINT 'Adding Billable Period fields...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Claims') AND name = 'BillablePeriodStart')
BEGIN
    ALTER TABLE [dbo].[Claims] ADD [BillablePeriodStart] DATETIME2 NULL;
    PRINT '  ? Added BillablePeriodStart';
END
ELSE
    PRINT '  - BillablePeriodStart already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Claims') AND name = 'BillablePeriodEnd')
BEGIN
  ALTER TABLE [dbo].[Claims] ADD [BillablePeriodEnd] DATETIME2 NULL;
    PRINT '  ? Added BillablePeriodEnd';
END
ELSE
    PRINT '  - BillablePeriodEnd already exists';

PRINT '';

-- ================================================================
-- SECTION 5: INDEXES FOR PERFORMANCE
-- ================================================================
PRINT 'Adding indexes for performance...';

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Claims_PreAuthorizationRef' AND object_id = OBJECT_ID('dbo.Claims'))
BEGIN
    CREATE INDEX IX_Claims_PreAuthorizationRef ON [dbo].[Claims]([PreAuthorizationRef]) WHERE [PreAuthorizationRef] IS NOT NULL;
  PRINT '  ? Created index IX_Claims_PreAuthorizationRef';
END
ELSE
    PRINT '  - Index IX_Claims_PreAuthorizationRef already exists';

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Claims_ReferralIdentifier' AND object_id = OBJECT_ID('dbo.Claims'))
BEGIN
    CREATE INDEX IX_Claims_ReferralIdentifier ON [dbo].[Claims]([ReferralIdentifier]) WHERE [ReferralIdentifier] IS NOT NULL;
    PRINT '  ? Created index IX_Claims_ReferralIdentifier';
END
ELSE
    PRINT '  - Index IX_Claims_ReferralIdentifier already exists';

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Claims_PrescriptionIdentifier' AND object_id = OBJECT_ID('dbo.Claims'))
BEGIN
    CREATE INDEX IX_Claims_PrescriptionIdentifier ON [dbo].[Claims]([PrescriptionIdentifier]) WHERE [PrescriptionIdentifier] IS NOT NULL;
    PRINT '  ? Created index IX_Claims_PrescriptionIdentifier';
END
ELSE
    PRINT '  - Index IX_Claims_PrescriptionIdentifier already exists';

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Claims_AccidentDate' AND object_id = OBJECT_ID('dbo.Claims'))
BEGIN
    CREATE INDEX IX_Claims_AccidentDate ON [dbo].[Claims]([AccidentDate]) WHERE [AccidentDate] IS NOT NULL;
    PRINT '  ? Created index IX_Claims_AccidentDate';
END
ELSE
    PRINT '  - Index IX_Claims_AccidentDate already exists';

PRINT '';

-- ================================================================
-- SECTION 6: ADD EXTENDED PROPERTIES (DOCUMENTATION)
-- ================================================================
PRINT 'Adding extended properties for documentation...';

-- Accident fields
EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Date when the accident occurred (quick reference). For detailed accident info, see ClaimAccident table.' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'Claims', 
    @level2type=N'COLUMN',
    @level2name=N'AccidentDate';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
  @value=N'Type of accident: MVA (Motor Vehicle Accident), WORK (Workplace), etc. Maps to FHIR Claim.accident.type' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'Claims', 
    @level2type=N'COLUMN',
    @level2name=N'AccidentType';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'System URL for accident type coding (e.g., http://terminology.hl7.org/CodeSystem/v3-ActCode)' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'Claims', 
    @level2type=N'COLUMN',
    @level2name=N'AccidentTypeSystem';

-- Funds Reserve fields
EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Funds reserve requested: patient, provider, none. Indicates who should hold funds in reserve. Maps to FHIR Claim.fundsReserve' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'Claims', 
    @level2type=N'COLUMN',
    @level2name=N'FundsReserveCode';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'System URL for funds reserve coding (e.g., http://terminology.hl7.org/CodeSystem/fundsreserve)' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'Claims', 
    @level2type=N'COLUMN',
    @level2name=N'FundsReserveSystem';

-- Reference fields
EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Reference to a ServiceRequest/referral document. Maps to FHIR Claim.referral' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'Claims', 
    @level2type=N'COLUMN',
    @level2name=N'ReferralIdentifier';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Reference to MedicationRequest/prescription. Maps to FHIR Claim.prescription' , 
@level0type=N'SCHEMA',
  @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'Claims', 
    @level2type=N'COLUMN',
    @level2name=N'PrescriptionIdentifier';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Reference to the original prescription being refilled. Maps to FHIR Claim.originalPrescription' , 
 @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'Claims', 
    @level2type=N'COLUMN',
    @level2name=N'OriginalPrescriptionIdentifier';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
  @value=N'Pre-authorization approval reference number (e.g., "136997701"). Maps to FHIR Claim.preAuthRef' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'Claims', 
    @level2type=N'COLUMN',
  @level2name=N'PreAuthorizationRef';

-- Billable Period fields
EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Start of billing period. May differ from service period. Maps to FHIR Claim.billablePeriod.start' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'Claims', 
    @level2type=N'COLUMN',
    @level2name=N'BillablePeriodStart';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'End of billing period. May differ from service period. Maps to FHIR Claim.billablePeriod.end' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
  @level1type=N'TABLE',
    @level1name=N'Claims', 
    @level2type=N'COLUMN',
    @level2name=N'BillablePeriodEnd';

PRINT '  ? Added extended properties';
PRINT '';

-- ================================================================
-- SECTION 7: VERIFICATION
-- ================================================================
PRINT 'Verifying changes...';
PRINT '';

DECLARE @ColumnCount INT;

SELECT @ColumnCount = COUNT(*)
FROM sys.columns 
WHERE object_id = OBJECT_ID('dbo.Claims') 
AND name IN (
    'AccidentDate', 'AccidentType', 'AccidentTypeSystem',
    'FundsReserveCode', 'FundsReserveSystem',
    'ReferralIdentifier', 'PrescriptionIdentifier', 'OriginalPrescriptionIdentifier', 'PreAuthorizationRef',
    'BillablePeriodStart', 'BillablePeriodEnd'
);

PRINT 'Columns added/verified: ' + CAST(@ColumnCount AS NVARCHAR(10)) + ' of 11';

IF @ColumnCount = 11
    PRINT '? All columns verified successfully!';
ELSE
  PRINT '? Warning: Not all columns were added. Please review the script output.';

PRINT '';

DECLARE @IndexCount INT;

SELECT @IndexCount = COUNT(*)
FROM sys.indexes 
WHERE object_id = OBJECT_ID('dbo.Claims')
AND name IN (
    'IX_Claims_PreAuthorizationRef',
    'IX_Claims_ReferralIdentifier',
    'IX_Claims_PrescriptionIdentifier',
    'IX_Claims_AccidentDate'
);

PRINT 'Indexes created/verified: ' + CAST(@IndexCount AS NVARCHAR(10)) + ' of 4';

IF @IndexCount = 4
    PRINT '? All indexes verified successfully!';
ELSE
    PRINT '? Warning: Not all indexes were created. Please review the script output.';

PRINT '';
PRINT '========================================';
PRINT 'Claims Table Enhancement Complete!';
PRINT '========================================';
PRINT '';
PRINT 'Next Steps:';
PRINT '1. Verify the changes in your application';
PRINT '2. Update FHIR mapping services to use new fields';
PRINT '3. Update API documentation';
PRINT '4. Test claim creation/update with new fields';
PRINT '';

-- If everything looks good, commit the transaction
COMMIT TRANSACTION;
PRINT 'Transaction committed successfully.';

-- Uncomment the line below to rollback if needed
-- ROLLBACK TRANSACTION;
-- PRINT 'Transaction rolled back.';
GO

-- ================================================================
-- END OF SCRIPT
-- ================================================================
