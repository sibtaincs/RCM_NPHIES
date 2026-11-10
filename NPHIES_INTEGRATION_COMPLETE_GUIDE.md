# ?? NPHIES FHIR Integration - Complete Technical Guide

## ?? Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Authentication & Authorization](#authentication--authorization)
3. [Transport Security (TLS/HTTPS)](#transport-security)
4. [Eligibility Check Workflow](#eligibility-check-workflow)
5. [Pre-Authorization Workflow](#pre-authorization-workflow)
6. [Claim Submission Workflow](#claim-submission-workflow)
7. [Polling Mechanism](#polling-mechanism)
8. [Error Handling](#error-handling)
9. [Code Examples](#code-examples)

---

## ??? Architecture Overview

### Layer Structure

```
???????????????????????????????????????????????????????????
?        API Controllers   ?
?            (NPhies_FHIR_Integration.ApiService)         ?
???????????????????????????????????????????????????????????
              ?
???????????????????????????????????????????????????????????
?              Orchestration Services     ?
?   (NphiesOrchestrationService - Coordinates Workflows)  ?
???????????????????????????????????????????????????????????
      ?
???????????????????????????????????????????????????????????
?           Core Services Layer   ?
?  ?? FhirBundleService (Create FHIR Bundles)   ?
?  ?? AuthenticationService (OAuth2 Tokens)     ?
?  ?? NphiesHttpClient (HTTP Communication)        ?
?  ?? NphiesPollingService (Async Response Polling)     ?
???????????????????????????????????????????????????????????
           ?
???????????????????????????????????????????????????????????
?     Infrastructure Layer        ?
?  ?? NPhiesApiClient (Low-level API calls)     ?
?  ?? HttpClient with Polly Policies             ?
?  ?? Entity Framework Core (Database)               ?
???????????????????????????????????????????????????????????
                ?
???????????????????????????????????????????????????????????
? NPHIES API     ?
?        (https://nphies.sa/api/fhir)     ?
???????????????????????????????????????????????????????????
```

---

## ?? Authentication & Authorization

### 1. OAuth2 Client Credentials Flow

**File:** `NPhies_FHIR_Integration.Application/Services/Auth/NphiesAuthenticationService.cs`

```csharp
public interface IAuthenticationService
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task<string> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<bool> IsTokenValidAsync(string token);
}
```

### How Authentication Works:

#### Step 1: Configuration
```json
{
  "Nphies": {
    "Auth": {
      "Authority": "https://auth.nphies.sa",
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret",
      "Scope": "nphies.api",
"TokenEndpoint": "/oauth2/token",
      "TokenCacheDurationMinutes": 50
  }
  }
}
```

#### Step 2: Token Request
```csharp
// Authentication service requests token
var tokenRequest = new Dictionary<string, string>
{
    { "grant_type", "client_credentials" },
    { "client_id", _config.ClientId },
    { "client_secret", _config.ClientSecret },
    { "scope", _config.Scope }
};

var response = await _httpClient.PostAsync(
    _config.TokenEndpoint, 
    new FormUrlEncodedContent(tokenRequest)
);
```

#### Step 3: Token Caching
```csharp
// Token is cached in memory to avoid repeated requests
if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiryTime)
{
    return _cachedAccessToken; // Return cached token
}
```

#### Step 4: Token Usage
```csharp
// Token is added to all API requests
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

### Key Files:
- **Interface:** `Application/Services/Auth/IAuthenticationService.cs`
- **Implementation:** `Application/Services/Auth/NphiesAuthenticationService.cs`
- **Configuration:** `Application/Configuration/NphiesConfiguration.cs`

---

## ?? Transport Security (TLS/HTTPS)

### HTTP Client Configuration

**File:** `Application/Extensions/ServiceCollectionExtensions.cs`

```csharp
services.AddHttpClient<INphiesHttpClient, NphiesHttpClient>("NphiesHttpClient")
    .ConfigureHttpClient((sp, client) =>
  {
     var config = sp.GetRequiredService<IOptions<NphiesConfiguration>>().Value;
     client.BaseAddress = new Uri(config.BaseUrl);  // HTTPS endpoint
        client.Timeout = TimeSpan.FromSeconds(config.Timeout);
        client.DefaultRequestHeaders.Add("Accept", "application/fhir+json");
   client.DefaultRequestHeaders.Add("User-Agent", "NPHIES-FHIR-Integration/1.0");
    })
    .AddPolicyHandler(GetRetryPolicy())   // Retry on transient failures
    .AddPolicyHandler(GetCircuitBreakerPolicy())  // Circuit breaker pattern
    .AddPolicyHandler(GetTimeoutPolicy());        // Timeout policy
```

### Resilience Policies (Polly)

#### 1. Retry Policy (Exponential Backoff)
```csharp
private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()  // Handle 5xx and 408
        .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests) // 429
 .WaitAndRetryAsync(
  retryCount: 3,
    sleepDurationProvider: retryAttempt => 
   TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))// 2s, 4s, 8s
  );
}
```

#### 2. Circuit Breaker
```csharp
private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
      handledEventsAllowedBeforeBreaking: 5,  // Open after 5 failures
         durationOfBreak: TimeSpan.FromSeconds(30)  // Stay open for 30s
        );
}
```

#### 3. Timeout Policy
```csharp
private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
{
    return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));
}
```

### Security Features:
? **TLS 1.2+** enforced for all connections  
? **OAuth2 Bearer tokens** for authentication  
? **Retry with exponential backoff** for transient failures  
? **Circuit breaker** to prevent cascading failures  
? **Request/Response logging** for audit trail  

---

## ?? Eligibility Check Workflow

### Complete Flow Diagram

```
????????????????    ????????????????    ????????????????
?   Provider   ????>?     API  ????>? Orchestration?
?   System     ?    ?  Controller  ?    ?   Service    ?
???????????????? ????????????????    ????????????????
            ?
 ?
          ???????????????????????????????????????
