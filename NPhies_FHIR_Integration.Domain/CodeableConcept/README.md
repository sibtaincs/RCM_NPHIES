# NPHIES CodeableConcept Database Design & Implementation Guide

## Overview

This document provides a complete guide for implementing the **NPHIES CodeableConcept Database** for your NPhies_FHIR_Integration project (.NET 9).

The database manages all NPHIES terminology (CodeSystems, ValueSets, Concepts) and provides validation for claim processing, eligibility checks, authorizations, and all other NPHIES message types.

---

## Architecture

### Database Schema (9 Tables)

```
CodeSystem (1) ????????????< Concept (Many)
    ?
    ?? ValueSetCodeSystemMap (M:N)
           ?
    ValueSet (1) ????????????< ProfileElement
          ?
            ??< ConceptCodeFilter

NphiesMessageType (1) ????????????< NphiesMessageRequiredElement
    ?
 ??> ValidationRule
```

### Core Tables

| Table | Purpose | Records |
|-------|---------|---------|
| **CodeSystem** | Metadata for all terminology sources (HL7, NPHIES, WHO) | ~180 |
| **Concept** | Individual codes within each CodeSystem | ~50,000+ |
| **ValueSet** | Logical groups of codes for specific use cases | ~120 |
| **ValueSetCodeSystemMap** | Links ValueSets to CodeSystems (M:N) | ~300 |
| **ProfileElement** | FHIR path bindings to ValueSets | ~500 |
| **ConceptCodeFilter** | Specific code restrictions for ValueSets | ~100 |
| **ValidationRule** | NPHIES-specific validation constraints | 1,682+ |
| **NphiesMessageType** | Message type definitions (eligibility, claim, etc.) | 14 |
| **NphiesMessageRequiredElement** | Required fields per message type | 1,000+ |

---

## Implementation Steps

### Step 1: Create Database Schema

Run the SQL script to create all tables and indexes:

```sql
-- File: NPhies_FHIR_Integration.Domain/CodeableConcept/DatabaseSchema.sql
-- This creates all 9 tables with proper relationships and indexes
```

**SQL Server Connection String Example:**
```json
{
  "ConnectionStrings": {
    "NPhiesCodeableConcept": "Server=YOUR_SERVER;Database=NPhiesCodeableConcept;Trusted_Connection=true;Encrypt=true;TrustServerCertificate=true;"
  }
}
```

### Step 2: Add Entity Framework Models

Already provided in: `NPhies_FHIR_Integration.Domain/CodeableConcept/CodeableConceptModels.cs`

These models include:
- Entity classes (CodeSystemEntity, ConceptEntity, ValueSetEntity, etc.)
- DTO classes for API responses
- Relationships and constraints

### Step 3: Configure DbContext

Already provided in: `NPhies_FHIR_Integration.Infrastructure/Persistence/CodeableConceptDbContext.cs`

Register in your DI container (Program.cs or Startup.cs):

```csharp
services.AddDbContext<ICodeableConceptDbContext, CodeableConceptDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("NPhiesCodeableConcept"))
);
```

### Step 4: Import Terminology Data

Use the import scripts to populate the database:

**File:** `NPhies_FHIR_Integration.Domain/CodeableConcept/ImportDataInitial.sql`

This script seeds:
1. All CodeSystems (HL7, NPHIES, WHO)
2. All ValueSets
3. ValueSet-to-CodeSystem mappings
4. Message type definitions

### Step 5: Create Service Layer

Already provided in: `NPhies_FHIR_Integration.Application/Services/CodeableConceptService.cs`

This service provides:
- **CodeSystem Operations**: Get by URL/name, list all
- **Concept Operations**: Get concept details, validate codes
- **ValueSet Operations**: Get by URL/name, check membership
- **Validation Operations**: Validate codes against ValueSets, rules, message types
- **Message Type Operations**: Get required elements for NPHIES messages

### Step 6: Register Service in DI

Add to your dependency injection container:

```csharp
services.AddScoped<ICodeableConceptService, CodeableConceptService>();
```

---

## Usage Examples

### Example 1: Validate a Claim Item Code

```csharp
var service = serviceProvider.GetRequiredService<ICodeableConceptService>();

// Validate that code "12345" is valid in "institutional-billing" ValueSet
var result = await service.ValidateCodeAsync(
    code: "12345",
    codeSystemUrl: "http://nphies.sa/terminology/CodeSystem/procedures",
    valueSetUrl: "http://nphies.sa/terminology/ValueSet/institutional-billing"
);

if (result.IsValid)
{
    Console.WriteLine($"Code is valid: {result.Concept.Display}");
}
else
{
    foreach (var error in result.Errors)
        Console.WriteLine($"Error: {error}");
}
```

### Example 2: Get Allowed Codes for a Field

