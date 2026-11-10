-- ================================================================
-- Organizations and Practitioners Table Enhancement Migration Script
-- Version: 1.0
-- Date: 2025-01-XX
-- Description: Adds Saudi Arabia-specific identifiers and regulatory
--          compliance fields to Organizations and Practitioners tables
-- ================================================================

USE [NPhies_FHIR_Integration] -- Update with your database name
GO

BEGIN TRANSACTION;
GO

PRINT '========================================';
PRINT 'Starting Organizations and Practitioners Enhancement';
PRINT '========================================';
PRINT '';

-- ================================================================
-- SECTION 1: ORGANIZATIONS TABLE ENHANCEMENTS
-- ================================================================
PRINT '========================================';
PRINT 'PART 1: Enhancing Organizations Table';
PRINT '========================================';
PRINT '';

PRINT 'Adding Saudi Arabia-specific identifiers to Organizations...';

-- MOH License Number
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Organizations') AND name = 'MOHLicenseNumber')
BEGIN
    ALTER TABLE [dbo].[Organizations] ADD [MOHLicenseNumber] NVARCHAR(50) NULL;
  PRINT '  ? Added MOHLicenseNumber';
END
ELSE
    PRINT '  - MOHLicenseNumber already exists';

-- CHI Number
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Organizations') AND name = 'CHINumber')
BEGIN
    ALTER TABLE [dbo].[Organizations] ADD [CHINumber] NVARCHAR(50) NULL;
    PRINT '  ? Added CHINumber (Council of Health Insurance)';
END
ELSE
 PRINT '  - CHINumber already exists';

-- NPHIES Organization ID
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Organizations') AND name = 'NphiesOrganizationId')
BEGIN
    ALTER TABLE [dbo].[Organizations] ADD [NphiesOrganizationId] NVARCHAR(100) NULL;
    PRINT '  ? Added NphiesOrganizationId';
END
ELSE
    PRINT '  - NphiesOrganizationId already exists';

-- NPHIES Provider ID
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Organizations') AND name = 'NphiesProviderId')
BEGIN
ALTER TABLE [dbo].[Organizations] ADD [NphiesProviderId] NVARCHAR(100) NULL;
    PRINT '  ? Added NphiesProviderId';
END
ELSE
    PRINT '  - NphiesProviderId already exists';

-- NPHIES Payer ID
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Organizations') AND name = 'NphiesPayerId')
BEGIN
    ALTER TABLE [dbo].[Organizations] ADD [NphiesPayerId] NVARCHAR(100) NULL;
PRINT '  ? Added NphiesPayerId';
END
ELSE
    PRINT '  - NphiesPayerId already exists';

-- Tax Registration Number
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Organizations') AND name = 'TaxRegistrationNumber')
BEGIN
    ALTER TABLE [dbo].[Organizations] ADD [TaxRegistrationNumber] NVARCHAR(50) NULL;
    PRINT '  ? Added TaxRegistrationNumber';
END
ELSE
  PRINT '  - TaxRegistrationNumber already exists';

PRINT '';
PRINT 'Organizations table columns enhanced successfully.';
PRINT '';

-- ================================================================
-- SECTION 2: PRACTITIONERS TABLE ENHANCEMENTS
-- ================================================================
PRINT '========================================';
PRINT 'PART 2: Enhancing Practitioners Table';
PRINT '========================================';
PRINT '';

PRINT 'Adding Saudi Arabia-specific fields to Practitioners...';

-- Practitioner License Number
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Practitioners') AND name = 'PractitionerLicenseNumber')
BEGIN
    ALTER TABLE [dbo].[Practitioners] ADD [PractitionerLicenseNumber] NVARCHAR(50) NULL;
    PRINT '  ? Added PractitionerLicenseNumber';
END
ELSE
    PRINT '  - PractitionerLicenseNumber already exists';

-- License Issuing Authority
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Practitioners') AND name = 'LicenseIssuingAuthority')
BEGIN
    ALTER TABLE [dbo].[Practitioners] ADD [LicenseIssuingAuthority] NVARCHAR(100) NULL;
    PRINT '  ? Added LicenseIssuingAuthority';
END
ELSE
    PRINT '  - LicenseIssuingAuthority already exists';