?  1. Create FHIR Bundle  ?
    ?     (Patient, Coverage, Request)    ?
        ???????????????????????????????????????
    ?
      ?
           ???????????????????????????????????????
             ?  2. Get OAuth2 Token    ?
        ?     (Client Credentials Flow)        ?
          ???????????????????????????????????????
    ?
         ?
        ???????????????????????????????????????
    ?  3. Submit to NPHIES     ?
   ?     POST /CoverageEligibilityRequest?
       ???????????????????????????????????????
                ?
        ?
       ???????????????????????????????????????
         ?  4. Poll for Response   ?
          ?     (Every 3-5 seconds)       ?
 ???????????????????????????????????????
     ?
       ?
         ???????????????????????????????????????
    ?  5. Parse Response Bundle           ?
            ?     (Benefits, Coverage, Errors)    ?
            ???????????????????????????????????????
         ?
 ?
           ???????????????????????????????????????
                ?  6. Save to Database         ?
          ?     (Request + Response)   ?
         ???????????????????????????????????????
```

### Step-by-Step Code Walkthrough

#### Step 1: Create FHIR Bundle

**File:** `Application/Services/FHIR/FhirBundleService.cs`

```csharp
public async Task<Bundle> CreateEligibilityRequestBundleAsync(
    DomainCoverageEligibilityRequest request,
    DomainPatient patient,
DomainCoverage coverage,
    DomainOrganization provider,
    DomainOrganization insurer)
{
    var bundle = new Bundle
    {
    Type = Bundle.BundleType.Transaction,
        Id = $"bundle-{Guid.NewGuid()}"
    };

    // 1. Add MessageHeader (Required by NPHIES)
    var messageHeader = CreateMessageHeader(provider);
    bundle.Entry.Add(new Bundle.EntryComponent
    {
    FullUrl = $"urn:uuid:{messageHeader.Id}",
        Resource = messageHeader,
        Request = new Bundle.RequestComponent
        {
   Method = Bundle.HTTPVerb.POST,
      Url = "MessageHeader"
        }
  });

    // 2. Add CoverageEligibilityRequest
    var eligibilityRequest = CreateEligibilityRequest(request, patient, coverage, provider, insurer);
    bundle.Entry.Add(new Bundle.EntryComponent
    {
FullUrl = $"urn:uuid:{eligibilityRequest.Id}",
        Resource = eligibilityRequest,
   Request = new Bundle.RequestComponent
        {
            Method = Bundle.HTTPVerb.POST,
  Url = "CoverageEligibilityRequest"
      }
    });

    // 3. Add Patient
    var fhirPatient = _mapper.MapToFhirPatient(patient);
    bundle.Entry.Add(new Bundle.EntryComponent
    {
 FullUrl = $"urn:uuid:{fhirPatient.Id}",
        Resource = fhirPatient,
        Request = new Bundle.RequestComponent
        {
            Method = Bundle.HTTPVerb.POST,
  Url = "Patient"
    }
    });

    // 4. Add Coverage
    var fhirCoverage = _mapper.MapToFhirCoverage(coverage);
 bundle.Entry.Add(new Bundle.EntryComponent
    {
        FullUrl = $"urn:uuid:{fhirCoverage.Id}",
        Resource = fhirCoverage,
        Request = new Bundle.RequestComponent
     {
            Method = Bundle.HTTPVerb.POST,
     Url = "Coverage"
        }
  });

    // 5. Add Provider Organization
  var providerOrg = _mapper.MapToFhirOrganization(provider);
    bundle.Entry.Add(new Bundle.EntryComponent
    {
        FullUrl = $"urn:uuid:{providerOrg.Id}",
        Resource = providerOrg,
        Request = new Bundle.RequestComponent
      {
      Method = Bundle.HTTPVerb.POST,
      Url = "Organization"
        }
    });

    return bundle;
}
```

#### Step 2: Submit Request

**File:** `Application/Services/Orchestration/NphiesOrchestrationService.cs`

```csharp
public async Task<DomainCoverageEligibilityResponse> SubmitEligibilityRequestAsync(
    DomainCoverageEligibilityRequest request,
    DomainPatient patient,
    DomainCoverage coverage,
    DomainOrganization provider,
    DomainOrganization insurer,
    CancellationToken cancellationToken = default)
{
try
    {
     _logger.LogInformation("Starting eligibility request submission for request {RequestId}", 
     request.RequestId);

    // Step 1: Create FHIR Bundle
        var bundle = await _bundleService.CreateEligibilityRequestBundleAsync(
    request, patient, coverage, provider, insurer);

        // Step 2: Serialize bundle to JSON
        var bundleJson = await _bundleService.SerializeBundleAsync(bundle);

      _logger.LogDebug("Eligibility bundle created: {BundleJson}", bundleJson);

        // Step 3: Submit to NPHIES (includes authentication)
        var response = await _httpClient.SubmitEligibilityRequestAsync(bundleJson, cancellationToken);

        if (!response.IsSuccess)
        {
            throw new NphiesApiException($"Eligibility submission failed: {response.ErrorMessage}");
        }

        _logger.LogInformation("Eligibility request submitted successfully. Polling for response...");

        // Step 4: Poll for response
 var responseBundle = await _pollingService.PollForEligibilityResponseAsync(
       request.RequestId, 
   cancellationToken);

        // Step 5: Parse response
      var eligibilityResponse = await _bundleService.ParseEligibilityResponseAsync(responseBundle);

        _logger.LogInformation("Eligibility response received and parsed successfully");

        return eligibilityResponse;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error submitting eligibility request");
        throw;
    }
}
```

#### Step 3: HTTP Client Submission

**File:** `Application/Services/Http/NphiesHttpClient.cs`

```csharp
public async Task<NphiesApiResponse<string>> SubmitEligibilityRequestAsync(
    string fhirBundle,
    CancellationToken cancellationToken = default)
{
 var stopwatch = Stopwatch.StartNew();

    try
    {
        _logger.LogInformation("Submitting eligibility request to NPHIES");

        // Get OAuth2 access token
        var accessToken = await _authService.GetAccessTokenAsync(cancellationToken);

  // Create HTTP request
        using var request = new HttpRequestMessage(HttpMethod.Post, 
      _config.Endpoints.Eligibility);
        
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));
        request.Content = new StringContent(fhirBundle, Encoding.UTF8, "application/fhir+json");

        // Send request (with Polly policies: retry, circuit breaker, timeout)
        var response = await _httpClient.SendAsync(request, cancellationToken);
    stopwatch.Stop();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
    _logger.LogInformation("Eligibility submitted successfully (Status: {Status}, Duration: {Duration}ms)",
       response.StatusCode, stopwatch.ElapsedMilliseconds);

   return new NphiesApiResponse<string>
            {
    IsSuccess = true,
    Data = responseContent,
    StatusCode = (int)response.StatusCode,
     Duration = stopwatch.Elapsed
            };
        }
      else
        {
            _logger.LogError("NPHIES API error (Status: {Status}, Response: {Response})",
        response.StatusCode, responseContent);

            return new NphiesApiResponse<string>
         {
      IsSuccess = false,
                StatusCode = (int)response.StatusCode,
       ErrorMessage = responseContent,
                Duration = stopwatch.Elapsed
            };
        }
    }
 catch (Exception ex)
    {
        stopwatch.Stop();
_logger.LogError(ex, "Exception submitting eligibility request");

        return new NphiesApiResponse<string>
        {
    IsSuccess = false,
          StatusCode = 500,
            ErrorMessage = ex.Message,
            Duration = stopwatch.Elapsed
        };
    }
}
```

#### Step 4: Polling for Response

**File:** `Application/Services/Polling/NphiesPollingService.cs`

```csharp
public async Task<Bundle> PollForEligibilityResponseAsync(
    string requestId,
    CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Starting polling for eligibility response {RequestId}", requestId);

    var pollingConfig = _config.Polling;
    var maxDuration = TimeSpan.FromMinutes(pollingConfig.MaxDurationMinutes);
    var startTime = DateTime.UtcNow;
    var attemptCount = 0;
    var currentInterval = TimeSpan.FromSeconds(pollingConfig.IntervalSeconds);

    while (DateTime.UtcNow - startTime < maxDuration)
  {
        attemptCount++;
        _logger.LogDebug("Polling attempt {Attempt} for request {RequestId}", 
            attemptCount, requestId);

   try
        {
        // Poll NPHIES for response
            var response = await _httpClient.PollEligibilityResponseAsync(requestId, cancellationToken);

      if (response.IsSuccess && !string.IsNullOrWhiteSpace(response.Data))
    {
          // Parse response bundle
   var bundle = _fhirParser.Parse<Bundle>(response.Data);

        // Check if response is complete
                if (IsResponseComplete(bundle))
    {
          _logger.LogInformation("Eligibility response received after {Attempts} attempts ({Duration}s)",
        attemptCount, (DateTime.UtcNow - startTime).TotalSeconds);
            
            return bundle;
        }
            }

            // Wait before next poll (exponential backoff if configured)
          if (pollingConfig.ExponentialBackoff)
            {
      currentInterval = CalculateExponentialBackoff(
    attemptCount, 
             pollingConfig.InitialBackoffSeconds,
   pollingConfig.MaxBackoffSeconds);
            }

         _logger.LogDebug("Waiting {Interval}s before next poll", currentInterval.TotalSeconds);
            await Task.Delay(currentInterval, cancellationToken);
        }
        catch (Exception ex)
        {
       _logger.LogWarning(ex, "Error during polling attempt {Attempt}", attemptCount);
   
            if (attemptCount >= pollingConfig.MaxRetries)
   {
        throw new NphiesPollingException(
    $"Max polling attempts ({pollingConfig.MaxRetries}) exceeded", ex);
   }
 }
    }

    throw new NphiesPollingTimeoutException(
        $"Polling timed out after {maxDuration.TotalMinutes} minutes");
}