```csharp
// Get all allowed codes for Claim.item.productOrService in an institutional claim
var elements = await service.GetMessageRequiredElementsAsync("claim-request");
var productOrServiceElement = elements.FirstOrDefault(e => e.ElementPath == "Claim.item.productOrService");

if (productOrServiceElement != null)
{
    var valueSet = await service.GetValueSetByUrlAsync(productOrServiceElement.ValueSetUrl);
    var concepts = await service.GetValueSetConceptsAsync(valueSet.ValueSetId);
    
    foreach (var concept in concepts)
   Console.WriteLine($"{concept.Code}: {concept.Display}");
}
```

### Example 3: Validate Entire Claim Message

```csharp
// Validate all required fields for a claim
var requiredElements = await service.GetMessageRequiredElementsAsync("claim-request");

var claimData = new { /* your claim data */ };
var validationErrors = new List<string>();

foreach (var element in requiredElements.Where(e => e.IsRequired))
{
    var code = GetCodeFromClaimData(claimData, element.ElementPath);
    var context = await service.GetValidationContextAsync("claim-request", element.ElementPath);
    
    var result = await service.ValidateCodeAsync(code, context.CodeSystemUrl, context.ValueSetUrl);
    if (!result.IsValid)
        validationErrors.AddRange(result.Errors);
}

if (validationErrors.Any())
 throw new ValidationException($"Claim validation failed: {string.Join("; ", validationErrors)}");
```

### Example 4: Get Validation Rules for a Field

```csharp
// Get all validation rules for Claim.diagnosis.type
var rules = await service.GetValidationRulesForFieldAsync("Claim.diagnosis.type");

foreach (var rule in rules)
{
 Console.WriteLine($"{rule.ErrorCode}: {rule.ErrorMessage}");
    if (rule.Severity == "error")
      Console.WriteLine("  ?? This is a critical error");
}
```

---

## Data Import Strategy

### Phase 1: CodeSystems (Already included)
- All HL7 CodeSystems (claim-type, fm-status, etc.)
- All NPHIES CodeSystems (claim-subtype, diagnosis-type, etc.)
- WHO CodeSystems (diagnosis-related-group, etc.)

### Phase 2: Concepts (To be bulk-imported)

For each CodeSystem, create a CSV file and bulk import:

```sql
-- Example: Import FDI Tooth Surface codes
BULK INSERT Concept
FROM 'C:\nphies\fdi-tooth-surface.csv'
WITH (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '\n',
    CODEPAGE = '65001'
);
```

CSV format:
```csv
CodeSystemId,Code,Display,Definition,IsActive
1,M,Mesial,The surface of a tooth closest to midline,1
1,O,Occlusal,The chewing surface of posterior teeth,1
```

### Phase 3: ValueSets & Mappings (Partially included)

Create a CSV for ValueSet mappings:

```csv
ValueSetName,CodeSystemName,Sequence
institutional-billing,procedures,1
institutional-billing,services,2
institutional-billing,medication-codes,3
```

### Phase 4: Message Types & Required Elements (To be completed)

Define all NPHIES message types and their required fields:

```sql
INSERT INTO NphiesMessageType VALUES
('claim-request', 'Claim', 'Request for claim adjudication', 1),
('claim-response', 'ClaimResponse', 'Response with claim adjudication', 1),
('eligibility-request', 'CoverageEligibilityRequest', 'Request for eligibility check', 1),
...;

INSERT INTO NphiesMessageRequiredElement VALUES
(1, 'Claim.type', 2, 1, '1..1', 'Type of claim'),
(1, 'Claim.subType', 3, 1, '1..1', 'Sub-type of claim'),
(1, 'Claim.item.productOrService', 4, 1, '1..*', 'Product or service code'),
...;
```

---

## Entity Framework Migrations

### Create Migration

```bash
dotnet ef migrations add "InitialCodeableConceptSchema" \
  --project NPhies_FHIR_Integration.Infrastructure \
  --startup-project NPhies_FHIR_Integration.ApiService \
  --context CodeableConceptDbContext
```

### Apply Migration

```bash
dotnet ef database update \
  --project NPhies_FHIR_Integration.Infrastructure \
  --startup-project NPhies_FHIR_Integration.ApiService \
  --context CodeableConceptDbContext
```

---

## API Endpoints (To be Implemented)

```csharp
[ApiController]
[Route("api/[controller]")]
public class CodeableConceptController : ControllerBase
{
    private readonly ICodeableConceptService _service;
    
    // GET /api/codeableconcept/codesystems/{url}
    [HttpGet("codesystems/{url}")]
    public async Task<CodeSystemDto> GetCodeSystem(string url)
 
    // GET /api/codeableconcept/valuesets/{url}
    [HttpGet("valuesets/{url}")]
    public async Task<ValueSetDto> GetValueSet(string url)
    
    // POST /api/codeableconcept/validate
    [HttpPost("validate")]
    public async Task<ValidationResultDto> ValidateCode([FromBody] ValidateCodeRequest request)
 
    // GET /api/codeableconcept/messages/{messageType}/elements
    [HttpGet("messages/{messageType}/elements")]
    public async Task<IEnumerable<NphiesMessageRequiredElementDto>> GetMessageElements(string messageType)
}
```

