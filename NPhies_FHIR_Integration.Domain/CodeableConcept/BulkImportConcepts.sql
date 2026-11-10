-- ========================================================================
-- NPHIES CodeableConcept Bulk Import Script
-- ========================================================================
-- This script provides templates for bulk importing concepts from CSV files
-- into the CodeableConcept database
--
-- Prerequisites:
-- 1. Run DatabaseSchema.sql first
-- 2. Run ImportDataInitial.sql to seed CodeSystems and ValueSets
-- 3. Prepare CSV files with concept data
--
-- CSV Format:
-- CodeSystemId,Code,Display,Definition,DisplayArabic,SortOrder,IsActive
-- 1,institutional,Institutional,Institutional claim,?????? ??????,1,1
-- ========================================================================

USE NPhiesDb;
GO

-- ========================================================================
-- STEP 1: Create temporary staging table
-- ========================================================================
IF OBJECT_ID('tempdb..#ConceptStaging') IS NOT NULL
    DROP TABLE #ConceptStaging;

CREATE TABLE #ConceptStaging (
    CodeSystemId INT NOT NULL,
    Code NVARCHAR(100) NOT NULL,
    Display NVARCHAR(500) NOT NULL,
    Definition NVARCHAR(MAX) NULL,
    DisplayArabic NVARCHAR(500) NULL,
    SortOrder INT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

-- ========================================================================
-- STEP 2: Bulk Import from CSV Files
-- ========================================================================

-- Example 1: Import Claim Type Codes
-- CSV File: ClaimTypeCodes.csv
PRINT '?? Importing Claim Type codes...';
BULK INSERT #ConceptStaging
FROM 'C:\NPhiesData\ClaimTypeCodes.csv'
WITH (
    FIRSTROW = 2,       -- Skip header row
    FIELDTERMINATOR = ',',  -- CSV delimiter
    ROWTERMINATOR = '\n', -- Line ending
    CODEPAGE = '65001',          -- UTF-8 encoding
    TABLOCK,
    ERRORFILE = 'C:\NPhiesData\Errors\ClaimTypeCodes_errors.txt'
);
GO

-- Example 2: Import Diagnosis Type Codes
-- CSV File: DiagnosisTypeCodes.csv
PRINT '?? Importing Diagnosis Type codes...';
BULK INSERT #ConceptStaging
FROM 'C:\NPhiesData\DiagnosisTypeCodes.csv'
WITH (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '\n',
    CODEPAGE = '65001',
    TABLOCK,
    ERRORFILE = 'C:\NPhiesData\Errors\DiagnosisTypeCodes_errors.txt'
);
GO

-- Example 3: Import NPHIES Service Codes (large dataset)
-- CSV File: NphiesServiceCodes.csv
PRINT '?? Importing NPHIES Service codes...';
BULK INSERT #ConceptStaging
FROM 'C:\NPhiesData\NphiesServiceCodes.csv'
WITH (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '\n',
    CODEPAGE = '65001',
    BATCHSIZE = 10000,           -- Process in batches for large files
    TABLOCK,
    ERRORFILE = 'C:\NPhiesData\Errors\NphiesServiceCodes_errors.txt'
);
GO

-- Example 4: Import ICD-10 Diagnosis Codes (very large dataset)
-- CSV File: ICD10Codes.csv
PRINT '?? Importing ICD-10 diagnosis codes...';
BULK INSERT #ConceptStaging
FROM 'C:\NPhiesData\ICD10Codes.csv'
WITH (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '\n',
    CODEPAGE = '65001',
    BATCHSIZE = 50000,     -- Large batch size for huge files
    TABLOCK,
    ERRORFILE = 'C:\NPhiesData\Errors\ICD10Codes_errors.txt'
);
GO

-- ========================================================================
-- STEP 3: Validate staged data
-- ========================================================================
PRINT '?? Validating staged data...';

-- Check for duplicate codes within same CodeSystem
SELECT 
    CodeSystemId,
    Code,
    COUNT(*) AS DuplicateCount
