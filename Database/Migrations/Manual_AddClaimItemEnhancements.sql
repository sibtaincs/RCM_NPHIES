-- ================================================================
-- ClaimItems Table Enhancement Migration Script
-- Version: 1.0
-- Date: 2025-01-XX
-- Description: Adds body site, pricing factors, linkages, device/location,
--       and program code fields to the ClaimItems table
-- ================================================================

USE [NPhies_FHIR_Integration] -- Update with your database name
GO

BEGIN TRANSACTION;
GO

PRINT '========================================';
PRINT 'Starting ClaimItems Table Enhancement';
PRINT '========================================';
PRINT '';

-- ================================================================
-- SECTION 1: BODY SITE AND SUB-SITE
-- ================================================================
PRINT 'Adding Body Site and Sub-Site fields...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ClaimItems') AND name = 'BodySiteCode')
BEGIN
    ALTER TABLE [dbo].[ClaimItems] ADD [BodySiteCode] NVARCHAR(50) NULL;
 PRINT '  ? Added BodySiteCode';
END
ELSE
    PRINT '  - BodySiteCode already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ClaimItems') AND name = 'BodySiteSystem')
BEGIN
    ALTER TABLE [dbo].[ClaimItems] ADD [BodySiteSystem] NVARCHAR(200) NULL;
    PRINT '  ? Added BodySiteSystem';
END
ELSE
    PRINT '  - BodySiteSystem already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ClaimItems') AND name = 'SubSiteCode')
BEGIN
    ALTER TABLE [dbo].[ClaimItems] ADD [SubSiteCode] NVARCHAR(50) NULL;
    PRINT '  ? Added SubSiteCode';
END
ELSE
    PRINT '  - SubSiteCode already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ClaimItems') AND name = 'SubSiteSystem')
BEGIN
    ALTER TABLE [dbo].[ClaimItems] ADD [SubSiteSystem] NVARCHAR(200) NULL;
    PRINT '  ? Added SubSiteSystem';
END
ELSE
    PRINT '  - SubSiteSystem already exists';

PRINT '';

-- ================================================================
-- SECTION 2: PRICING FACTORS
-- ================================================================
PRINT 'Adding Pricing Factor fields...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ClaimItems') AND name = 'Factor')
BEGIN
    ALTER TABLE [dbo].[ClaimItems] ADD [Factor] DECIMAL(5,2) NULL;
    PRINT '  ? Added Factor';
END
ELSE
    PRINT '  - Factor already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ClaimItems') AND name = 'Tax')
BEGIN
    ALTER TABLE [dbo].[ClaimItems] ADD [Tax] DECIMAL(18,2) NULL;
    PRINT '  ? Added Tax';
END
ELSE
    PRINT '  - Tax already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ClaimItems') AND name = 'TaxRate')
BEGIN
    ALTER TABLE [dbo].[ClaimItems] ADD [TaxRate] DECIMAL(5,2) NULL;
    PRINT '  ? Added TaxRate';
END
ELSE
    PRINT '  - TaxRate already exists';

PRINT '';

-- ================================================================
-- SECTION 3: LINKAGE TO OTHER CLAIM ELEMENTS
-- ================================================================
PRINT 'Adding Linkage fields (DiagnosisSequence, InformationSequence, ProcedureSequence)...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ClaimItems') AND name = 'DiagnosisSequence')
BEGIN
    ALTER TABLE [dbo].[ClaimItems] ADD [DiagnosisSequence] NVARCHAR(100) NULL;
    PRINT '  ? Added DiagnosisSequence';
END
ELSE
    PRINT '  - DiagnosisSequence already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ClaimItems') AND name = 'InformationSequence')
BEGIN
    ALTER TABLE [dbo].[ClaimItems] ADD [InformationSequence] NVARCHAR(100) NULL;
    PRINT '  ? Added InformationSequence';
END
ELSE
    PRINT '  - InformationSequence already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ClaimItems') AND name = 'ProcedureSequence')
BEGIN
    ALTER TABLE [dbo].[ClaimItems] ADD [ProcedureSequence] NVARCHAR(100) NULL;
    PRINT '  ? Added ProcedureSequence';
END
ELSE
    PRINT '  - ProcedureSequence already exists';

PRINT '';

-- ================================================================
-- SECTION 4: DEVICE AND LOCATION
-- ================================================================
PRINT 'Adding Device and Location fields...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ClaimItems') AND name = 'UDI')
BEGIN
    ALTER TABLE [dbo].[ClaimItems] ADD [UDI] NVARCHAR(100) NULL;
    PRINT '  ? Added UDI (Unique Device Identifier)';
END
ELSE
    PRINT '  - UDI already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ClaimItems') AND name = 'LocationId')
BEGIN
    ALTER TABLE [dbo].[ClaimItems] ADD [LocationId] NVARCHAR(100) NULL;
    PRINT '  ? Added LocationId';
END
ELSE
    PRINT '  - LocationId already exists';

PRINT '';