---

## Performance Optimization

### Indexes Created

All tables have strategic indexes for:
- **Primary keys**: Identity columns
- **Foreign keys**: Fast relationships
- **Unique constraints**: URL, code combinations
- **Active filters**: IsActive flags
- **Search columns**: Name, Path, MessageType

### Query Optimization Tips

```csharp
// ? GOOD: Use AsNoTracking for read-only queries
var concepts = await dbContext.Concepts
    .AsNoTracking()
    .Where(c => c.CodeSystemId == id && c.IsActive)
    .ToListAsync();

// ? GOOD: Use Include for known relationships
var valueSets = await dbContext.ValueSets
    .AsNoTracking()
  .Include(vs => vs.CodeSystemMappings)
    .Where(vs => vs.IsActive)
    .ToListAsync();

// ? AVOID: N+1 queries
var concepts = dbContext.Concepts.ToList(); // Loads all
foreach (var c in concepts)
{
    var cs = dbContext.CodeSystems.Find(c.CodeSystemId); // N queries!
}
```

---

## Testing

### Unit Tests

```csharp
[TestClass]
public class CodeableConceptServiceTests
{
    private Mock<ICodeableConceptDbContext> _mockContext;
    private ICodeableConceptService _service;
    
    [TestInitialize]
    public void Setup()
    {
   _mockContext = new Mock<ICodeableConceptDbContext>();
 _service = new CodeableConceptService(_mockContext.Object);
    }
    
    [TestMethod]
    public async Task ValidateCodeAsync_WithValidCode_ReturnsSuccess()
    {
 // Arrange
        var codeSystem = new CodeSystemEntity { CodeSystemId = 1, Url = "http://test", IsActive = true };
        var concept = new ConceptEntity { Code = "TEST", CodeSystemId = 1, IsActive = true };
    
   // Act
        var result = await _service.ValidateCodeAsync("TEST", "http://test");
        
        // Assert
        Assert.IsTrue(result.IsValid);
    }
}
```

---

## Data Maintenance

### Update Terminology (Quarterly)

```sql
-- Archive old versions
UPDATE CodeSystem SET IsActive = 0 
WHERE Version < 'X.Y.Z';

-- Insert new version
INSERT INTO CodeSystem (...) 
VALUES (...);

-- Update concepts
INSERT INTO Concept (...)
SELECT ... FROM [ImportStaging].[Concepts];
```

### Monitor Database Size

```sql
SELECT 
    TABLE_NAME,
    (CAST(SUM(ps.reserved_page_count) * 8.0 / 1024 / 1024 AS DECIMAL(15,2))) AS SizeMB
FROM sys.dm_db_partition_stats AS ps
GROUP BY TABLE_NAME
ORDER BY SizeMB DESC;
```

---

## Related Files

| File | Purpose |
|------|---------|
| `DatabaseSchema.sql` | DDL for all tables and indexes |
| `ImportDataInitial.sql` | Initial data seeding |
| `CodeableConceptModels.cs` | Entity and DTO classes |
| `CodeableConceptDbContext.cs` | EF Core DbContext |
| `CodeableConceptService.cs` | Business logic layer |
| `CodeableConceptController.cs` | (To be created) REST API |

---

## Troubleshooting

### Issue: "CodeSystem not found"
- Check CodeSystem.Url spelling
- Ensure CodeSystem.IsActive = 1
- Verify bulk import completed successfully

### Issue: "Code not in ValueSet"
- Ensure Concept.IsActive = 1
- Check ValueSetCodeSystemMap exists
- Verify Concept belongs to correct CodeSystem

### Issue: Slow queries
- Check indexes are created
- Use EXPLAIN PLAN to analyze
- Consider caching frequently accessed ValueSets

---

## Next Steps

1. **Run DatabaseSchema.sql** to create tables
2. **Run ImportDataInitial.sql** to seed basic data
3. **Create EF migrations** for your CI/CD pipeline
4. **Implement API layer** for REST access
5. **Bulk import concept codes** from Excel files
6. **Add caching layer** for high-traffic ValueSets
7. **Create validation middleware** for incoming claims

---

## References

- **NPHIES Specification**: https://www.nphies.sa/
- **FHIR Terminology**: https://www.hl7.org/fhir/terminology-module.html
- **Entity Framework Documentation**: https://learn.microsoft.com/en-us/ef/core/
- **SQL Server Best Practices**: https://learn.microsoft.com/en-us/sql/

---

**Version**: 1.0.0  
**Last Updated**: 2024  
**Maintained By**: NPHIES Integration Team
