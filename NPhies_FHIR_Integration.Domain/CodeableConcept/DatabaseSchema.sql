-- ============================================================================
-- NPHIES CodeableConcept Terminology Database Schema
-- Target: SQL Server 2019+
-- Version: 1.0.0
-- Description: Complete normalized schema for NPHIES FHIR CodeSystems, ValueSets, 
--              Concepts, and their usage in NPHIES message types
-- ============================================================================

-- ============================================================================
-- 1. CodeSystem Table - Metadata for all CodeSystems
-- ============================================================================
CREATE TABLE [dbo].[CodeSystem] (
    [CodeSystemId]      INT IDENTITY(1,1) PRIMARY KEY,
    [Url]       NVARCHAR(300) NOT NULL UNIQUE,
    [Version]       NVARCHAR(50) NULL,
    [Name]              NVARCHAR(200) NOT NULL,
    [Title]             NVARCHAR(300) NULL,
    [Definition]        NVARCHAR(MAX) NULL,
    [Committee]  NVARCHAR(200) NULL,
    [Oid] NVARCHAR(100) NULL,
    [Copyright]       NVARCHAR(MAX) NULL,
    [SourceResource]    NVARCHAR(300) NULL,
    [CaseSensitive]  BIT NOT NULL DEFAULT 1,
    [IsActive]    BIT NOT NULL DEFAULT 1,
    [CreatedAt]    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
[UpdatedAt]         DATETIME2 NULL,
    
    INDEX [IX_CodeSystem_Url] NONCLUSTERED ([Url]),
    INDEX [IX_CodeSystem_Name] NONCLUSTERED ([Name])
);

-- ============================================================================
-- 2. Concept Table - Individual codes within a CodeSystem
-- ============================================================================
CREATE TABLE [dbo].[Concept] (
    [ConceptId]         BIGINT IDENTITY(1,1) PRIMARY KEY,
[CodeSystemId]      INT NOT NULL,
    [Code]   NVARCHAR(100) NOT NULL,
    [Display]   NVARCHAR(500) NULL,
    [Definition]   NVARCHAR(MAX) NULL,
    [DisplayArabic]   NVARCHAR(500) NULL,
    [DefinitionArabic]  NVARCHAR(MAX) NULL,
    [IsActive]          BIT NOT NULL DEFAULT 1,
    [SortOrder]         INT NULL,
    [ParentCode]  NVARCHAR(100) NULL,
    [CreatedAt]         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT [UQ_Concept_Code] UNIQUE ([CodeSystemId], [Code]),
    CONSTRAINT [FK_Concept_CodeSystem] FOREIGN KEY ([CodeSystemId]) 
   REFERENCES [dbo].[CodeSystem]([CodeSystemId]),
    
    INDEX [IX_Concept_Code] NONCLUSTERED ([Code]),
    INDEX [IX_Concept_CodeSystem] NONCLUSTERED ([CodeSystemId]),
    INDEX [IX_Concept_Active] NONCLUSTERED ([IsActive])
);

-- ============================================================================
-- 3. ValueSet Table - Collections of codes from one or more CodeSystems
-- ============================================================================
CREATE TABLE [dbo].[ValueSet] (
    [ValueSetId]   INT IDENTITY(1,1) PRIMARY KEY,
    [Url]   NVARCHAR(300) NOT NULL UNIQUE,
    [Version]     NVARCHAR(50) NULL,
    [Name]       NVARCHAR(200) NOT NULL,
    [Title]  NVARCHAR(300) NULL,
    [Definition]        NVARCHAR(MAX) NULL,
    [Committee]       NVARCHAR(200) NULL,
    [Oid]    NVARCHAR(100) NULL,
    [Copyright]         NVARCHAR(MAX) NULL,
    [SourceResource]    NVARCHAR(300) NULL,
    [Restrictions]      NVARCHAR(MAX) NULL,
    [IsActive]        BIT NOT NULL DEFAULT 1,
  [CreatedAt]         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]         DATETIME2 NULL,
    
    INDEX [IX_ValueSet_Url] NONCLUSTERED ([Url]),
    INDEX [IX_ValueSet_Name] NONCLUSTERED ([Name])
);

-- ============================================================================
-- 4. ValueSet_CodeSystem_Map - Links ValueSets to CodeSystems (M:N relationship)
-- ============================================================================
CREATE TABLE [dbo].[ValueSetCodeSystemMap] (
  [ValueSetCodeSystemMapId] INT IDENTITY(1,1) PRIMARY KEY,
    [ValueSetId]        INT NOT NULL,
    [CodeSystemId]      INT NOT NULL,
    [Sequence]          INT NULL,
    [IsActive]   BIT NOT NULL DEFAULT 1,
    
    CONSTRAINT [UQ_ValueSetCodeSystem] UNIQUE ([ValueSetId], [CodeSystemId]),
    CONSTRAINT [FK_VSCM_ValueSet] FOREIGN KEY ([ValueSetId]) 
        REFERENCES [dbo].[ValueSet]([ValueSetId]),
 CONSTRAINT [FK_VSCM_CodeSystem] FOREIGN KEY ([CodeSystemId]) 
        REFERENCES [dbo].[CodeSystem]([CodeSystemId]),
    
    INDEX [IX_VSCM_ValueSet] NONCLUSTERED ([ValueSetId]),
    INDEX [IX_VSCM_CodeSystem] NONCLUSTERED ([CodeSystemId])
);