-- ================================================================
-- SECTION 5: PROGRAM CODE
-- ================================================================
PRINT 'Adding Program Code fields...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ClaimItems') AND name = 'ProgramCode')
BEGIN
    ALTER TABLE [dbo].[ClaimItems] ADD [ProgramCode] NVARCHAR(50) NULL;
    PRINT '  ? Added ProgramCode';
END
ELSE
    PRINT '  - ProgramCode already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ClaimItems') AND name = 'ProgramCodeSystem')
BEGIN
    ALTER TABLE [dbo].[ClaimItems] ADD [ProgramCodeSystem] NVARCHAR(200) NULL;
    PRINT '  ? Added ProgramCodeSystem';
END
ELSE
    PRINT '  - ProgramCodeSystem already exists';

PRINT '';

-- ================================================================
-- SECTION 6: INDEXES FOR PERFORMANCE
-- ================================================================
PRINT 'Adding indexes for performance...';

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ClaimItems_BodySiteCode' AND object_id = OBJECT_ID('dbo.ClaimItems'))
BEGIN
    CREATE INDEX IX_ClaimItems_BodySiteCode ON [dbo].[ClaimItems]([BodySiteCode]) WHERE [BodySiteCode] IS NOT NULL;
    PRINT '  ? Created index IX_ClaimItems_BodySiteCode';
END
ELSE
    PRINT '  - Index IX_ClaimItems_BodySiteCode already exists';

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ClaimItems_LocationId' AND object_id = OBJECT_ID('dbo.ClaimItems'))
BEGIN
    CREATE INDEX IX_ClaimItems_LocationId ON [dbo].[ClaimItems]([LocationId]) WHERE [LocationId] IS NOT NULL;
  PRINT '  ? Created index IX_ClaimItems_LocationId';
END
ELSE
    PRINT '  - Index IX_ClaimItems_LocationId already exists';

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ClaimItems_UDI' AND object_id = OBJECT_ID('dbo.ClaimItems'))
BEGIN
    CREATE INDEX IX_ClaimItems_UDI ON [dbo].[ClaimItems]([UDI]) WHERE [UDI] IS NOT NULL;
 PRINT '  ? Created index IX_ClaimItems_UDI';
END
ELSE
    PRINT '  - Index IX_ClaimItems_UDI already exists';

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ClaimItems_ProgramCode' AND object_id = OBJECT_ID('dbo.ClaimItems'))
BEGIN
    CREATE INDEX IX_ClaimItems_ProgramCode ON [dbo].[ClaimItems]([ProgramCode]) WHERE [ProgramCode] IS NOT NULL;
    PRINT '  ? Created index IX_ClaimItems_ProgramCode';
END
ELSE
    PRINT '  - Index IX_ClaimItems_ProgramCode already exists';

PRINT '';

-- ================================================================
-- SECTION 7: FOREIGN KEY CONSTRAINTS
-- ================================================================
PRINT 'Adding foreign key constraints...';

-- Foreign key to Locations table
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys 
    WHERE name = 'FK_ClaimItems_Locations_LocationId' 
    AND parent_object_id = OBJECT_ID('dbo.ClaimItems')
)
BEGIN
    ALTER TABLE [dbo].[ClaimItems]
        ADD CONSTRAINT FK_ClaimItems_Locations_LocationId 
     FOREIGN KEY ([LocationId]) 
        REFERENCES [dbo].[Locations]([Id])
        ON DELETE NO ACTION; -- Restrict delete
    PRINT '  ? Created foreign key FK_ClaimItems_Locations_LocationId';
END
ELSE
    PRINT '  - Foreign key FK_ClaimItems_Locations_LocationId already exists';

PRINT '';

-- ================================================================
-- SECTION 8: ADD EXTENDED PROPERTIES (DOCUMENTATION)
-- ================================================================
PRINT 'Adding extended properties for documentation...';

-- Body Site fields
EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Anatomical location code where service was performed (e.g., "72696002" for knee). Maps to FHIR Claim.item.bodySite' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'ClaimItems', 
    @level2type=N'COLUMN',
    @level2name=N'BodySiteCode';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'System URL for body site coding (e.g., "http://snomed.info/sct" for SNOMED CT)' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'ClaimItems', 
@level2type=N'COLUMN',
    @level2name=N'BodySiteSystem';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'More specific anatomical location (e.g., "upper outer quadrant"). Maps to FHIR Claim.item.subSite' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'ClaimItems', 
    @level2type=N'COLUMN',
    @level2name=N'SubSiteCode';

EXEC sys.sp_addextendedproperty 
  @name=N'MS_Description', 
    @value=N'System URL for sub-site coding' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'ClaimItems', 
    @level2type=N'COLUMN',
    @level2name=N'SubSiteSystem';

-- Pricing Factor fields
EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Price multiplier (e.g., 1.5 for emergency, 2.0 for after-hours). Applied to unit price. Maps to FHIR Claim.item.factor' , 
    @level0type=N'SCHEMA',
  @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'ClaimItems', 
    @level2type=N'COLUMN',
 @level2name=N'Factor';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Tax amount applied to this item. Maps to FHIR Claim.item.tax (Money)' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'ClaimItems', 
    @level2type=N'COLUMN',
 @level2name=N'Tax';