private TimeSpan CalculateExponentialBackoff(int attempt, int initialSeconds, int maxSeconds)
{
    var backoffSeconds = Math.Min(initialSeconds * Math.Pow(2, attempt - 1), maxSeconds);
    return TimeSpan.FromSeconds(backoffSeconds);
}
```

---

## ?? Pre-Authorization Workflow

Pre-authorization follows a similar pattern to eligibility but with different FHIR resources.

### Key Differences:

| Aspect | Eligibility | Pre-Authorization |
|--------|------------|-------------------|
| **FHIR Resource** | CoverageEligibilityRequest | Claim (with type=preauthorization) |
| **Purpose** | Check coverage validity | Get approval for planned procedures |
| **Response** | CoverageEligibilityResponse | ClaimResponse (with preAuthRef) |
| **Items Required** | Basic coverage info | Detailed service items, diagnoses |

### Code Example:

```csharp
public async Task<DomainClaimResponse> SubmitPreAuthorizationAsync(
    DomainClaim claim,
    DomainPatient patient,
    DomainOrganization provider,
    DomainOrganization insurer,
    DomainCoverage coverage,
    CancellationToken cancellationToken = default)
{
    // Step 1: Set claim type to preauthorization
    claim.ClaimType = "preauthorization";

    // Step 2: Create FHIR Bundle
    var bundle = await _bundleService.CreateClaimRequestBundleAsync(
 claim, patient, provider, insurer, coverage, null);

    // Step 3: Submit to NPHIES
    var bundleJson = await _bundleService.SerializeBundleAsync(bundle);
    var response = await _httpClient.SubmitPriorAuthorizationAsync(bundleJson, cancellationToken);

    if (!response.IsSuccess)
    {
        throw new NphiesApiException($"Pre-auth submission failed: {response.ErrorMessage}");
    }

    // Step 4: Poll for response
    var responseBundle = await _pollingService.PollForClaimResponseAsync(
        claim.ClaimId, 
  cancellationToken);

    // Step 5: Parse response
    var claimResponse = await _bundleService.ParseClaimResponseAsync(responseBundle);

    return claimResponse;
}
```

### Pre-Authorization Bundle Structure:

```json
{
  "resourceType": "Bundle",
  "type": "transaction",
  "entry": [
    {
   "resource": {
        "resourceType": "Claim",
        "type": {
          "coding": [{
         "system": "http://terminology.hl7.org/CodeSystem/claim-type",
   "code": "professional"
        }]
        },
        "use": "preauthorization",  // Key difference!
        "patient": { "reference": "Patient/123" },
        "provider": { "reference": "Organization/456" },
 "insurer": { "reference": "Organization/789" },
   "item": [
   {
            "sequence": 1,
      "productOrService": {
       "coding": [{
       "system": "http://nphies.sa/terminology/CodeSystem/service",
     "code": "99213"
       }]
    },
            "unitPrice": {
         "value": 100.00,
        "currency": "SAR"
            }
     }
        ],
        "diagnosis": [
          {
    "sequence": 1,
            "diagnosisCodeableConcept": {
         "coding": [{
                "system": "http://hl7.org/fhir/sid/icd-10",
          "code": "E11.9"
              }]
            }
     }
        ]
}
    }
  ]
}
```

---

## ?? Claim Submission Workflow

### Claim Types Supported:

1. **Professional** - Doctor visits, consultations
2. **Institutional** - Hospital admissions
3. **Pharmacy** - Medication claims
4. **Oral** - Dental claims

### Complete Claim Submission Flow:

**File:** `Application/Services/Orchestration/NphiesOrchestrationService.cs`

```csharp
public async Task<DomainClaimResponse> SubmitClaimAsync(
    DomainClaim claim,
    DomainPatient patient,
    DomainOrganization provider,
    DomainOrganization insurer,
 DomainCoverage coverage,
    DomainEncounter? encounter,
    CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("Starting claim submission for claim {ClaimId}", claim.ClaimId);

        // Validation
        ValidateClaim(claim);

        // Step 1: Create FHIR Bundle
        var bundle = await _bundleService.CreateClaimRequestBundleAsync(
     claim, patient, provider, insurer, coverage, encounter);

        _logger.LogInformation("Claim bundle created with {ResourceCount} resources", 
     bundle.Entry.Count);

        // Step 2: Submit to NPHIES
        var bundleJson = await _bundleService.SerializeBundleAsync(bundle);
        var response = await _httpClient.SubmitClaimAsync(
            bundleJson, 
 claim.ClaimType, 
            cancellationToken);

        if (!response.IsSuccess)
        {
       _logger.LogError("Claim submission failed: {Error}", response.ErrorMessage);
            throw new NphiesApiException($"Claim submission failed: {response.ErrorMessage}");
        }

        _logger.LogInformation("Claim submitted successfully. Starting polling...");

        // Step 3: Poll for response
        var responseBundle = await _pollingService.PollForClaimResponseAsync(
      claim.ClaimId, 
     cancellationToken);

        // Step 4: Parse response
        var claimResponse = await _bundleService.ParseClaimResponseAsync(responseBundle);

        _logger.LogInformation("Claim response received. Status: {Status}, Outcome: {Outcome}",
            claimResponse.Status, claimResponse.Outcome);

  // Step 5: Save to database
        await SaveClaimResponse(claim, claimResponse);

     return claimResponse;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error submitting claim {ClaimId}", claim.ClaimId);
        throw;
    }
}