-- ============================================================================
-- 5. ProfileElement - FHIR Profile paths and their ValueSet bindings
-- ============================================================================
CREATE TABLE [dbo].[ProfileElement] (
    [ProfileElementId]  INT IDENTITY(1,1) PRIMARY KEY,
    [ProfileName]       NVARCHAR(100) NOT NULL,
    [Path]        NVARCHAR(400) NOT NULL,
    [PathArabic]      NVARCHAR(400) NULL,
    [Definition]        NVARCHAR(MAX) NULL,
    [ValueSetId]        INT NULL,
    [BindingStrength]   NVARCHAR(50) NULL, -- required, extensible, preferred, example
    [MessageType]       NVARCHAR(100) NULL, -- eligibility-request, claim-request, cancel-request, etc.
    [ResourceType]      NVARCHAR(100) NULL, -- Claim, CoverageEligibilityRequest, Task, etc.
    [IsRequired]        BIT NOT NULL DEFAULT 0,
    [IsActive]       BIT NOT NULL DEFAULT 1,
    [CreatedAt]         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT [FK_PE_ValueSet] FOREIGN KEY ([ValueSetId]) 
        REFERENCES [dbo].[ValueSet]([ValueSetId]),
    
    INDEX [IX_PE_Path] NONCLUSTERED ([Path]),
    INDEX [IX_PE_MessageType] NONCLUSTERED ([MessageType]),
    INDEX [IX_PE_ResourceType] NONCLUSTERED ([ResourceType]),
INDEX [IX_PE_ProfileName] NONCLUSTERED ([ProfileName])
);

-- ============================================================================
-- 6. ConceptCodeFilter - For ValueSets that restrict specific codes
-- ============================================================================
CREATE TABLE [dbo].[ConceptCodeFilter] (
    [ConceptCodeFilterId] INT IDENTITY(1,1) PRIMARY KEY,
    [ValueSetId]  INT NOT NULL,
    [Code]NVARCHAR(100) NOT NULL,
    [Display]     NVARCHAR(500) NULL,
    [Notes]             NVARCHAR(MAX) NULL,
    [IsActive]      BIT NOT NULL DEFAULT 1,
 
    CONSTRAINT [FK_CCF_ValueSet] FOREIGN KEY ([ValueSetId]) 
        REFERENCES [dbo].[ValueSet]([ValueSetId]),
    
    INDEX [IX_CCF_ValueSet] NONCLUSTERED ([ValueSetId]),
    INDEX [IX_CCF_Code] NONCLUSTERED ([Code])
);

-- ============================================================================
-- 7. ValidationRule - NPHIES-specific validation constraints
-- ============================================================================
CREATE TABLE [dbo].[ValidationRule] (
    [ValidationRuleId]  INT IDENTITY(1,1) PRIMARY KEY,
    [ErrorCode] NVARCHAR(50) NOT NULL,
    [ErrorMessage]      NVARCHAR(MAX) NOT NULL,
    [ErrorMessageArabic] NVARCHAR(MAX) NULL,
    [FieldPath]         NVARCHAR(400) NULL,
    [RuleType]    NVARCHAR(100) NULL, -- CodeExists, SystemMustBe, ValueSetMembership, etc.
    [Parameters]        NVARCHAR(MAX) NULL, -- JSON for rule-specific parameters
    [Severity]          NVARCHAR(50) NULL, -- error, warning, info
    [IsActive]       BIT NOT NULL DEFAULT 1,
    [CreatedAt]         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT [UQ_ValidationRule_ErrorCode] UNIQUE ([ErrorCode]),
    
    INDEX [IX_VR_ErrorCode] NONCLUSTERED ([ErrorCode]),
    INDEX [IX_VR_FieldPath] NONCLUSTERED ([FieldPath]),
  INDEX [IX_VR_RuleType] NONCLUSTERED ([RuleType])
);

-- ============================================================================
-- 8. NphiesMessageType - Mapping of NPHIES message types to required fields
-- ============================================================================
CREATE TABLE [dbo].[NphiesMessageType] (
    [NphiesMessageTypeId] INT IDENTITY(1,1) PRIMARY KEY,
    [MessageType]  NVARCHAR(100) NOT NULL UNIQUE, -- eligibility-request, claim-request, etc.
    [MessageTypeArabic] NVARCHAR(100) NULL,
    [FhirResourceType]  NVARCHAR(100) NOT NULL, -- Claim, CoverageEligibilityRequest, etc.
    [Description]       NVARCHAR(MAX) NULL,
    [Version]           NVARCHAR(50) NULL,
    [IsActive]          BIT NOT NULL DEFAULT 1,
    
    INDEX [IX_NMT_MessageType] NONCLUSTERED ([MessageType]),
    INDEX [IX_NMT_ResourceType] NONCLUSTERED ([FhirResourceType])
);

