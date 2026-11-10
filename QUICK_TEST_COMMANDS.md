# ?? NPHIES Quick Test Commands

## Run Tests

```bash
# All tests
dotnet test

# Mapper tests only
dotnet test --filter "EntityToFhirMapper"

# Bundle service tests only
dotnet test --filter "FhirBundleService"

# With detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Generate Sample Bundles

```bash
# Run manual test
dotnet run --project NPhies_FHIR_Integration.Tests/Manual/ManualBundleCreationTest.cs

# Output files:
# - eligibility-request-YYYYMMDD-HHMMSS.json
# - claim-request-YYYYMMDD-HHMMSS.json
```

## Inspect Bundles

```bash
# Open in VS Code
code eligibility-request-*.json
code claim-request-*.json

# Pretty print in terminal
cat eligibility-request-*.json | jq '.'
```

## Validate with FHIR Validator

**Online**: https://validator.fhir.org/

**CLI** (if installed):
```bash
java -jar validator_cli.jar eligibility-request-*.json -version 4.0
```

## Test with NPHIES Sandbox

### Update Configuration
```json
{
  "Nphies": {
    "BaseUrl": "https://hsb.nphies.sa/fhir",
    "Auth": {
      "ClientId": "YOUR_SANDBOX_CLIENT_ID",
      "ClientSecret": "YOUR_SANDBOX_SECRET"
    }
  }
}
```

### Run API
```bash
dotnet run --project NPhies_FHIR_Integration.ApiService
```

### Test Endpoints
```bash
# Health check
curl https://localhost:7001/health

# Create eligibility request (use your API endpoint)
curl -X POST https://localhost:7001/api/eligibility/submit \
  -H "Content-Type: application/json" \
  -d @test-data.json
```

---

## Quick Checklist

- [ ] `dotnet test` - All pass ?
- [ ] Generate bundles - JSON files created ?
- [ ] Inspect JSON - Structure looks good ?
- [ ] Validate FHIR - No errors ?
- [ ] Get sandbox credentials
- [ ] Update appsettings.json
- [ ] Submit to sandbox
- [ ] Parse response
- [ ] All tests pass! ??

---

**Status**: ? Ready for NPHIES Sandbox Testing!