-- License Expiry Date
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Practitioners') AND name = 'LicenseExpiryDate')
BEGIN
    ALTER TABLE [dbo].[Practitioners] ADD [LicenseExpiryDate] DATETIME2 NULL;
    PRINT '  ? Added LicenseExpiryDate';
END
ELSE
    PRINT '  - LicenseExpiryDate already exists';

-- National Identification Number
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Practitioners') AND name = 'NationalIdentificationNumber')
BEGIN
    ALTER TABLE [dbo].[Practitioners] ADD [NationalIdentificationNumber] NVARCHAR(20) NULL;
    PRINT '  ? Added NationalIdentificationNumber';
END
ELSE
    PRINT '  - NationalIdentificationNumber already exists';

-- Practitioner Role
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Practitioners') AND name = 'PractitionerRole')
BEGIN
    ALTER TABLE [dbo].[Practitioners] ADD [PractitionerRole] NVARCHAR(100) NULL;
    PRINT '  ? Added PractitionerRole';
END
ELSE
    PRINT '  - PractitionerRole already exists';

-- Practitioner Role System
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Practitioners') AND name = 'PractitionerRoleSystem')
BEGIN
    ALTER TABLE [dbo].[Practitioners] ADD [PractitionerRoleSystem] NVARCHAR(200) NULL;
 PRINT '  ? Added PractitionerRoleSystem';
END
ELSE
    PRINT '  - PractitionerRoleSystem already exists';

PRINT '';
PRINT 'Practitioners table columns enhanced successfully.';
PRINT '';

-- ================================================================
-- SECTION 3: ORGANIZATIONS INDEXES
-- ================================================================
PRINT '========================================';
PRINT 'PART 3: Creating Indexes for Organizations';
PRINT '========================================';
PRINT '';

-- MOH License Number (Unique, Filtered)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Organizations_MOHLicenseNumber' AND object_id = OBJECT_ID('dbo.Organizations'))
BEGIN
    CREATE UNIQUE INDEX IX_Organizations_MOHLicenseNumber 
    ON [dbo].[Organizations]([MOHLicenseNumber]) 
    WHERE [MOHLicenseNumber] IS NOT NULL;
    PRINT '  ? Created unique filtered index IX_Organizations_MOHLicenseNumber';
END
ELSE
    PRINT '  - Index IX_Organizations_MOHLicenseNumber already exists';

-- CHI Number (Unique, Filtered)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Organizations_CHINumber' AND object_id = OBJECT_ID('dbo.Organizations'))
BEGIN
    CREATE UNIQUE INDEX IX_Organizations_CHINumber 
    ON [dbo].[Organizations]([CHINumber]) 
    WHERE [CHINumber] IS NOT NULL;
    PRINT '  ? Created unique filtered index IX_Organizations_CHINumber';
END
ELSE
    PRINT '  - Index IX_Organizations_CHINumber already exists';

-- NPHIES Organization ID (Filtered)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Organizations_NphiesOrganizationId' AND object_id = OBJECT_ID('dbo.Organizations'))
BEGIN
    CREATE INDEX IX_Organizations_NphiesOrganizationId 
    ON [dbo].[Organizations]([NphiesOrganizationId]) 
    WHERE [NphiesOrganizationId] IS NOT NULL;
    PRINT '  ? Created filtered index IX_Organizations_NphiesOrganizationId';
END
ELSE
    PRINT '  - Index IX_Organizations_NphiesOrganizationId already exists';

-- NPHIES Provider ID (Filtered)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Organizations_NphiesProviderId' AND object_id = OBJECT_ID('dbo.Organizations'))
BEGIN
    CREATE INDEX IX_Organizations_NphiesProviderId 
    ON [dbo].[Organizations]([NphiesProviderId]) 
  WHERE [NphiesProviderId] IS NOT NULL;
    PRINT '  ? Created filtered index IX_Organizations_NphiesProviderId';
END
ELSE
    PRINT '  - Index IX_Organizations_NphiesProviderId already exists';

-- NPHIES Payer ID (Filtered)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Organizations_NphiesPayerId' AND object_id = OBJECT_ID('dbo.Organizations'))
BEGIN
    CREATE INDEX IX_Organizations_NphiesPayerId 
    ON [dbo].[Organizations]([NphiesPayerId]) 