private void ValidateClaim(DomainClaim claim)
{
    if (string.IsNullOrWhiteSpace(claim.ClaimId))
        throw new ValidationException("ClaimId is required");

    if (string.IsNullOrWhiteSpace(claim.ClaimType))
      throw new ValidationException("ClaimType is required");

    if (!claim.Items.Any())
    throw new ValidationException("Claim must have at least one item");

 if (!claim.Diagnoses.Any())
        throw new ValidationException("Claim must have at least one diagnosis");
}
```

### Claim Item Structure:

```csharp
public class ClaimItem : BaseEntity
{
    public string ClaimId { get; set; }
    public int Sequence { get; set; }
    
    // Service/Product
    public string ServiceCode { get; set; }  // e.g., "99213"
 public string ServiceSystem { get; set; }  // e.g., "http://nphies.sa/terminology/CodeSystem/service"
    public string ServiceDisplay { get; set; }  // e.g., "Office Visit"
    
    // Quantity & Pricing
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal NetAmount { get; set; }  // Quantity * UnitPrice
    
    // Service Details
    public DateTime ServicedDate { get; set; }
    public string? BodySite { get; set; }  // For procedures
    public string? SubSite { get; set; }
    
    // Caregivers
    public string? CareTeamSequence { get; set; }
    public string? PractitionerId { get; set; }
    