FROM #ConceptStaging
GROUP BY CodeSystemId, Code
HAVING COUNT(*) > 1;

-- Check for invalid CodeSystemIds
SELECT DISTINCT s.CodeSystemId
FROM #ConceptStaging s
LEFT JOIN CodeSystems cs ON s.CodeSystemId = cs.CodeSystemId
WHERE cs.CodeSystemId IS NULL;

-- Check for missing required fields
SELECT *
FROM #ConceptStaging
WHERE Code IS NULL 
   OR Display IS NULL 
   OR CodeSystemId IS NULL;

-- ========================================================================
-- STEP 4: Insert into Concept table
-- ========================================================================
PRINT '?? Inserting concepts into database...';

BEGIN TRANSACTION;

INSERT INTO Concepts (
    CodeSystemId,
    Code,
    Display,
    Definition,
    DisplayArabic,
    SortOrder,
    IsActive,
    CreatedAt
)
SELECT 
    s.CodeSystemId,
    s.Code,
    s.Display,
    s.Definition,
    s.DisplayArabic,
    ISNULL(s.SortOrder, 0),
    s.IsActive,
    GETUTCDATE()
FROM #ConceptStaging s
WHERE NOT EXISTS (
    -- Avoid duplicates
    SELECT 1
    FROM Concepts c
    WHERE c.CodeSystemId = s.CodeSystemId
    AND c.Code = s.Code
);

DECLARE @InsertedCount INT = @@ROWCOUNT;
PRINT CONCAT('? Inserted ', @InsertedCount, ' new concepts');

COMMIT TRANSACTION;
GO

-- ========================================================================
-- STEP 5: Create ValueSet mappings
-- ========================================================================
PRINT '?? Creating ValueSet-to-CodeSystem mappings...';

-- Example: Map claim-type ValueSet to claim-type CodeSystem
DECLARE @ClaimTypeValueSetId INT = (SELECT ValueSetId FROM ValueSets WHERE Name = 'claim-type');
DECLARE @ClaimTypeCodeSystemId INT = (SELECT CodeSystemId FROM CodeSystems WHERE Name = 'claim-type');

IF @ClaimTypeValueSetId IS NOT NULL AND @ClaimTypeCodeSystemId IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM ValueSetCodeSystemMaps 
      WHERE ValueSetId = @ClaimTypeValueSetId 
 AND CodeSystemId = @ClaimTypeCodeSystemId
    )
  BEGIN
        INSERT INTO ValueSetCodeSystemMaps (ValueSetId, CodeSystemId, Sequence, IsActive)
        VALUES (@ClaimTypeValueSetId, @ClaimTypeCodeSystemId, 1, 1);
    
        PRINT '? Created mapping: claim-type ValueSet -> claim-type CodeSystem';
    END
END
GO

-- Example: Map institutional-billing ValueSet to multiple CodeSystems
DECLARE @InstitutionalBillingVSId INT = (SELECT ValueSetId FROM ValueSets WHERE Name = 'institutional-billing');
DECLARE @ProceduresCSId INT = (SELECT CodeSystemId FROM CodeSystems WHERE Name = 'procedures');
DECLARE @ServicesCSId INT = (SELECT CodeSystemId FROM CodeSystems WHERE Name = 'services');

IF @InstitutionalBillingVSId IS NOT NULL
BEGIN
    -- Map to procedures CodeSystem
    IF @ProceduresCSId IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM ValueSetCodeSystemMaps 
        WHERE ValueSetId = @InstitutionalBillingVSId AND CodeSystemId = @ProceduresCSId
    )
    BEGIN
    INSERT INTO ValueSetCodeSystemMaps (ValueSetId, CodeSystemId, Sequence, IsActive)
        VALUES (@InstitutionalBillingVSId, @ProceduresCSId, 1, 1);
    END
    
    -- Map to services CodeSystem
    IF @ServicesCSId IS NOT NULL AND NOT EXISTS (
 SELECT 1 FROM ValueSetCodeSystemMaps 
        WHERE ValueSetId = @InstitutionalBillingVSId AND CodeSystemId = @ServicesCSId
    )
    BEGIN
        INSERT INTO ValueSetCodeSystemMaps (ValueSetId, CodeSystemId, Sequence, IsActive)
        VALUES (@InstitutionalBillingVSId, @ServicesCSId, 2, 1);
    END
    
    PRINT '? Created mappings: institutional-billing ValueSet -> procedures & services CodeSystems';