WHERE [NphiesPayerId] IS NOT NULL;
    PRINT '  ? Created filtered index IX_Organizations_NphiesPayerId';
END
ELSE
    PRINT '  - Index IX_Organizations_NphiesPayerId already exists';

-- Tax Registration Number (Filtered)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Organizations_TaxRegistrationNumber' AND object_id = OBJECT_ID('dbo.Organizations'))
BEGIN
    CREATE INDEX IX_Organizations_TaxRegistrationNumber 
    ON [dbo].[Organizations]([TaxRegistrationNumber]) 
    WHERE [TaxRegistrationNumber] IS NOT NULL;
    PRINT '  ? Created filtered index IX_Organizations_TaxRegistrationNumber';
END
ELSE
    PRINT '  - Index IX_Organizations_TaxRegistrationNumber already exists';

PRINT '';

-- ================================================================
-- SECTION 4: PRACTITIONERS INDEXES
-- ================================================================
PRINT '========================================';
PRINT 'PART 4: Creating Indexes for Practitioners';
PRINT '========================================';
PRINT '';

-- Practitioner License Number (Unique, Filtered)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Practitioners_PractitionerLicenseNumber' AND object_id = OBJECT_ID('dbo.Practitioners'))
BEGIN
    CREATE UNIQUE INDEX IX_Practitioners_PractitionerLicenseNumber 
    ON [dbo].[Practitioners]([PractitionerLicenseNumber]) 
    WHERE [PractitionerLicenseNumber] IS NOT NULL;
    PRINT '  ? Created unique filtered index IX_Practitioners_PractitionerLicenseNumber';
END
ELSE
    PRINT '  - Index IX_Practitioners_PractitionerLicenseNumber already exists';

-- National Identification Number (Unique, Filtered)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Practitioners_NationalIdentificationNumber' AND object_id = OBJECT_ID('dbo.Practitioners'))
BEGIN
    CREATE UNIQUE INDEX IX_Practitioners_NationalIdentificationNumber 
    ON [dbo].[Practitioners]([NationalIdentificationNumber]) 
    WHERE [NationalIdentificationNumber] IS NOT NULL;
    PRINT '  ? Created unique filtered index IX_Practitioners_NationalIdentificationNumber';
END
ELSE
    PRINT '  - Index IX_Practitioners_NationalIdentificationNumber already exists';

-- License Expiry Date (Filtered)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Practitioners_LicenseExpiryDate' AND object_id = OBJECT_ID('dbo.Practitioners'))
BEGIN
    CREATE INDEX IX_Practitioners_LicenseExpiryDate 
    ON [dbo].[Practitioners]([LicenseExpiryDate]) 
    WHERE [LicenseExpiryDate] IS NOT NULL;
    PRINT '  ? Created filtered index IX_Practitioners_LicenseExpiryDate';
END
ELSE
    PRINT '  - Index IX_Practitioners_LicenseExpiryDate already exists';

-- Practitioner Role (Filtered)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Practitioners_PractitionerRole' AND object_id = OBJECT_ID('dbo.Practitioners'))
BEGIN
    CREATE INDEX IX_Practitioners_PractitionerRole 
    ON [dbo].[Practitioners]([PractitionerRole]) 
    WHERE [PractitionerRole] IS NOT NULL;
    PRINT '  ? Created filtered index IX_Practitioners_PractitionerRole';
END
ELSE
    PRINT '  - Index IX_Practitioners_PractitionerRole already exists';

PRINT '';

-- ================================================================
-- SECTION 5: EXTENDED PROPERTIES (DOCUMENTATION)
-- ================================================================
PRINT '========================================';
PRINT 'PART 5: Adding Extended Properties';
PRINT '========================================';
PRINT '';