    // Diagnosis Links
    public List<int> DiagnosisSequences { get; set; } = new();
    
    // Modifiers
    public List<string> Modifiers { get; set; } = new();
}
```

---

## ?? Polling Mechanism

### Polling Configuration:

```json
{
  "Nphies": {
    "Polling": {
    "IntervalSeconds": 5,           // Initial polling interval
      "MaxDurationMinutes": 10,  // Maximum time to poll
      "ExponentialBackoff": true,     // Enable exponential backoff
      "MaxRetries": 100,// Maximum polling attempts
      "InitialBackoffSeconds": 2,     // Starting backoff time
  "MaxBackoffSeconds": 60         // Maximum backoff time
    }
  }
}
```

### Exponential Backoff Example:

```
Attempt 1: Wait 2 seconds
Attempt 2: Wait 4 seconds
Attempt 3: Wait 8 seconds
Attempt 4: Wait 16 seconds
Attempt 5: Wait 32 seconds
Attempt 6+: Wait 60 seconds (max)
```

### Polling State Machine:

```
????????????????
?   PENDING    ? ? Initial state after submission
????????????????
   ?
       ? (Poll every N seconds)
????????????????
?   POLLING    ? ? Actively checking for response
????????????????
       ?
       ???? Response Ready ??? ????????????????
       ?         ?  COMPLETED   ?
       ?   ????????????????
       ?
       ???? Timeout ????????? ????????????????
       ?             ?TIMEOUT    ?
       ?    ????????????????
  ?
       ???? Max Retries ????? ????????????????
     ?    FAILED    ?
         ????????????????