END
GO

-- ========================================================================
-- STEP 6: Verification queries
-- ========================================================================
PRINT '?? Database Statistics:';

-- Count concepts by CodeSystem
SELECT 
    cs.Name AS CodeSystemName,
    COUNT(c.ConceptId) AS ConceptCount,
    SUM(CASE WHEN c.IsActive = 1 THEN 1 ELSE 0 END) AS ActiveCount
FROM CodeSystems cs
LEFT JOIN Concepts c ON cs.CodeSystemId = c.CodeSystemId
GROUP BY cs.Name
ORDER BY ConceptCount DESC;

-- Count concepts by ValueSet
SELECT 
 vs.Name AS ValueSetName,
    COUNT(DISTINCT c.ConceptId) AS ConceptCount
FROM ValueSets vs
INNER JOIN ValueSetCodeSystemMaps vscm ON vs.ValueSetId = vscm.ValueSetId
INNER JOIN Concepts c ON vscm.CodeSystemId = c.CodeSystemId AND c.IsActive = 1
GROUP BY vs.Name
ORDER BY ConceptCount DESC;

-- Total statistics
SELECT 
    (SELECT COUNT(*) FROM CodeSystems WHERE IsActive = 1) AS ActiveCodeSystems,
    (SELECT COUNT(*) FROM ValueSets WHERE IsActive = 1) AS ActiveValueSets,
    (SELECT COUNT(*) FROM Concepts WHERE IsActive = 1) AS ActiveConcepts,
    (SELECT COUNT(*) FROM ValueSetCodeSystemMaps WHERE IsActive = 1) AS ActiveMappings;

PRINT '? Bulk import completed successfully!';
GO

-- ========================================================================
-- STEP 7: Cleanup
-- ========================================================================
DROP TABLE #ConceptStaging;
GO

-- ========================================================================
-- CSV FILE TEMPLATES
-- ========================================================================
/*
-- ClaimTypeCodes.csv
CodeSystemId,Code,Display,Definition,DisplayArabic,SortOrder,IsActive
1,institutional,Institutional,Institutional claim,?????? ??????,1,1
1,oral,Oral,Oral health claim,?????? ??? ????,2,1
1,pharmacy,Pharmacy,Pharmacy claim,?????? ??????,3,1
1,professional,Professional,Professional claim,?????? ?????,4,1
1,vision,Vision,Vision care claim,?????? ????? ?????,5,1

-- DiagnosisTypeCodes.csv
CodeSystemId,Code,Display,Definition,DisplayArabic,SortOrder,IsActive
2,admitting,Admitting,Admitting diagnosis,????? ??????,1,1
2,principal,Principal,Principal diagnosis,??????? ???????,2,1
2,discharge,Discharge,Discharge diagnosis,????? ??????,3,1

-- NphiesServiceCodes.csv (example)
CodeSystemId,Code,Display,Definition,DisplayArabic,SortOrder,IsActive
3,10001,General consultation,General medical consultation,??????? ???? ????,1,1
3,10002,Follow-up consultation,Follow-up medical consultation,??????? ??????,2,1
3,20001,Blood test,Complete blood count,????? ?? ????,3,1

-- ICD10Codes.csv (example)
CodeSystemId,Code,Display,Definition,DisplayArabic,SortOrder,IsActive
4,A00.0,Cholera due to Vibrio cholerae 01, biovar cholerae,Cholera caused by V. cholerae,????????,1,1
4,A00.1,Cholera due to Vibrio cholerae 01, biovar eltor,Cholera El Tor,???????? ?????,2,1
*/