-- Organizations Extended Properties
EXEC sys.sp_addextendedproperty 
  @name=N'MS_Description', 
    @value=N'Ministry of Health (MOH) License Number - Required for healthcare providers in Saudi Arabia' , 
    @level0type=N'SCHEMA', @level0name=N'dbo', 
    @level1type=N'TABLE', @level1name=N'Organizations', 
 @level2type=N'COLUMN', @level2name=N'MOHLicenseNumber';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Council of Health Insurance (CHI) Number - Required for insurance companies in Saudi Arabia' , 
    @level0type=N'SCHEMA', @level0name=N'dbo', 
    @level1type=N'TABLE', @level1name=N'Organizations', 
    @level2type=N'COLUMN', @level2name=N'CHINumber';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'NPHIES Organization Identifier - Unique ID assigned by NPHIES platform for all transactions' , 
    @level0type=N'SCHEMA', @level0name=N'dbo', 
    @level1type=N'TABLE', @level1name=N'Organizations', 
    @level2type=N'COLUMN', @level2name=N'NphiesOrganizationId';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'NPHIES Provider ID - Specific identifier for healthcare providers in NPHIES system' , 
    @level0type=N'SCHEMA', @level0name=N'dbo', 
    @level1type=N'TABLE', @level1name=N'Organizations', 
    @level2type=N'COLUMN', @level2name=N'NphiesProviderId';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'NPHIES Payer ID - Specific identifier for insurance payers in NPHIES system' , 
    @level0type=N'SCHEMA', @level0name=N'dbo', 
    @level1type=N'TABLE', @level1name=N'Organizations', 
    @level2type=N'COLUMN', @level2name=N'NphiesPayerId';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Tax Registration Number / VAT Number - Required for billing and tax purposes in Saudi Arabia (15 digits)' , 
    @level0type=N'SCHEMA', @level0name=N'dbo', 
    @level1type=N'TABLE', @level1name=N'Organizations', 
    @level2type=N'COLUMN', @level2name=N'TaxRegistrationNumber';

-- Practitioners Extended Properties
EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Professional Practice License Number issued by licensing authority (e.g., SCFHS)' , 
    @level0type=N'SCHEMA', @level0name=N'dbo', 
    @level1type=N'TABLE', @level1name=N'Practitioners', 
    @level2type=N'COLUMN', @level2name=N'PractitionerLicenseNumber';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'License Issuing Authority - e.g., "Saudi Commission for Health Specialties (SCFHS)", "Ministry of Health"' , 
    @level0type=N'SCHEMA', @level0name=N'dbo', 
    @level1type=N'TABLE', @level1name=N'Practitioners', 
@level2type=N'COLUMN', @level2name=N'LicenseIssuingAuthority';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'License Expiry Date - Date when practitioner license expires and needs renewal' , 
    @level0type=N'SCHEMA', @level0name=N'dbo', 
    @level1type=N'TABLE', @level1name=N'Practitioners', 
    @level2type=N'COLUMN', @level2name=N'LicenseExpiryDate';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
@value=N'National ID (Saudi citizens) or Iqama Number (residents) - 10 digits starting with 1 or 2' , 
    @level0type=N'SCHEMA', @level0name=N'dbo', 
    @level1type=N'TABLE', @level1name=N'Practitioners', 
    @level2type=N'COLUMN', @level2name=N'NationalIdentificationNumber';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Practitioner Role Code - e.g., "doctor", "nurse", "pharmacist". Maps to FHIR PractitionerRole.code' , 
    @level0type=N'SCHEMA', @level0name=N'dbo', 
    @level1type=N'TABLE', @level1name=N'Practitioners', 
    @level2type=N'COLUMN', @level2name=N'PractitionerRole';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Practitioner Role System URL - e.g., "http://terminology.hl7.org/CodeSystem/practitioner-role"' , 
    @level0type=N'SCHEMA', @level0name=N'dbo', 
    @level1type=N'TABLE', @level1name=N'Practitioners', 
    @level2type=N'COLUMN', @level2name=N'PractitionerRoleSystem';

PRINT '  ? Added extended properties for documentation';
PRINT '';

-- ================================================================
-- SECTION 6: VERIFICATION
-- ================================================================
PRINT '========================================';
PRINT 'PART 6: Verification';
PRINT '========================================';
PRINT '';

-- Verify Organizations columns
DECLARE @OrgColumnCount INT;
SELECT @OrgColumnCount = COUNT(*)
FROM sys.columns 
WHERE object_id = OBJECT_ID('dbo.Organizations') 
AND name IN (
    'MOHLicenseNumber', 'CHINumber', 'NphiesOrganizationId',
    'NphiesProviderId', 'NphiesPayerId', 'TaxRegistrationNumber'
);