```

---

## ? Error Handling

### Error Handling Strategy:

#### 1. NPHIES API Errors

**File:** `Application/Exceptions/NphiesApiException.cs`

```csharp
public class NphiesApiException : Exception
{
    public int StatusCode { get; set; }
 public string ErrorCode { get; set; }
    public string? ResponseBody { get; set; }
    
public NphiesApiException(string message, int statusCode = 500) 
     : base(message)
    {
        StatusCode = statusCode;
    }
}
```

#### 2. Validation Errors

```csharp
public class NphiesValidationException : Exception
{
    public List<ValidationError> Errors { get; set; } = new();
    
    public NphiesValidationException(string message, List<ValidationError> errors) 
  : base(message)
  {
     Errors = errors;
  }
}
```

#### 3. Polling Errors

```csharp
public class NphiesPollingTimeoutException : Exception
{
    public TimeSpan Duration { get; set; }
    
    public NphiesPollingTimeoutException(string message, TimeSpan duration) 
        : base(message)
 {
        Duration = duration;
    }
}
```

### Error Response Parsing:

```csharp
public async Task<OperationOutcome> ParseErrorResponseAsync(string responseJson)
{
    try
    {
        var operationOutcome = _fhirParser.Parse<OperationOutcome>(responseJson);
     
 foreach (var issue in operationOutcome.Issue)
        {
   _logger.LogError("NPHIES Error - Severity: {Severity}, Code: {Code}, Details: {Details}",
    issue.Severity, issue.Code, issue.Details?.Text);
        }
        
        return operationOutcome;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to parse error response");
      throw;
    }
}
```

### Common NPHIES Error Codes:

| Error Code | Description | Resolution |
|------------|-------------|------------|
| `INV-01` | Invalid member ID | Verify patient MRN |
| `INV-02` | Invalid policy number | Check coverage details |
| `EXP-01` | Policy expired | Request updated coverage |
| `DUP-01` | Duplicate submission | Check claim ID |
| `VAL-01` | Validation error | Review bundle structure |

---

## ?? Code Examples

### Example 1: Complete Eligibility Check

```csharp
// Controller endpoint
[HttpPost("eligibility/check")]
public async Task<IActionResult> CheckEligibility(
    [FromBody] EligibilityCheckRequest request,
    CancellationToken cancellationToken)
{
    try
    {
  // 1. Fetch entities from database
        var patient = await _patientRepository.GetByIdAsync(request.PatientId);
        var coverage = await _coverageRepository.GetByIdAsync(request.CoverageId);
    var provider = await _organizationRepository.GetByIdAsync(request.ProviderId);
        var insurer = await _organizationRepository.GetByIdAsync(request.InsurerId);

        // 2. Create eligibility request entity
        var eligibilityRequest = new CoverageEligibilityRequest
        {
   RequestId = Guid.NewGuid().ToString(),
      Status = "active",
            Created = DateTime.UtcNow,
       PatientId = patient.Id,
            CoverageId = coverage.Id,
            ProviderId = provider.Id,
  InsurerId = insurer.Id
        };

        // 3. Submit through orchestration service
   var response = await _orchestrationService.SubmitEligibilityRequestAsync(
            eligibilityRequest,
   patient,
            coverage,
            provider,
insurer,
     cancellationToken);

        // 4. Return result
        return Ok(new
 {
   requestId = eligibilityRequest.RequestId,
          status = response.Status,
            outcome = response.Outcome,
     inNetwork = response.InNetwork,
 benefits = response.BenefitBalances.Select(b => new
     {
          category = b.Category,
              allowed = b.Allowed,
         used = b.Used,
 remaining = b.Allowed - b.Used
        })
      });
    }
  catch (NphiesApiException ex)
    {
        _logger.LogError(ex, "NPHIES API error during eligibility check");
        return StatusCode(ex.StatusCode, new { error = ex.Message });
    }
    catch (Exception ex)
    {
  _logger.LogError(ex, "Unexpected error during eligibility check");
      return StatusCode(500, new { error = "Internal server error" });
    }
}
```

### Example 2: Submit Claim with Items

```csharp
[HttpPost("claims/submit")]
public async Task<IActionResult> SubmitClaim(
    [FromBody] ClaimSubmissionRequest request,
    CancellationToken cancellationToken)
{
    try
    {
      // 1. Create claim entity
    var claim = new Claim
        {
            ClaimId = $"CLM-{DateTime.UtcNow:yyyyMMddHHmmss}",
   ClaimType = "professional",
     Status = "active",
        Use = "claim",
         PatientId = request.PatientId,
            ProviderId = request.ProviderId,
       InsurerId = request.InsurerId,
            CoverageId = request.CoverageId,
      Created = DateTime.UtcNow,
            BillablePeriodStart = request.ServiceDate,
            BillablePeriodEnd = request.ServiceDate
        };

        // 2. Add claim items
        foreach (var item in request.Items)
        {
         claim.Items.Add(new ClaimItem
 {
       Sequence = item.Sequence,
 ServiceCode = item.ServiceCode,
            ServiceSystem = "http://nphies.sa/terminology/CodeSystem/service",
    Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
    NetAmount = item.Quantity * item.UnitPrice,
                ServicedDate = request.ServiceDate
            });
        }

        // 3. Add diagnoses
        foreach (var diagnosis in request.Diagnoses)
      {
     claim.Diagnoses.Add(new ClaimDiagnosis
  {
                Sequence = diagnosis.Sequence,
            DiagnosisCode = diagnosis.Code,
     DiagnosisSystem = "http://hl7.org/fhir/sid/icd-10",
          Type = diagnosis.Type
});
        }

      // 4. Fetch related entities
        var patient = await _patientRepository.GetByIdAsync(request.PatientId);
        var provider = await _organizationRepository.GetByIdAsync(request.ProviderId);
 var insurer = await _organizationRepository.GetByIdAsync(request.InsurerId);
        var coverage = await _coverageRepository.GetByIdAsync(request.CoverageId);

     // 5. Submit through orchestration
    var response = await _orchestrationService.SubmitClaimAsync(
  claim,
    patient,
      provider,
            insurer,
     coverage,
            null,
    cancellationToken);

     // 6. Return result
   return Ok(new
        {
          claimId = claim.ClaimId,
        status = response.Status,
  outcome = response.Outcome,
  totalApproved = response.Items.Sum(i => i.ApprovedAmount),
       totalBilled = claim.Items.Sum(i => i.NetAmount),
            preAuthRef = response.PreAuthRef
        });
    }
    catch (Exception ex)
    {
     _logger.LogError(ex, "Error submitting claim");
        return StatusCode(500, new { error = ex.Message });
    }
}
```

### Example 3: Check Claim Status (Polling)

```csharp
[HttpGet("claims/{claimId}/status")]
public async Task<IActionResult> GetClaimStatus(
  string claimId,
    CancellationToken cancellationToken)
{
    try
    {
        // Poll for claim response
        var response = await _pollingService.PollForClaimResponseAsync(
      claimId, 
          cancellationToken);

    // Parse response
        var claimResponse = await _bundleService.ParseClaimResponseAsync(response);

        return Ok(new
        {
   claimId = claimId,
            status = claimResponse.Status,
       outcome = claimResponse.Outcome,
            disposition = claimResponse.Disposition,
            items = claimResponse.Items.Select(i => new
          {
       sequence = i.ItemSequence,
   adjudication = i.AdjudicationCode,
       approvedAmount = i.ApprovedAmount,
    patientAmount = i.PatientAmount
   })
        });
    }
    catch (NphiesPollingTimeoutException ex)
    {
        return StatusCode(408, new 
        { 
    error = "Request timeout",
            message = ex.Message 
    });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error checking claim status");
     return StatusCode(500, new { error = ex.Message });
    }
}
```

---

## ?? Database Schema

### Key Entities:

```
???????????????????????
?      Patient        ?
???????????????????????
? Id (PK)   ?
? MRN     ?
? NationalId      ?
? FirstName     ?
? LastName            ?
? DateOfBirth      ?
? Gender        ?
???????????????????????
      ?
         ? 1:N
         ?
