using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds synthetic claim workflows for local development only.
/// </summary>
public static class DevelopmentClaimSeeder
{
    private const string ClaimNumberPrefix = "DEV-CLAIM-";
    private const string CreatedBy = "DEVELOPMENT_SEED";

    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger)
    {
        if (await db.Claims.AnyAsync(c => c.ClaimNumber.StartsWith(ClaimNumberPrefix)))
        {
            await EnsureExistingDevelopmentClaimDetailsAsync(db);
            logger.LogInformation("Existing development claims were checked and enriched without duplication.");
            return;
        }

        var provider = await db.Organizations.FirstOrDefaultAsync(o => o.OrganizationType == "prov");
        if (provider is null)
        {
            provider = new Organization
            {
                OrganizationName = "Development Medical Center",
                LicenseNumber = "DEV-PROVIDER-001",
                LicenseSystem = "http://nphies.sa/license/provider-license",
                OrganizationType = "prov",
                SpecializationType = "Hospital",
                Email = "provider@example.test",
                PhoneNumber = "+966500000001",
                City = "Riyadh",
                State = "Riyadh",
                AddressCountry = "SA",
                Status = "active"
            };
            await db.Organizations.AddAsync(provider);

        }

        var insurer = await db.Organizations.FirstOrDefaultAsync(o => o.OrganizationType == "ins");
        if (insurer is null)
        {
            insurer = new Organization
            {
                OrganizationName = "Development Health Payer",
                LicenseNumber = "DEV-INSURER-001",
                LicenseSystem = "http://nphies.sa/license/payer-license",
                OrganizationType = "ins",
                SpecializationType = "Insurance",
                Email = "payer@example.test",
                PhoneNumber = "+966500000002",
                City = "Riyadh",
                State = "Riyadh",
                AddressCountry = "SA",
                NphiesPayerId = "DEV-PAYER-001",
                Status = "active"
            };
            await db.Organizations.AddAsync(insurer);
        }

        var practitioner = await db.Practitioners
            .FirstOrDefaultAsync(p => p.OrganizationId == provider.Id);
        if (practitioner is null)
        {
            practitioner = new Practitioner
            {
                FirstName = "Development",
                LastName = "Clinician",
                LicenseNumber = "DEV-PRACTITIONER-001",
                LicenseSystem = "http://nphies.sa/license/practitioner-license",
                Specialization = "General Practice",
                Qualification = "MD",
                Title = "Dr.",
                Email = "clinician@example.test",
                Phone = "+966500000003",
                OrganizationId = provider.Id,
                Status = "active",
                PractitionerRole = "doctor"
            };
            await db.Practitioners.AddAsync(practitioner);
        }

        var doctor = await db.DoctorMasters.FirstOrDefaultAsync(d => d.PractitionerId == practitioner.Id);
        if (doctor is null)
        {
            var clinicMaster = await db.ClinicMasters.FirstOrDefaultAsync(c => c.OrganizationId == provider.Id);
            if (clinicMaster is null)
            {
                clinicMaster = new ClinicMaster
                {
                    OrganizationId = provider.Id,
                    ClinicName = "Development Family Medicine Clinic",
                    ClinicCode = "DEV-CLINIC-001",
                    ClinicType = "Outpatient",
                    SpecializedServices = "General medicine and diagnostics",
                    IsEmergencyAvailable = true,
                    PharmacyAvailable = true,
                    LabAvailable = true,
                    ImagingAvailable = true,
                    AcceptsInsurance = true,
                    IsActive = true,
                    CreatedBy = CreatedBy
                };
                await db.ClinicMasters.AddAsync(clinicMaster);
            }

            doctor = new DoctorMaster
            {
                PractitionerId = practitioner.Id,
                DoctorCode = "DEV-DOC-001",
                DoctorName = practitioner.GetDisplayName(),
                Specialization = practitioner.Specialization,
                QualificationDegree = practitioner.Qualification,
                CurrencyCode = "SAR",
                ClinicMasterId = clinicMaster.Id,
                IsAvailableForAppointments = true,
                IsActive = true,
                CreatedBy = CreatedBy
            };
            await db.DoctorMasters.AddAsync(doctor);
        }

        var location = await db.Locations.FirstOrDefaultAsync(l => l.OrganizationId == provider.Id);
        if (location is null)
        {
            location = new Location
            {
                LocationName = "Development Outpatient Clinic",
                LocationLicense = "DEV-LOCATION-001",
                LicenseSystem = "http://nphies.sa/license/location-license",
                OrganizationId = provider.Id,
                FacilityType = "CLI",
                FacilityTypeDescription = "Outpatient clinic",
                AddressLine1 = "Development Street",
                City = "Riyadh",
                State = "Riyadh",
                PostalCode = "00000",
                Country = "SA",
                Phone = "+966500000004",
                Email = "clinic@example.test",
                Status = "active"
            };
            await db.Locations.AddAsync(location);
        }

        var now = DateTime.UtcNow;
        var samples = new[]
        {
            new { Suffix = "1001", FirstName = "Sample", LastName = "Patient One", Gender = "F", Type = "professional", SubType = "op", Priority = "normal", Total = 420m, ServiceCode = "99213", ServiceName = "Office Visit", DiagnosisCode = "J06.9", Technical = "Pending", Medical = "Pending" },
            new { Suffix = "1002", FirstName = "Sample", LastName = "Patient Two", Gender = "M", Type = "institutional", SubType = "ip", Priority = "urgent", Total = 6840m, ServiceCode = "85025", ServiceName = "Complete Blood Count", DiagnosisCode = "I10", Technical = "Approved", Medical = "Pending" },
            new { Suffix = "1003", FirstName = "Sample", LastName = "Patient Three", Gender = "F", Type = "pharmacy", SubType = "op", Priority = "normal", Total = 168.50m, ServiceCode = "06285051", ServiceName = "Metformin 500mg Tablet", DiagnosisCode = "E11.9", Technical = "Pending", Medical = "Pending" },
            new { Suffix = "1004", FirstName = "Sample", LastName = "Patient Four", Gender = "M", Type = "professional", SubType = "op", Priority = "normal", Total = 275m, ServiceCode = "99213", ServiceName = "Office Visit", DiagnosisCode = "M54.5", Technical = "Approved", Medical = "Approved" }
        };

        var claims = new List<Claim>();
        foreach (var sample in samples)
        {
            var patient = new Patient
            {
                MRN = $"DEV-MRN-{sample.Suffix}",
                IdentifierSystem = "http://nphies.sa/identifier/member-id",
                NationalId = $"DEV-ID-{sample.Suffix}",
                NationalIdSystem = "urn:example:development-id",
                NationalIdType = "NI",
                FirstName = sample.FirstName,
                LastName = sample.LastName,
                FullName = $"{sample.FirstName} {sample.LastName}",
                DateOfBirth = new DateTime(1990, 1, 1).AddDays(int.Parse(sample.Suffix)),
                Gender = sample.Gender,
                Email = $"patient.{sample.Suffix}@example.test",
                Phone = "+966500000100",
                AddressLine1 = "Development Street",
                City = "Riyadh",
                State = "Riyadh",
                PostalCode = "00000",
                Country = "SA",
                Status = "active",
                Active = true
            };

            var coverage = new Coverage
            {
                PolicyNumber = $"DEV-POLICY-{sample.Suffix}",
                MemberID = $"DEV-MEMBER-{sample.Suffix}",
                CoverageIdentifierSystem = "urn:example:development-policy",
                CoverageIdentifierValue = $"DEV-MEMBER-{sample.Suffix}",
                CoverageType = "EHCPOL",
                CoverageTypeSystem = "http://nphies.sa/terminology/CodeSystem/coverage-type",
                Status = "active",
                SubscriberRelationship = "self",
                SubscriberId = $"DEV-MEMBER-{sample.Suffix}",
                SubscriberPatientId = patient.Id,
                RelationToSubscriber = "self",
                SubscriberMRN = patient.MRN,
                PatientId = patient.Id,
                Patient = patient,
                InsurerId = insurer.Id,
                Insurer = insurer,
                CoverageStartDate = now.AddYears(-1),
                CoverageEndDate = now.AddYears(1),
                AnnualDeductible = 1000m,
                DeductibleMet = 100m,
                Copay = 20m,
                CoinsurancePercent = 20m,
                OutOfPocketMax = 10000m
            };

            var encounter = new Encounter
            {
                EncounterId = $"DEV-ENC-{sample.Suffix}",
                IdentifierSystem = "urn:example:development-encounter",
                IdentifierValue = $"DEV-ENC-{sample.Suffix}",
                Status = "finished",
                Class = sample.SubType == "ip" ? "IMP" : "AMB",
                ServiceType = sample.ServiceCode,
                PatientId = patient.Id,
                Patient = patient,
                PeriodStart = now.AddDays(-int.Parse(sample.Suffix[^1..])),
                PeriodEnd = now.AddDays(-int.Parse(sample.Suffix[^1..])).AddHours(1),
                AdmitSource = sample.SubType == "ip" ? "emergency" : "walk-in",
                AdmitSourceSystem = "http://terminology.hl7.org/CodeSystem/admit-source",
                ServiceProviderId = provider.Id,
                ServiceProvider = provider
            };

            var claimId = Guid.NewGuid().ToString();
            var messageHeader = new MessageHeader
            {
                MessageUUID = Guid.NewGuid().ToString(),
                CorrelationId = Guid.NewGuid().ToString(),
                EventCode = "claim",
                SenderOrganizationId = provider.Id,
                SenderOrganization = provider,
                DestinationName = insurer.OrganizationName,
                DestinationEndpoint = "https://nphies.example.test/fhir",
                DestinationOrganizationId = insurer.Id,
                FocusResourceType = "Claim",
                FocusResourceId = claimId,
                SourceName = provider.OrganizationName,
                SourceEndpoint = "https://provider.example.test/rcm",
                MessageTimestamp = now,
                Status = "received",
                ResponseStatus = "ok"
            };

            var claim = new Claim
            {
                Id = claimId,
                ClaimNumber = $"DEV-CLAIM-{sample.Suffix}",
                ClaimIdentifierSystem = "urn:example:development-claim",
                ClaimIdentifierValue = $"DEV-CLAIM-{sample.Suffix}",
                Status = "active",
                ClaimType = sample.Type,
                ClaimTypeSystem = "http://terminology.hl7.org/CodeSystem/claim-type",
                ClaimSubType = sample.SubType,
                Use = "claim",
                Priority = sample.Priority,
                PayeeType = "provider",
                Total = sample.Total,
                TotalCurrency = "SAR",
                PatientId = patient.Id,
                Patient = patient,
                CoverageId = coverage.Id,
                Coverage = coverage,
                ProviderId = provider.Id,
                Provider = provider,
                InsurerId = insurer.Id,
                Insurer = insurer,
                PractitionerId = practitioner.Id,
                Practitioner = practitioner,
                LocationId = location.Id,
                ServiceLocation = location,
                EncounterId = encounter.Id,
                Encounter = encounter,
                MessageHeaderId = messageHeader.Id,
                MessageHeader = messageHeader,
                CreatedDate = now.AddDays(-int.Parse(sample.Suffix[^1..])),
                ServicedPeriodStart = now.AddDays(-int.Parse(sample.Suffix[^1..])),
                ServicedPeriodEnd = now.AddDays(-int.Parse(sample.Suffix[^1..])),
                TechnicalReviewStatus = sample.Technical,
                TechnicalReviewerId = sample.Technical == "Approved" ? "DEV-TECH-REVIEWER" : null,
                TechnicalReviewedAt = sample.Technical == "Approved" ? now.AddHours(-2) : null,
                TechnicalReviewNotes = sample.Technical == "Approved" ? "Synthetic coding review passed." : null,
                MedicalReviewStatus = sample.Medical,
                MedicalReviewerId = sample.Medical == "Approved" ? "DEV-MED-REVIEWER" : null,
                MedicalReviewedAt = sample.Medical == "Approved" ? now.AddHours(-1) : null,
                MedicalReviewNotes = sample.Medical == "Approved" ? "Synthetic medical review passed." : null
            };

            var services = sample.Suffix == "1004"
                ? new[]
                {
                    (Code: "99213", Name: "Office Visit - Established Patient", Amount: 100m),
                    (Code: "80050", Name: "General Health Panel", Amount: 75m),
                    (Code: "85025", Name: "Complete Blood Count", Amount: 60m),
                    (Code: "71046", Name: "Chest X-Ray, 2 Views", Amount: 40m)
                }
                : new[] { (Code: sample.ServiceCode, Name: sample.ServiceName, Amount: sample.Total) };

            for (var index = 0; index < services.Length; index++)
            {
                var service = services[index];
                var sequence = index + 1;
                var item = new ClaimItem
                {
                    ClaimId = claim.Id,
                    Sequence = sequence,
                    ProductOrServiceCode = service.Code,
                    ProductOrServiceSystem = "http://nphies.sa/terminology/CodeSystem/services",
                    ProductOrServiceDisplay = service.Name,
                    ServicedDate = encounter.PeriodStart,
                    Quantity = 1m,
                    UnitPrice = service.Amount,
                    Net = service.Amount,
                    PatientShare = service.Amount * 0.2m,
                    PatientShareCurrency = "SAR",
                    LocationId = location.Id,
                    CareTeamSequence = 1,
                    DiagnosisSequence = "1",
                    PatientInvoiceSystem = "urn:example:development-invoice",
                    PatientInvoiceValue = $"DEV-INV-{sample.Suffix}-{sequence:00}"
                };
                item.Details.Add(new ClaimItemDetail
                {
                    ClaimItemId = item.Id,
                    Sequence = 1,
                    ProductOrServiceCode = service.Code,
                    ProductOrServiceSystem = "http://nphies.sa/terminology/CodeSystem/services",
                    ProductOrServiceDisplay = service.Name,
                    Quantity = 1m,
                    UnitPrice = service.Amount,
                    Net = service.Amount
                });
                claim.Items.Add(item);
            }
            claim.Diagnoses.Add(new ClaimDiagnosis
            {
                ClaimId = claim.Id,
                Sequence = 1,
                DiagnosisCode = sample.DiagnosisCode,
                DiagnosisSystem = "http://hl7.org/fhir/sid/icd-10",
                DiagnosisDisplay = "Synthetic sample diagnosis",
                DiagnosisType = "principal",
                OnAdmissionCode = "y"
            });
            claim.CareTeam.Add(new ClaimCareTeam
            {
                ClaimId = claim.Id,
                Sequence = 1,
                PractitionerId = practitioner.Id,
                Practitioner = practitioner,
                Role = "primary",
                RoleDisplay = "Primary practitioner",
                QualificationDisplay = practitioner.Specialization
            });
            var clinicalValues = new (string Category, string Display, string Value)[]
            {
                ("emergency-arrival-code", "Emergency arrival code", "other"),
                ("triage-category", "Triage category", "non-urgent"),
                ("discharge-disposition", "Discharge disposition", "home-other"),
                ("emergency-department-disposition", "Emergency department disposition", "discharged-to-home"),
                ("triage-date", "Triage date", encounter.PeriodStart.ToString("yyyy-MM-dd")),
                ("discharge-specialty", "Discharge specialty", practitioner.Specialization),
                ("investigation-result", "Investigation result", "Synthetic results within expected range."),
                ("treatment-plan", "Treatment plan", "Synthetic outpatient follow-up plan."),
                ("patient-history", "Patient history", "Synthetic development history; no real patient data."),
                ("physical-examination", "Physical examination", "Synthetic normal examination findings."),
                ("history-of-present-illness", "History of present illness", "Synthetic sample presenting complaint."),
                ("vital-blood-pressure", "Blood pressure", "120/80"),
                ("vital-lmp", "Last menstrual period", "2026-09-10")
            };
            for (var index = 0; index < clinicalValues.Length; index++)
            {
                var clinicalValue = clinicalValues[index];
                claim.SupportingInfo.Add(new ClaimSupportingInfo
                {
                    ClaimId = claim.Id,
                    Sequence = index + 1,
                    Category = clinicalValue.Category,
                    CategorySystem = "urn:example:development-claim-info",
                    CategoryDisplay = clinicalValue.Display,
                    StringValue = clinicalValue.Value
                });
            }

            var vitalSigns = new (string Category, string Name, decimal Value, string Unit)[]
            {
                ("vital-height", "Height", 168m, "cm"),
                ("vital-weight", "Weight", 68m, "kg"),
                ("vital-pulse", "Pulse", 80m, "/min"),
                ("vital-temperature", "Temperature", 36.5m, "Cel"),
                ("vital-oxygen-saturation", "Oxygen saturation", 98m, "%"),
                ("vital-respiratory-rate", "Respiratory rate", 16m, "/min")
            };
            for (var index = 0; index < vitalSigns.Length; index++)
            {
                var vital = vitalSigns[index];
                claim.SupportingInfo.Add(new ClaimSupportingInfo
                {
                    ClaimId = claim.Id,
                    Sequence = clinicalValues.Length + index + 1,
                    Category = vital.Category,
                    CategorySystem = "urn:example:development-vitals",
                    CategoryDisplay = vital.Name,
                    QuantityValue = vital.Value,
                    QuantityUnit = vital.Unit,
                    QuantitySystem = "http://unitsofmeasure.org"
                });
            }
            if (sample.Suffix == "1004")
            {
                var claimResponse = new ClaimResponse
                {
                    ClaimId = claim.Id,
                    Claim = claim,
                    ResponseIdentifierSystem = "urn:example:development-approval",
                    ResponseIdentifierValue = "DEV-APPROVAL-1004",
                    ClaimResponseStatus = "active",
                    ClaimType = claim.ClaimType,
                    Use = claim.Use,
                    PatientId = patient.Id,
                    Patient = patient,
                    InsurerId = insurer.Id,
                    Insurer = insurer,
                    RequestorId = provider.Id,
                    Requestor = provider,
                    PreAuthRef = "DEV-PREAUTH-1004",
                    ServiceProviderId = provider.Id,
                    ServiceProvider = provider
                };

                claimResponse.Insurance.Add(new ClaimResponseInsurance
                {
                    ClaimResponseId = claimResponse.Id,
                    ClaimResponse = claimResponse,
                    Sequence = 1,
                    Focal = true,
                    CoverageId = coverage.Id,
                    Coverage = coverage,
                    PreAuthReferences = "DEV-PREAUTH-1004"
                });

                foreach (var item in claim.Items)
                {
                    var approvedAmount = (item.Net ?? 0m) * 0.9m;
                    var responseItem = new ClaimResponseAddItem
                    {
                        ClaimResponseId = claimResponse.Id,
                        ClaimResponse = claimResponse,
                        Sequence = item.Sequence,
                        ProductOrServiceCode = item.ProductOrServiceCode,
                        ProductOrServiceSystem = item.ProductOrServiceSystem,
                        ProductOrServiceDisplay = item.ProductOrServiceDisplay,
                        IsApproved = true,
                        ApprovedQuantity = 1,
                        BenefitAmount = approvedAmount,
                        BenefitCurrency = "SAR",
                        SubmittedAmount = item.Net,
                        DiagnosisSequence = 1,
                        Notes = "Synthetic payer approval amount for development only."
                    };
                    responseItem.Adjudications.Add(new ClaimResponseAdjudication
                    {
                        ClaimResponseAddItemId = responseItem.Id,
                        ClaimResponseAddItem = responseItem,
                        AdjudicationCategory = "benefit",
                        AdjudicationSystem = "http://terminology.hl7.org/CodeSystem/adjudication",
                        AdjudicationDisplay = "Payer approved benefit",
                        Amount = approvedAmount,
                        Currency = "SAR"
                    });
                    claimResponse.AddItems.Add(responseItem);
                }

                claimResponse.Totals.Add(new ClaimResponseTotal
                {
                    ClaimResponseId = claimResponse.Id,
                    ClaimResponse = claimResponse,
                    Category = "submitted",
                    CategorySystem = "http://terminology.hl7.org/CodeSystem/adjudication",
                    Amount = claim.Total,
                    Currency = "SAR",
                    Sequence = 1
                });
                claimResponse.Totals.Add(new ClaimResponseTotal
                {
                    ClaimResponseId = claimResponse.Id,
                    ClaimResponse = claimResponse,
                    Category = "benefit",
                    CategorySystem = "http://terminology.hl7.org/CodeSystem/adjudication",
                    Amount = claim.Total * 0.9m,
                    Currency = "SAR",
                    Sequence = 2
                });
                claim.ClaimResponses.Add(claimResponse);
            }
            if (sample.Suffix == "1002")
            {
                claim.RelatedClaims.Add(new ClaimRelated
                {
                    ClaimId = claim.Id,
                    RelatedClaimIdentifierSystem = "urn:example:development-authorization",
                    RelatedClaimIdentifierValue = "DEV-AUTH-1002",
                    Relationship = "prior",
                    RelationshipDisplay = "Prior authorization",
                    Notes = "Synthetic development reference."
                });
            }

            claims.Add(claim);
        }

        await db.Claims.AddRangeAsync(claims);
        await db.SaveChangesAsync();
        logger.LogInformation(
            "Seeded {Count} synthetic development claims with patient, coverage, encounter, message, and claim-detail records.",
            claims.Count);
    }

    private static async Task EnsureExistingDevelopmentClaimDetailsAsync(ApplicationDbContext db)
    {
        var claims = await db.Claims
            .Where(claim => claim.ClaimNumber.StartsWith(ClaimNumberPrefix))
            .Include(claim => claim.Patient)
            .Include(claim => claim.Provider)
            .Include(claim => claim.Insurer)
            .Include(claim => claim.Coverage)
            .Include(claim => claim.Encounter)
            .Include(claim => claim.Practitioner)
            .Include(claim => claim.Items)
                .ThenInclude(item => item.Details)
            .Include(claim => claim.SupportingInfo)
            .Include(claim => claim.ClaimResponses)
                .ThenInclude(response => response.AddItems)
                    .ThenInclude(item => item.Adjudications)
            .ToListAsync();

        foreach (var claim in claims)
        {
            if (claim.Patient is not null && string.IsNullOrWhiteSpace(claim.Patient.NationalIdType))
                claim.Patient.NationalIdType = "NI";

            if (claim.Encounter is not null && string.IsNullOrWhiteSpace(claim.Encounter.AdmitSource))
                claim.Encounter.AdmitSource = claim.ClaimSubType == "ip" ? "emergency" : "walk-in";

            var supportValues = new (string Category, string Display, string Value)[]
            {
                ("emergency-arrival-code", "Emergency arrival code", "other"),
                ("triage-category", "Triage category", "non-urgent"),
                ("discharge-disposition", "Discharge disposition", "home-other"),
                ("emergency-department-disposition", "Emergency department disposition", "discharged-to-home"),
                ("triage-date", "Triage date", (claim.Encounter?.PeriodStart ?? claim.CreatedAt).ToString("yyyy-MM-dd")),
                ("discharge-specialty", "Discharge specialty", claim.Practitioner?.Specialization ?? "General Medicine"),
                ("chief-complaint", "Chief complaint", "Synthetic sample presenting complaint."),
                ("significant-signs", "Significant signs", "Synthetic development sample."),
                ("investigation-result", "Investigation result", "Synthetic results within expected range."),
                ("treatment-plan", "Treatment plan", "Synthetic outpatient follow-up plan."),
                ("patient-history", "Patient history", "Synthetic development history; no real patient data."),
                ("physical-examination", "Physical examination", "Synthetic normal examination findings."),
                ("history-of-present-illness", "History of present illness", "Synthetic sample presenting complaint."),
                ("vital-blood-pressure", "Blood pressure", "120/80"),
                ("vital-lmp", "Last menstrual period", "2026-09-10")
            };

            foreach (var value in supportValues)
            {
                if (claim.SupportingInfo.Any(info => info.Category == value.Category)) continue;
                claim.SupportingInfo.Add(new ClaimSupportingInfo
                {
                    ClaimId = claim.Id,
                    Sequence = claim.SupportingInfo.Count + 1,
                    Category = value.Category,
                    CategorySystem = "urn:example:development-claim-info",
                    CategoryDisplay = value.Display,
                    StringValue = value.Value
                });
            }

            var vitals = new (string Category, string Name, decimal Value, string Unit)[]
            {
                ("vital-height", "Height", 168m, "cm"),
                ("vital-weight", "Weight", 68m, "kg"),
                ("vital-pulse", "Pulse", 80m, "/min"),
                ("vital-temperature", "Temperature", 36.5m, "Cel"),
                ("vital-oxygen-saturation", "Oxygen saturation", 98m, "%"),
                ("vital-respiratory-rate", "Respiratory rate", 16m, "/min")
            };

            foreach (var vital in vitals)
            {
                if (claim.SupportingInfo.Any(info => info.Category == vital.Category)) continue;
                claim.SupportingInfo.Add(new ClaimSupportingInfo
                {
                    ClaimId = claim.Id,
                    Sequence = claim.SupportingInfo.Count + 1,
                    Category = vital.Category,
                    CategorySystem = "urn:example:development-vitals",
                    CategoryDisplay = vital.Name,
                    QuantityValue = vital.Value,
                    QuantityUnit = vital.Unit,
                    QuantitySystem = "http://unitsofmeasure.org"
                });
            }

            if (!claim.ClaimNumber.EndsWith("1004", StringComparison.Ordinal)) continue;

            var services = new[]
            {
                (Code: "99213", Name: "Office Visit - Established Patient", Amount: 100m),
                (Code: "80050", Name: "General Health Panel", Amount: 75m),
                (Code: "85025", Name: "Complete Blood Count", Amount: 60m),
                (Code: "71046", Name: "Chest X-Ray, 2 Views", Amount: 40m)
            };

            foreach (var service in services)
            {
                var sequence = Array.IndexOf(services, service) + 1;
                var item = claim.Items.FirstOrDefault(existing => existing.Sequence == sequence);
                if (item is null)
                {
                    item = new ClaimItem { ClaimId = claim.Id, Sequence = sequence };
                    claim.Items.Add(item);
                }

                item.ProductOrServiceCode = service.Code;
                item.ProductOrServiceSystem = "http://nphies.sa/terminology/CodeSystem/services";
                item.ProductOrServiceDisplay = service.Name;
                item.ServicedDate ??= claim.Encounter?.PeriodStart;
                item.Quantity = 1m;
                item.UnitPrice = service.Amount;
                item.Net = service.Amount;
                item.PatientShare = service.Amount * 0.2m;
                item.PatientShareCurrency = "SAR";
                item.DiagnosisSequence = "1";
                item.CareTeamSequence = 1;
                item.PatientInvoiceSystem = "urn:example:development-invoice";
                item.PatientInvoiceValue = $"DEV-INV-1004-{sequence:00}";

                var detail = item.Details.FirstOrDefault() ?? new ClaimItemDetail
                {
                    ClaimItemId = item.Id,
                    Sequence = 1
                };
                detail.ProductOrServiceCode = service.Code;
                detail.ProductOrServiceSystem = item.ProductOrServiceSystem;
                detail.ProductOrServiceDisplay = service.Name;
                detail.Quantity = 1m;
                detail.UnitPrice = service.Amount;
                detail.Net = service.Amount;
                if (!item.Details.Contains(detail)) item.Details.Add(detail);
            }

            var response = claim.ClaimResponses.OrderByDescending(item => item.CreatedAt).FirstOrDefault();
            if (response is null)
            {
                response = new ClaimResponse
                {
                    ClaimId = claim.Id,
                    Claim = claim,
                    ResponseIdentifierSystem = "urn:example:development-approval",
                    ResponseIdentifierValue = "DEV-APPROVAL-1004",
                    ClaimResponseStatus = "active",
                    ClaimType = claim.ClaimType,
                    Use = claim.Use,
                    PatientId = claim.PatientId,
                    Patient = claim.Patient,
                    InsurerId = claim.InsurerId,
                    Insurer = claim.Insurer,
                    RequestorId = claim.ProviderId,
                    Requestor = claim.Provider,
                    PreAuthRef = "DEV-PREAUTH-1004",
                    ServiceProviderId = claim.ProviderId,
                    ServiceProvider = claim.Provider
                };
                claim.ClaimResponses.Add(response);
            }

            if (!response.Insurance.Any())
            {
                response.Insurance.Add(new ClaimResponseInsurance
                {
                    ClaimResponseId = response.Id,
                    ClaimResponse = response,
                    Sequence = 1,
                    Focal = true,
                    CoverageId = claim.CoverageId,
                    Coverage = claim.Coverage,
                    PreAuthReferences = response.PreAuthRef
                });
            }

            foreach (var item in claim.Items)
            {
                var responseItem = response.AddItems.FirstOrDefault(existing => existing.Sequence == item.Sequence);
                var approvedAmount = (item.Net ?? 0m) * 0.9m;
                if (responseItem is null)
                {
                    responseItem = new ClaimResponseAddItem
                    {
                        ClaimResponseId = response.Id,
                        ClaimResponse = response,
                        Sequence = item.Sequence
                    };
                    response.AddItems.Add(responseItem);
                }

                responseItem.ProductOrServiceCode = item.ProductOrServiceCode;
                responseItem.ProductOrServiceSystem = item.ProductOrServiceSystem;
                responseItem.ProductOrServiceDisplay = item.ProductOrServiceDisplay;
                responseItem.IsApproved = true;
                responseItem.ApprovedQuantity = 1;
                responseItem.BenefitAmount = approvedAmount;
                responseItem.BenefitCurrency = "SAR";
                responseItem.SubmittedAmount = item.Net;
                responseItem.DiagnosisSequence = 1;

                if (!responseItem.Adjudications.Any(adjudication => adjudication.AdjudicationCategory == "benefit"))
                {
                    responseItem.Adjudications.Add(new ClaimResponseAdjudication
                    {
                        ClaimResponseAddItemId = responseItem.Id,
                        ClaimResponseAddItem = responseItem,
                        AdjudicationCategory = "benefit",
                        AdjudicationSystem = "http://terminology.hl7.org/CodeSystem/adjudication",
                        AdjudicationDisplay = "Payer approved benefit",
                        Amount = approvedAmount,
                        Currency = "SAR"
                    });
                }
            }
        }

        await db.SaveChangesAsync();
    }
}