EXEC sys.sp_addextendedproperty 
@name=N'MS_Description', 
 @value=N'Tax rate as percentage (e.g., 15.00 for 15% VAT). Used to calculate Tax amount' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'ClaimItems', 
    @level2type=N'COLUMN',
    @level2name=N'TaxRate';

-- Linkage fields
EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Comma-separated sequence numbers of related diagnoses (e.g., "1,3"). Maps to FHIR Claim.item.diagnosisSequence' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'ClaimItems', 
    @level2type=N'COLUMN',
    @level2name=N'DiagnosisSequence';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Comma-separated sequence numbers of related supporting information (e.g., "2,4"). Maps to FHIR Claim.item.informationSequence' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'ClaimItems', 
    @level2type=N'COLUMN',
    @level2name=N'InformationSequence';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Comma-separated sequence numbers of related procedures (e.g., "1"). Maps to FHIR Claim.item.procedureSequence' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'ClaimItems', 
@level2type=N'COLUMN',
    @level2name=N'ProcedureSequence';

-- Device and Location fields
EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Unique Device Identifier for medical devices/implants (e.g., GS1 barcode). Maps to FHIR Claim.item.udi' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'ClaimItems', 
    @level2type=N'COLUMN',
  @level2name=N'UDI';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Reference to Location where service was performed (e.g., operating room, ICU). Maps to FHIR Claim.item.locationReference' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'ClaimItems', 
    @level2type=N'COLUMN',
    @level2name=N'LocationId';

-- Program Code fields
EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'Insurance program or government scheme code (e.g., "maternal-health"). Maps to FHIR Claim.item.programCode' , 
@level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
    @level1name=N'ClaimItems', 
    @level2type=N'COLUMN',
    @level2name=N'ProgramCode';

EXEC sys.sp_addextendedproperty 
    @name=N'MS_Description', 
    @value=N'System URL for program coding (e.g., "http://nphies.sa/terminology/programs")' , 
    @level0type=N'SCHEMA',
    @level0name=N'dbo', 
    @level1type=N'TABLE',
  @level1name=N'ClaimItems', 
    @level2type=N'COLUMN',
    @level2name=N'ProgramCodeSystem';

PRINT '  ? Added extended properties';
PRINT '';

-- ================================================================
-- SECTION 9: VERIFICATION
-- ================================================================
PRINT 'Verifying changes...';
PRINT '';

DECLARE @ColumnCount INT;

SELECT @ColumnCount = COUNT(*)
FROM sys.columns 
WHERE object_id = OBJECT_ID('dbo.ClaimItems') 
AND name IN (
    'BodySiteCode', 'BodySiteSystem', 'SubSiteCode', 'SubSiteSystem',
    'Factor', 'Tax', 'TaxRate',
    'DiagnosisSequence', 'InformationSequence', 'ProcedureSequence',
    'UDI', 'LocationId',
    'ProgramCode', 'ProgramCodeSystem'
);

PRINT 'Columns added/verified: ' + CAST(@ColumnCount AS NVARCHAR(10)) + ' of 14';

IF @ColumnCount = 14
    PRINT '? All columns verified successfully!';
ELSE
    PRINT '? Warning: Not all columns were added. Please review the script output.';

PRINT '';

DECLARE @IndexCount INT;

SELECT @IndexCount = COUNT(*)
FROM sys.indexes 
WHERE object_id = OBJECT_ID('dbo.ClaimItems')
AND name IN (
    'IX_ClaimItems_BodySiteCode',
    'IX_ClaimItems_LocationId',
  'IX_ClaimItems_UDI',
    'IX_ClaimItems_ProgramCode'
);

PRINT 'Indexes created/verified: ' + CAST(@IndexCount AS NVARCHAR(10)) + ' of 4';

IF @IndexCount = 4
    PRINT '? All indexes verified successfully!';
ELSE
    PRINT '? Warning: Not all indexes were created. Please review the script output.';

PRINT '';

DECLARE @FKCount INT;

SELECT @FKCount = COUNT(*)
FROM sys.foreign_keys 
WHERE parent_object_id = OBJECT_ID('dbo.ClaimItems')
AND name = 'FK_ClaimItems_Locations_LocationId';

PRINT 'Foreign keys created/verified: ' + CAST(@FKCount AS NVARCHAR(10)) + ' of 1';

IF @FKCount = 1
    PRINT '? Foreign key verified successfully!';
ELSE
    PRINT '? Warning: Foreign key was not created. Please review the script output.';

PRINT '';
PRINT '========================================';
PRINT 'ClaimItems Table Enhancement Complete!';
PRINT '========================================';
PRINT '';
PRINT 'Next Steps:';
PRINT '1. Verify the changes in your application';
PRINT '2. Update FHIR mapping services to use new fields';
PRINT '3. Update validation rules for new fields';
PRINT '4. Add unit tests for pricing calculations';
PRINT '5. Test device tracking with UDI';
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