???????????????????????
?   Coverage        ?
???????????????????????
? Id (PK)     ?
? PatientId (FK)?
? PolicyNumber        ?
? InsurerId (FK)      ?
? CoverageStartDate   ?
? CoverageEndDate     ?
? Status     ?
???????????????????????
   ?
         ? 1:N
     ?
???????????????????????        ???????????????????????
? EligibilityRequest  ?        ?      Claim          ?
???????????????????????        ???????????????????????
? Id (PK)             ?        ? Id (PK)     ?
? RequestId      ?        ? ClaimId        ?
? PatientId (FK)  ?        ? PatientId (FK)      ?
? CoverageId (FK)     ?        ? CoverageId (FK)     ?
? Status    ?        ? ClaimType       ?
? Created       ?      ? Status       ?
???????????????????????        ? TotalAmount         ?
      ?      ???????????????????????
    ? 1:1    ?
         ?        ? 1:N
???????????????????????     ?
? EligibilityResponse ?        ???????????????????????
???????????????????????        ?    ClaimItem        ?
? Id (PK)             ?      ???????????????????????
? RequestId (FK)      ?? Id (PK)       ?
? Status       ?        ? ClaimId (FK)        ?
? Outcome     ?        ? Sequence      ?
? Disposition         ?    ? ServiceCode     ?
? InNetwork       ?     ? UnitPrice           ?
???????????????????????        ? Quantity ?
         ?        ? NetAmount           ?
         ? 1:N                  ???????????????????????
         ?