-- ============================================================================
-- 9. NphiesMessageRequiredElement - Required fields for each message type
-- ============================================================================
CREATE TABLE [dbo].[NphiesMessageRequiredElement] (
    [NphiesMessageRequiredElementId] INT IDENTITY(1,1) PRIMARY KEY,
  [NphiesMessageTypeId] INT NOT NULL,
    [ElementPath]       NVARCHAR(400) NOT NULL,
    [ValueSetId]        INT NULL,
    [IsRequired]        BIT NOT NULL DEFAULT 1,
    [Cardinality]       NVARCHAR(20) NULL, -- 0..1, 1..1, 0..*, 1..*, etc.
    [Notes] NVARCHAR(MAX) NULL,
    
    CONSTRAINT [FK_NMRE_MessageType] FOREIGN KEY ([NphiesMessageTypeId]) 
     REFERENCES [dbo].[NphiesMessageType]([NphiesMessageTypeId]),
    CONSTRAINT [FK_NMRE_ValueSet] FOREIGN KEY ([ValueSetId]) 
        REFERENCES [dbo].[ValueSet]([ValueSetId])
);

-- ============================================================================
-- Indexes for Performance Optimization
-- ============================================================================
CREATE INDEX [IX_Concept_CodeSystemId_Active] ON [dbo].[Concept]([CodeSystemId], [IsActive]);
CREATE INDEX [IX_ValueSet_Active] ON [dbo].[ValueSet]([IsActive]);
CREATE INDEX [IX_ProfileElement_Active] ON [dbo].[ProfileElement]([IsActive]);
CREATE INDEX [IX_ValidationRule_Active] ON [dbo].[ValidationRule]([IsActive]);

-- ============================================================================
-- Views for Common Queries
-- ============================================================================

-- View: Get all active concepts for a ValueSet
CREATE VIEW [dbo].[vw_ValueSetConcepts]
AS
SELECT 
    vs.[ValueSetId],
    vs.[Url] AS [ValueSetUrl],
    vs.[Name] AS [ValueSetName],
    cs.[CodeSystemId],
 cs.[Url] AS [CodeSystemUrl],
    cs.[Name] AS [CodeSystemName],
    c.[ConceptId],
    c.[Code],
    c.[Display],
    c.[Definition]
FROM [dbo].[ValueSet] vs
INNER JOIN [dbo].[ValueSetCodeSystemMap] vscm ON vs.[ValueSetId] = vscm.[ValueSetId]
INNER JOIN [dbo].[CodeSystem] cs ON vscm.[CodeSystemId] = cs.[CodeSystemId]
INNER JOIN [dbo].[Concept] c ON cs.[CodeSystemId] = c.[CodeSystemId]
WHERE vs.[IsActive] = 1 
  AND cs.[IsActive] = 1 
  AND c.[IsActive] = 1;

-- View: Get all profile elements for a message type
CREATE VIEW [dbo].[vw_MessageTypeElements]
AS
SELECT 
    nmt.[MessageType],
    nmt.[FhirResourceType],
pe.[ProfileElementId],
    pe.[Path],
    pe.[Definition],
    vs.[Url] AS [ValueSetUrl],
    vs.[Name] AS [ValueSetName],
    pe.[BindingStrength],
    pe.[IsRequired]
FROM [dbo].[NphiesMessageType] nmt
INNER JOIN [dbo].[ProfileElement] pe ON nmt.[MessageType] = pe.[MessageType]
LEFT JOIN [dbo].[ValueSet] vs ON pe.[ValueSetId] = vs.[ValueSetId]
WHERE nmt.[IsActive] = 1 
  AND pe.[IsActive] = 1;

-- View: Validation context for a specific field path
CREATE VIEW [dbo].[vw_FieldValidationContext]
AS
SELECT 
    pe.[Path],
    pe.[ProfileName],
    pe.[MessageType],
    vs.[Url] AS [ValueSetUrl],
    cs.[Url] AS [CodeSystemUrl],
    COUNT(c.[ConceptId]) AS [AllowedConceptCount],
    STRING_AGG(c.[Code], ', ') AS [AllowedCodes]
FROM [dbo].[ProfileElement] pe
LEFT JOIN [dbo].[ValueSet] vs ON pe.[ValueSetId] = vs.[ValueSetId]
LEFT JOIN [dbo].[ValueSetCodeSystemMap] vscm ON vs.[ValueSetId] = vscm.[ValueSetId]
LEFT JOIN [dbo].[CodeSystem] cs ON vscm.[CodeSystemId] = cs.[CodeSystemId]
LEFT JOIN [dbo].[Concept] c ON cs.[CodeSystemId] = c.[CodeSystemId] AND c.[IsActive] = 1
WHERE pe.[IsActive] = 1
GROUP BY pe.[Path], pe.[ProfileName], pe.[MessageType], vs.[Url], cs.[Url];

PRINT 'NPHIES CodeableConcept Database Schema created successfully.';
