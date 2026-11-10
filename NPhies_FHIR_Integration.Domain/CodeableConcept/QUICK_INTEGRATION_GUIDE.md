# Week 2 - Quick Integration Guide

## ?? 2-Minute Integration

### Step 1: Enable Validation Middleware

**File**: `NPhies_FHIR_Integration.ApiService/Program.cs`

**Add this line** (around line 122, after `app.UseComprehensiveSecurity()`):

```csharp
app.UseComprehensiveSecurity(builder.Configuration);
app.UseMiddleware<CodeableConceptValidationMiddleware>(); // ? ADD THIS LINE
```

---

### Step 2: Integrate Claim Validator

**File**: `NPhies_FHIR_Integration.Application/Services/ClaimService.cs`

**Add constructor parameter**:
```csharp
private readonly ClaimCodeableConceptValidator _validator;

public ClaimService(
    IClaimRepository claimRepository,
    IMapper mapper,
    ILogger<ClaimService> logger,
ClaimCodeableConceptValidator validator) // ? ADD THIS
{
    _claimRepository = claimRepository;
    _mapper = mapper;
    _logger = logger;
    _validator = validator; // ? ADD THIS
}
```

**Add validation** in `SubmitClaimAsync` method:
```csharp
public async Task<ClaimDto> SubmitClaimAsync(ClaimDto claimDto)
{
    var claim = _mapper.Map<Claim>(claimDto);
    
    // ? ADD THIS BLOCK
    var validationResult = await _validator.ValidateClaimAsync(claim);
    if (!validationResult.IsValid)
    {
    _logger.LogWarning("Claim {ClaimNumber} validation failed: {Errors}", 
     claim.ClaimNumber, string.Join("; ", validationResult.Errors));
        throw new ValidationException(string.Join("; ", validationResult.Errors));
    }
    // ? END ADD
    
    // ... rest of your existing code
}
```

---

### Step 3: Test It

**Run the application**:
```bash
dotnet run --project NPhies_FHIR_Integration.ApiService
```

**Check the logs** for:
```
? ProfileElements seeded successfully
? Message Required Elements seeded successfully
? CodeableConcept database seeding completed successfully!
```

**Test an API endpoint**:
```bash
curl https://localhost:7001/api/codeableconcept/codesystems
```

---

## ?? Quick Test

### Test Validation Endpoint

```bash
curl -X POST https://localhost:7001/api/codeableconcept/validate \
  -H "Content-Type: application/json" \
  -d '{
    "code": "institutional",
    "codeSystemUrl": "http://terminology.hl7.org/CodeSystem/claim-type",
    "valueSetUrl": "http://nphies.sa/terminology/ValueSet/claim-type"
  }'
```

**Expected Response**:
```json
{
  "isValid": true,
  "errors": [],
  "concept": {
    "code": "institutional",
    "display": "Institutional",
    "isActive": true
  }
}
```

---

## ?? What Was Added

### New Files Created
1. `CodeableConceptValidationMiddleware.cs` - Auto-validates requests
2. `ClaimCodeableConceptValidator.cs` - Claim-specific validation
3. `ProfileElementSeeder.cs` - Seeds FHIR bindings
4. `MessageRequiredElementSeeder.cs` - Seeds required fields
5. `BulkImportConcepts.sql` - Import script for concepts
6. `WEEK2_IMPLEMENTATION_CHECKLIST.md` - Status tracking
7. `WEEK2_FINAL_SUMMARY.md` - Complete summary

### Updated Files
1. `CodeableConceptSeeder.cs` - Now calls new seeders
2. `Program.cs` - Registered new services

---

## ? Verification Checklist

- [ ] Middleware enabled in Program.cs
- [ ] Validator integrated in ClaimService
- [ ] Application starts without errors
- [ ] Seeding logs show success
- [ ] API endpoints return data
- [ ] Validation endpoint works

---

## ?? Troubleshooting

### Issue: "Table 'ProfileElements' doesn't exist"
**Solution**: Run migrations
```bash
dotnet ef database update --project NPhies_FHIR_Integration.Infrastructure
```

### Issue: "Seeding failed"
**Solution**: Check if data already exists
```sql
SELECT COUNT(*) FROM ProfileElements;
SELECT COUNT(*) FROM NphiesMessageRequiredElements;
```

### Issue: "Validation always returns true"
**Solution**: Check if concepts are seeded
```sql
SELECT COUNT(*) FROM Concepts;
```

---

## ?? Need Help?

Check these files:
- **README.md** - Detailed implementation guide
- **DELIVERY_SUMMARY.md** - System architecture
- **WEEK2_IMPLEMENTATION_CHECKLIST.md** - Full status
- **WEEK2_FINAL_SUMMARY.md** - Complete summary

---

## ?? That's It!

You're done! The CodeableConcept validation system is now integrated.

**Next**: Import bulk concept data using `BulkImportConcepts.sql`
