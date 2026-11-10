-- ============================================================================
-- NPHIES CodeableConcept SQLite Database Schema
-- Target: SQLite 3.40+
-- Version: 1.0.0
-- Purpose: Lightweight terminology database for NPHIES FHIR integration
--          with JSON support for flexible data storage
-- ============================================================================

-- ============================================================================
-- 1. CodeSystem Table - Metadata for all CodeSystems
-- ============================================================================
CREATE TABLE IF NOT EXISTS CodeSystem (
    CodeSystemId    INTEGER PRIMARY KEY AUTOINCREMENT,
    [Url]           TEXT NOT NULL UNIQUE,
    [Version]       TEXT,
    [Name]     TEXT NOT NULL,
    [Title]         TEXT,
    [Definition]  TEXT,
    [Committee]     TEXT,
    [Oid]           TEXT,
    [Copyright]     TEXT,
    [SourceResource] TEXT,
    CaseSensitive   INTEGER NOT NULL DEFAULT 1,
    IsActive  INTEGER NOT NULL DEFAULT 1,
    CreatedAt       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt       DATETIME,
    MetadataJson    TEXT  -- JSON for extensibility
);

CREATE INDEX IF NOT EXISTS IX_CodeSystem_Url ON CodeSystem([Url]);
CREATE INDEX IF NOT EXISTS IX_CodeSystem_Name ON CodeSystem([Name]);
CREATE INDEX IF NOT EXISTS IX_CodeSystem_IsActive ON CodeSystem(IsActive);

-- ============================================================================
-- 2. Concept Table - Individual codes within a CodeSystem
-- ============================================================================
CREATE TABLE IF NOT EXISTS Concept (
    ConceptId       INTEGER PRIMARY KEY AUTOINCREMENT,
    CodeSystemId    INTEGER NOT NULL,
    [Code]    TEXT NOT NULL,
    [Display]       TEXT,
    [Definition]    TEXT,
    DisplayArabic   TEXT,
    DefinitionArabic TEXT,
  IsActive  INTEGER NOT NULL DEFAULT 1,
    SortOrder  INTEGER,
    ParentCode      TEXT,
    CreatedAt    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PropertiesJson  TEXT,  -- JSON for additional properties

  FOREIGN KEY (CodeSystemId) REFERENCES CodeSystem(CodeSystemId) ON DELETE CASCADE,
    UNIQUE(CodeSystemId, [Code])
);

CREATE INDEX IF NOT EXISTS IX_Concept_Code ON Concept([Code]);
CREATE INDEX IF NOT EXISTS IX_Concept_CodeSystem ON Concept(CodeSystemId);
CREATE INDEX IF NOT EXISTS IX_Concept_Active ON Concept(IsActive);
CREATE INDEX IF NOT EXISTS IX_Concept_CodeSystemActive ON Concept(CodeSystemId, IsActive);

-- ============================================================================
-- 3. ValueSet Table - Collections of codes from one or more CodeSystems
-- ============================================================================
CREATE TABLE IF NOT EXISTS ValueSet (
    ValueSetIdINTEGER PRIMARY KEY AUTOINCREMENT,
    [Url]       TEXT NOT NULL UNIQUE,
    [Version]       TEXT,
    [Name]       TEXT NOT NULL,
    [Title] TEXT,
    [Definition]    TEXT,
    [Committee]     TEXT,
    [Oid]           TEXT,
    [Copyright]   TEXT,
    [SourceResource] TEXT,
    Restrictions    TEXT,  -- JSON array of restrictions
    IsActive   INTEGER NOT NULL DEFAULT 1,
    CreatedAt       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt       DATETIME,
 MetadataJson TEXT
);

CREATE INDEX IF NOT EXISTS IX_ValueSet_Url ON ValueSet([Url]);
CREATE INDEX IF NOT EXISTS IX_ValueSet_Name ON ValueSet([Name]);
CREATE INDEX IF NOT EXISTS IX_ValueSet_IsActive ON ValueSet(IsActive);