PRINT 'Organizations - Columns added/verified: ' + CAST(@OrgColumnCount AS NVARCHAR(10)) + ' of 6';

IF @OrgColumnCount = 6
    PRINT '  ? All Organizations columns verified!';
ELSE
    PRINT '  ? Warning: Not all Organizations columns were added.';

-- Verify Practitioners columns
DECLARE @PractColumnCount INT;
SELECT @PractColumnCount = COUNT(*)
FROM sys.columns 
WHERE object_id = OBJECT_ID('dbo.Practitioners') 
AND name IN (
    'PractitionerLicenseNumber', 'LicenseIssuingAuthority', 'LicenseExpiryDate',
    'NationalIdentificationNumber', 'PractitionerRole', 'PractitionerRoleSystem'
);

PRINT 'Practitioners - Columns added/verified: ' + CAST(@PractColumnCount AS NVARCHAR(10)) + ' of 6';

IF @PractColumnCount = 6
  PRINT '  ? All Practitioners columns verified!';
ELSE
    PRINT '  ? Warning: Not all Practitioners columns were added.';

PRINT '';

-- Verify Organizations indexes
DECLARE @OrgIndexCount INT;
SELECT @OrgIndexCount = COUNT(*)
FROM sys.indexes 
WHERE object_id = OBJECT_ID('dbo.Organizations')
AND name IN (
    'IX_Organizations_MOHLicenseNumber',
    'IX_Organizations_CHINumber',
    'IX_Organizations_NphiesOrganizationId',
    'IX_Organizations_NphiesProviderId',
    'IX_Organizations_NphiesPayerId',
    'IX_Organizations_TaxRegistrationNumber'
);

PRINT 'Organizations - Indexes created/verified: ' + CAST(@OrgIndexCount AS NVARCHAR(10)) + ' of 6';

IF @OrgIndexCount = 6
    PRINT '  ? All Organizations indexes verified!';
ELSE
    PRINT '  ? Warning: Not all Organizations indexes were created.';

-- Verify Practitioners indexes
DECLARE @PractIndexCount INT;
SELECT @PractIndexCount = COUNT(*)
FROM sys.indexes 
WHERE object_id = OBJECT_ID('dbo.Practitioners')
AND name IN (
    'IX_Practitioners_PractitionerLicenseNumber',
    'IX_Practitioners_NationalIdentificationNumber',
    'IX_Practitioners_LicenseExpiryDate',
'IX_Practitioners_PractitionerRole'
);

PRINT 'Practitioners - Indexes created/verified: ' + CAST(@PractIndexCount AS NVARCHAR(10)) + ' of 4';

IF @PractIndexCount = 4
    PRINT '  ? All Practitioners indexes verified!';
ELSE
    PRINT '  ? Warning: Not all Practitioners indexes were created.';

PRINT '';
PRINT '========================================';
PRINT 'Enhancement Complete!';
PRINT '========================================';
PRINT '';
PRINT 'Summary:';
PRINT '  • Organizations: ' + CAST(@OrgColumnCount AS NVARCHAR(10)) + ' columns, ' + CAST(@OrgIndexCount AS NVARCHAR(10)) + ' indexes';
PRINT '  • Practitioners: ' + CAST(@PractColumnCount AS NVARCHAR(10)) + ' columns, ' + CAST(@PractIndexCount AS NVARCHAR(10)) + ' indexes';
PRINT '';
PRINT 'Next Steps:';
PRINT '1. Verify changes in application';
PRINT '2. Update organization/practitioner forms';
PRINT '3. Implement license expiry monitoring';
PRINT '4. Update NPHIES integration services';
PRINT '5. Add validation rules for identifiers';
PRINT '6. Test unique constraints';
PRINT '';

-- Commit the transaction
COMMIT TRANSACTION;
PRINT 'Transaction committed successfully.';

-- Uncomment to rollback if needed
-- ROLLBACK TRANSACTION;
-- PRINT 'Transaction rolled back.';
GO

-- ================================================================
-- END OF SCRIPT
-- ================================================================