???????????????????????
?   BenefitBalance    ?
???????????????????????
? Id (PK)             ?
? ResponseId (FK)     ?
? Category  ?
? Allowed    ?
? Used     ?
???????????????????????
```

---

## ?? Testing

### Diagnostic Endpoints Created:

```bash
# Full diagnostic test
GET /api/NphiesDiagnostics/full

# Individual tests
GET /api/NphiesDiagnostics/transport-security
GET /api/NphiesDiagnostics/authentication
GET /api/NphiesDiagnostics/endpoints
GET /api/NphiesDiagnostics/health
```

### Running Tests:

```bash
# Start the application
dotnet run --project NPhies_FHIR_Integration.ApiService

# Test transport security
curl https://localhost:5001/api/NphiesDiagnostics/transport-security

# Test authentication
curl https://localhost:5001/api/NphiesDiagnostics/authentication

# Full diagnostic report
curl https://localhost:5001/api/NphiesDiagnostics/full
```

---

## ?? Summary

### Complete Flow:

```
1. AUTHENTICATION
   ?? Get OAuth2 token (client_credentials)
   ?? Cache token (50 minutes)
   ?? Add to all requests as Bearer token

2. ELIGIBILITY CHECK
   ?? Create FHIR Bundle (Patient, Coverage, Request)
   ?? Submit to NPHIES (/CoverageEligibilityRequest/$submit)
   ?? Poll for response (every 3-5 seconds)
   ?? Parse response (benefits, coverage status)

3. PRE-AUTHORIZATION
   ?? Create FHIR Bundle (Claim with type=preauthorization)
   ?? Include items, diagnoses
   ?? Submit to NPHIES (/Claim/$submit)
   ?? Poll for response
   ?? Get preAuthRef for claim submission

4. CLAIM SUBMISSION
   ?? Create FHIR Bundle (Claim with items, diagnoses)
   ?? Reference preAuthRef if required
   ?? Submit to NPHIES (/Claim/$submit)
   ?? Poll for response
   ?? Get adjudication results

5. ERROR HANDLING
   ?? Retry with exponential backoff
   ?? Circuit breaker for cascading failures
   ?? Detailed error logging
   ?? User-friendly error messages
```

### Key Files Reference:

| Component | File Location |
|-----------|---------------|
| **Orchestration** | `Application/Services/Orchestration/NphiesOrchestrationService.cs` |
| **HTTP Client** | `Application/Services/Http/NphiesHttpClient.cs` |
| **Authentication** | `Application/Services/Auth/NphiesAuthenticationService.cs` |
| **FHIR Bundles** | `Application/Services/FHIR/FhirBundleService.cs` |
| **Polling** | `Application/Services/Polling/NphiesPollingService.cs` |
| **Configuration** | `Application/Configuration/NphiesConfiguration.cs` |
| **Diagnostics** | `Application/Services/Diagnostics/NphiesDiagnosticService.cs` |

---

## ?? Next Steps

1. **Configure NPHIES Credentials:**
   - Update `appsettings.json` with real ClientId/ClientSecret
   - Set correct NPHIES base URL

2. **Run Diagnostics:**
   - Test transport security
- Verify authentication
   - Check endpoint connectivity

3. **Test Workflows:**
   - Submit test eligibility request
   - Create pre-authorization
   - Submit test claim

4. **Monitor:**
   - Check application logs
   - Review health check endpoints
   - Monitor performance metrics

---

**Documentation Generated:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")  
**Version:** 1.0  
**Author:** NPHIES Integration Team