-- ============================================================================
-- 4. ValueSet_CodeSystem_Map - M:N relationship
-- ============================================================================
CREATE TABLE IF NOT EXISTS ValueSetCodeSystemMap (
    ValueSetCodeSystemMapId INTEGER PRIMARY KEY AUTOINCREMENT,
    ValueSetId   INTEGER NOT NULL,
  CodeSystemId    INTEGER NOT NULL,
    [Sequence]   INTEGER,
    IsActive        INTEGER NOT NULL DEFAULT 1,
    
    FOREIGN KEY (ValueSetId) REFERENCES ValueSet(ValueSetId) ON DELETE CASCADE,
FOREIGN KEY (CodeSystemId) REFERENCES CodeSystem(CodeSystemId) ON DELETE CASCADE,
    UNIQUE(ValueSetId, CodeSystemId)
);

CREATE INDEX IF NOT EXISTS IX_VSCM_ValueSet ON ValueSetCodeSystemMap(ValueSetId);
CREATE INDEX IF NOT EXISTS IX_VSCM_CodeSystem ON ValueSetCodeSystemMap(CodeSystemId);

-- ============================================================================
-- 5. ProfileElement - FHIR Profile paths and their ValueSet bindings
-- ============================================================================
CREATE TABLE IF NOT EXISTS ProfileElement (
    ProfileElementId INTEGER PRIMARY KEY AUTOINCREMENT,
    ProfileName     TEXT NOT NULL,
    [Path]          TEXT NOT NULL,
    PathArabic      TEXT,
    [Definition]    TEXT,
    ValueSetId      INTEGER,
    BindingStrength TEXT,  -- required, extensible, preferred, example
MessageType     TEXT,  -- eligibility-request, claim-request, etc.
    ResourceType    TEXT,-- Claim, CoverageEligibilityRequest, etc.
    IsRequired      INTEGER NOT NULL DEFAULT 0,
    IsActiveINTEGER NOT NULL DEFAULT 1,
  CreatedAt       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ConfigJson      TEXT,
    
  FOREIGN KEY (ValueSetId) REFERENCES ValueSet(ValueSetId) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS IX_PE_Path ON ProfileElement([Path]);
CREATE INDEX IF NOT EXISTS IX_PE_MessageType ON ProfileElement(MessageType);
CREATE INDEX IF NOT EXISTS IX_PE_ResourceType ON ProfileElement(ResourceType);
CREATE INDEX IF NOT EXISTS IX_PE_ProfileName ON ProfileElement(ProfileName);

-- ============================================================================
-- 6. ConceptCodeFilter - For ValueSets that restrict specific codes
-- ============================================================================
CREATE TABLE IF NOT EXISTS ConceptCodeFilter (
    ConceptCodeFilterId INTEGER PRIMARY KEY AUTOINCREMENT,
    ValueSetId      INTEGER NOT NULL,
    [Code]        TEXT NOT NULL,
    [Display]       TEXT,
    Notes         TEXT,
    IsActive        INTEGER NOT NULL DEFAULT 1,
 
    FOREIGN KEY (ValueSetId) REFERENCES ValueSet(ValueSetId) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_CCF_ValueSet ON ConceptCodeFilter(ValueSetId);
CREATE INDEX IF NOT EXISTS IX_CCF_Code ON ConceptCodeFilter([Code]);

-- ============================================================================
-- 7. ValidationRule - NPHIES-specific validation constraints
-- ============================================================================
CREATE TABLE IF NOT EXISTS ValidationRule (
 ValidationRuleId INTEGER PRIMARY KEY AUTOINCREMENT,
    ErrorCode       TEXT NOT NULL UNIQUE,
    ErrorMessage    TEXT NOT NULL,
    ErrorMessageArabic TEXT,
    FieldPath       TEXT,
    RuleType        TEXT,  -- CodeExists, SystemMustBe, ValueSetMembership, etc.
  Parameters  TEXT,  -- JSON for rule parameters
    Severity        TEXT,  -- error, warning, info
    IsActive        INTEGER NOT NULL DEFAULT 1,
    CreatedAt       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    RuleJson        TEXT
);

CREATE INDEX IF NOT EXISTS IX_VR_ErrorCode ON ValidationRule(ErrorCode);
CREATE INDEX IF NOT EXISTS IX_VR_FieldPath ON ValidationRule(FieldPath);
CREATE INDEX IF NOT EXISTS IX_VR_RuleType ON ValidationRule(RuleType);

-- ============================================================================
-- 8. NphiesMessageType - Mapping of NPHIES message types
-- ============================================================================
CREATE TABLE IF NOT EXISTS NphiesMessageType (
    NphiesMessageTypeId INTEGER PRIMARY KEY AUTOINCREMENT,
    MessageType     TEXT NOT NULL UNIQUE,
    MessageTypeArabic TEXT,
    FhirResourceType TEXT NOT NULL,
    [Description]   TEXT,
    [Version]       TEXT,
    IsActive      INTEGER NOT NULL DEFAULT 1,
    MetadataJson    TEXT
);

CREATE INDEX IF NOT EXISTS IX_NMT_MessageType ON NphiesMessageType(MessageType);
CREATE INDEX IF NOT EXISTS IX_NMT_ResourceType ON NphiesMessageType(FhirResourceType);

-- ============================================================================
-- 9. NphiesMessageRequiredElement - Required fields for each message type
-- ============================================================================
CREATE TABLE IF NOT EXISTS NphiesMessageRequiredElement (
    NphiesMessageRequiredElementId INTEGER PRIMARY KEY AUTOINCREMENT,
    NphiesMessageTypeId INTEGER NOT NULL,
    ElementPath     TEXT NOT NULL,
    ValueSetId      INTEGER,
    IsRequired      INTEGER NOT NULL DEFAULT 1,
    Cardinality     TEXT,  -- 0..1, 1..1, 0..*, 1..*, etc.
    Notes        TEXT,
    
    FOREIGN KEY (NphiesMessageTypeId) REFERENCES NphiesMessageType(NphiesMessageTypeId) ON DELETE CASCADE,
    FOREIGN KEY (ValueSetId) REFERENCES ValueSet(ValueSetId) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS IX_NMRE_MessageType ON NphiesMessageRequiredElement(NphiesMessageTypeId);
CREATE INDEX IF NOT EXISTS IX_NMRE_ValueSet ON NphiesMessageRequiredElement(ValueSetId);

-- ============================================================================
-- 10. JsonCache - Store JSON responses for faster retrieval
-- ============================================================================
CREATE TABLE IF NOT EXISTS JsonCache (
    JsonCacheId     INTEGER PRIMARY KEY AUTOINCREMENT,
    CacheKey        TEXT NOT NULL UNIQUE,
    JsonData      TEXT NOT NULL,
    EntityType TEXT,  -- CodeSystem, ValueSet, etc.
    EntityId        INTEGER,
    ExpiresAt DATETIME,
    CreatedAt       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    HitCountINTEGER DEFAULT 0
);

CREATE INDEX IF NOT EXISTS IX_JsonCache_Key ON JsonCache(CacheKey);
CREATE INDEX IF NOT EXISTS IX_JsonCache_Expires ON JsonCache(ExpiresAt);

-- ============================================================================
-- 11. AuditLog - Track all changes to terminology
-- ============================================================================
CREATE TABLE IF NOT EXISTS AuditLog (
    AuditLogId      INTEGER PRIMARY KEY AUTOINCREMENT,
    EntityType      TEXT NOT NULL,  -- CodeSystem, Concept, ValueSet, etc.
    EntityId        INTEGER,
    Operation       TEXT NOT NULL,  -- INSERT, UPDATE, DELETE
    OldValues     TEXT,  -- JSON
    NewValues       TEXT,  -- JSON
    ChangedBy       TEXT,
    ChangedAt       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS IX_AuditLog_Entity ON AuditLog(EntityType, EntityId);
CREATE INDEX IF NOT EXISTS IX_AuditLog_Date ON AuditLog(ChangedAt);

-- ============================================================================
-- VIEWS FOR COMMON QUERIES
-- ============================================================================

-- View: Get all active concepts for a ValueSet
CREATE VIEW IF NOT EXISTS vw_ValueSetConcepts AS
SELECT 
    vs.ValueSetId,
    vs.[Url] AS ValueSetUrl,
    vs.[Name] AS ValueSetName,
    cs.CodeSystemId,
    cs.[Url] AS CodeSystemUrl,
    cs.[Name] AS CodeSystemName,
    c.ConceptId,
 c.[Code],
    c.[Display],
 c.[Definition],
    c.DisplayArabic,
    c.DefinitionArabic
FROM ValueSet vs
INNER JOIN ValueSetCodeSystemMap vscm ON vs.ValueSetId = vscm.ValueSetId
INNER JOIN CodeSystem cs ON vscm.CodeSystemId = cs.CodeSystemId
INNER JOIN Concept c ON cs.CodeSystemId = c.CodeSystemId
WHERE vs.IsActive = 1 
  AND cs.IsActive = 1 
  AND c.IsActive = 1
ORDER BY vs.[Name], c.SortOrder, c.[Code];

-- View: Get all profile elements for a message type
CREATE VIEW IF NOT EXISTS vw_MessageTypeElements AS
SELECT 
    nmt.MessageType,
    nmt.FhirResourceType,
    pe.ProfileElementId,
    pe.[Path],
    pe.[Definition],
    vs.[Url] AS ValueSetUrl,
    vs.[Name] AS ValueSetName,
    pe.BindingStrength,
pe.IsRequired
FROM NphiesMessageType nmt
INNER JOIN ProfileElement pe ON nmt.MessageType = pe.MessageType
LEFT JOIN ValueSet vs ON pe.ValueSetId = vs.ValueSetId
WHERE nmt.IsActive = 1 
  AND pe.IsActive = 1
ORDER BY nmt.MessageType, pe.[Path];

-- View: Validation context for a specific field path
CREATE VIEW IF NOT EXISTS vw_FieldValidationContext AS
SELECT 
    pe.[Path],
    pe.ProfileName,
    pe.MessageType,
    vs.[Url] AS ValueSetUrl,
    cs.[Url] AS CodeSystemUrl,
    COUNT(c.ConceptId) AS AllowedConceptCount,
    pe.IsRequired,
    pe.BindingStrength
FROM ProfileElement pe
LEFT JOIN ValueSet vs ON pe.ValueSetId = vs.ValueSetId
LEFT JOIN ValueSetCodeSystemMap vscm ON vs.ValueSetId = vscm.ValueSetId
LEFT JOIN CodeSystem cs ON vscm.CodeSystemId = cs.CodeSystemId
LEFT JOIN Concept c ON cs.CodeSystemId = c.CodeSystemId AND c.IsActive = 1
WHERE pe.IsActive = 1
GROUP BY pe.[Path], pe.ProfileName, pe.MessageType, vs.[Url], cs.[Url], pe.IsRequired, pe.BindingStrength;

-- ============================================================================
-- STORED PROCEDURES (SQLite doesn't have traditional stored procedures,
-- but we provide SQL for common operations)
-- ============================================================================

-- Function to get ValueSet with all concepts
-- SELECT * FROM vw_ValueSetConcepts WHERE ValueSetUrl = 'http://...';

-- Function to validate code
-- SELECT COUNT(*) FROM Concept c
-- WHERE c.[Code] = 'CODE' 
--   AND c.CodeSystemId = (SELECT CodeSystemId FROM CodeSystem WHERE [Url] = 'http://...')
--   AND c.IsActive = 1;

-- ============================================================================
-- INDEXES FOR PERFORMANCE
-- ============================================================================

-- Composite indexes for common queries
CREATE INDEX IF NOT EXISTS IX_Concept_Search ON Concept(CodeSystemId, [Code], IsActive);
CREATE INDEX IF NOT EXISTS IX_ValueSet_Search ON ValueSet([Name], IsActive);
CREATE INDEX IF NOT EXISTS IX_PE_Search ON ProfileElement(MessageType, ResourceType, IsActive);

-- ============================================================================
-- TRIGGERS (optional - for SQLite compatibility)
-- ============================================================================

-- Trigger to update UpdatedAt on CodeSystem
CREATE TRIGGER IF NOT EXISTS tr_CodeSystem_Updated 
AFTER UPDATE ON CodeSystem
BEGIN
    UPDATE CodeSystem SET UpdatedAt = CURRENT_TIMESTAMP 
    WHERE CodeSystemId = NEW.CodeSystemId;
END;

-- Trigger to update UpdatedAt on ValueSet
CREATE TRIGGER IF NOT EXISTS tr_ValueSet_Updated 
AFTER UPDATE ON ValueSet
BEGIN
    UPDATE ValueSet SET UpdatedAt = CURRENT_TIMESTAMP 
    WHERE ValueSetId = NEW.ValueSetId;
END;

-- ============================================================================
-- FINAL VERIFICATION
-- ============================================================================

PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;-- Write-Ahead Logging for better concurrency

-- Insert comment to confirm creation
-- SELECT 'NPHIES CodeableConcept SQLite Database Schema Created Successfully' AS Status;

