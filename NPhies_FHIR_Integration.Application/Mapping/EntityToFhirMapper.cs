using Hl7.Fhir.Model;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using Microsoft.Extensions.Logging;
using DomainPatient = NPhies_FHIR_Integration.Domain.Entities.Patient;
using DomainOrganization = NPhies_FHIR_Integration.Domain.Entities.Organization;
using DomainCoverage = NPhies_FHIR_Integration.Domain.Entities.Coverage;
using DomainClaim = NPhies_FHIR_Integration.Domain.Entities.Claim;
using DomainPractitioner = NPhies_FHIR_Integration.Domain.Entities.Practitioner;
using DomainEncounter = NPhies_FHIR_Integration.Domain.Entities.Encounter;
using DomainLocation = NPhies_FHIR_Integration.Domain.Entities.Location;
using DomainCoverageEligibilityRequest = NPhies_FHIR_Integration.Domain.Entities.CoverageEligibilityRequest;
using FhirPatient = Hl7.Fhir.Model.Patient;
using FhirOrganization = Hl7.Fhir.Model.Organization;
using FhirCoverage = Hl7.Fhir.Model.Coverage;
using FhirClaim = Hl7.Fhir.Model.Claim;
using FhirPractitioner = Hl7.Fhir.Model.Practitioner;
using FhirEncounter = Hl7.Fhir.Model.Encounter;
using FhirLocation = Hl7.Fhir.Model.Location;

namespace NPhies_FHIR_Integration.Application.Mapping;

/// <summary>
/// Maps domain entities to FHIR R4 resources for NPHIES integration
/// </summary>
public interface IEntityToFhirMapper
{
    // Basic Resources
    FhirPatient MapToFhirPatient(DomainPatient patient);
    FhirCoverage MapToFhirCoverage(DomainCoverage coverage);
    FhirOrganization MapToFhirOrganization(DomainOrganization organization);
    FhirPractitioner MapToFhirPractitioner(DomainPractitioner practitioner);
    FhirEncounter MapToFhirEncounter(DomainEncounter encounter);
    FhirLocation MapToFhirLocation(DomainLocation location);
    MessageHeader CreateMessageHeader(string eventCode, DomainOrganization source);

    // Request Resources
    CoverageEligibilityRequest MapToFhirEligibilityRequest(
        DomainCoverageEligibilityRequest request,
        string patientRef,
        string coverageRef,
        string providerRef,
        string insurerRef);

    FhirClaim MapToFhirClaim(
    DomainClaim claim,
        string patientRef,
        string providerRef,
        string insurerRef,
        string coverageRef,
        string? encounterRef = null);

    // Extensions & Identifiers
    Extension CreateNphiesExtension(string url, DataType value);
    Identifier CreateNphiesIdentifier(string system, string value);
}

/// <summary>
/// Implementation of Entity to FHIR mapper
/// </summary>
public class EntityToFhirMapper : IEntityToFhirMapper
{
    private readonly ILogger<EntityToFhirMapper> _logger;

    public EntityToFhirMapper(ILogger<EntityToFhirMapper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Map Patient entity to FHIR Patient resource
    /// </summary>
    public FhirPatient MapToFhirPatient(DomainPatient patient)
    {
        _logger.LogDebug("Mapping patient {PatientId} to FHIR resource", patient.Id);

        var fhirPatient = new FhirPatient
        {
            Id = patient.Id,
            Identifier = new List<Identifier>
            {
         new Identifier
      {
             System = "urn:oid:2.16.840.1.113883.3.571.1.1", // National ID
   Value = patient.NationalId
      },
       new Identifier
       {
               System = "http://nphies.sa/identifier/member-id", // Member ID
      Value = patient.MRN
        }
       },
            Name = new List<HumanName>
     {
                new HumanName
  {
        Family = patient.LastName,
        Given = new[] { patient.FirstName },
         Text = patient.FullName
    }
        },
            BirthDate = patient.DateOfBirth.ToString("yyyy-MM-dd"),
            Gender = patient.Gender?.ToUpper() switch
            {
                "M" => AdministrativeGender.Male,
                "F" => AdministrativeGender.Female,
                "O" => AdministrativeGender.Other,
                _ => AdministrativeGender.Unknown
            },
            Active = patient.IsActive,
            Meta = new Meta
            {
                Profile = new[] { NphiesProfiles.Patient }
            }
        };

        // Add telecom if available
        var telecom = new List<ContactPoint>();
        if (!string.IsNullOrWhiteSpace(patient.Phone))
        {
            telecom.Add(new ContactPoint
            {
                System = ContactPoint.ContactPointSystem.Phone,
                Value = patient.Phone,
                Use = ContactPoint.ContactPointUse.Mobile
            });
        }
        if (!string.IsNullOrWhiteSpace(patient.Email))
        {
            telecom.Add(new ContactPoint
            {
                System = ContactPoint.ContactPointSystem.Email,
                Value = patient.Email
            });
        }
        if (telecom.Any())
            fhirPatient.Telecom = telecom;

        // Add address if available
        if (!string.IsNullOrWhiteSpace(patient.City))
        {
            fhirPatient.Address = new List<Address>
   {
       new Address
                {
             Line = new[] { patient.AddressLine1, patient.AddressLine2 }
      .Where(l => !string.IsNullOrWhiteSpace(l))
   .ToArray(),
    City = patient.City,
           State = patient.State,
          PostalCode = patient.PostalCode,
          Country = patient.Country ?? "SA"
       }
            };
        }

        return fhirPatient;
    }

    /// <summary>
    /// Map Coverage entity to FHIR Coverage resource
    /// </summary>
    public FhirCoverage MapToFhirCoverage(DomainCoverage coverage)
    {
        _logger.LogDebug("Mapping coverage {CoverageId} to FHIR resource", coverage.Id);

        var fhirCoverage = new FhirCoverage
        {
Id = coverage.Id,
            Identifier = new List<Identifier>
            {
    new Identifier
     {
          System = "http://nphies.sa/identifier/policy-number",
      Value = coverage.PolicyNumber
       },
        new Identifier
        {
     System = "http://nphies.sa/license/payer-member-id",
    Value = coverage.MemberID
       }
         },
     Status = FinancialResourceStatusCodes.Active,
    Type = new CodeableConcept
     {
          Coding = new List<Coding>
      {
    new Coding
               {
   System = "http://terminology.hl7.org/CodeSystem/v3-ActCode",
  Code = coverage.CoverageType ?? "EHCPOL",
Display = "Extended Healthcare"
        }
     }
     },
            Beneficiary = new ResourceReference($"Patient/{coverage.PatientId}"),
   Subscriber = new ResourceReference($"Patient/{coverage.SubscriberPatientId ?? coverage.PatientId}"),
  Relationship = new CodeableConcept
    {
      Coding = new List<Coding>
       {
       new Coding
     {
      System = "http://terminology.hl7.org/CodeSystem/subscriber-relationship",
     Code = coverage.SubscriberRelationship?.ToLower() ?? "self",
             Display = coverage.SubscriberRelationship ?? "Self"
  }
    }
         },
          Period = new Period
      {
             Start = coverage.CoverageStartDate.ToString("yyyy-MM-dd"),
          End = coverage.CoverageEndDate.ToString("yyyy-MM-dd") // Not nullable in entity
       },
          Payor = new List<ResourceReference>
         {
        new ResourceReference($"Organization/{coverage.InsurerId}") // Use InsurerId
  },
         Meta = new Meta
          {
      Profile = new[] { NphiesProfiles.Coverage }
            }
        };

        return fhirCoverage;
    }

    /// <summary>
    /// Map Organization entity to FHIR Organization resource
    /// </summary>
    public FhirOrganization MapToFhirOrganization(DomainOrganization organization)
    {
        _logger.LogDebug("Mapping organization {OrgId} to FHIR resource", organization.Id);

        var isProvider = organization.OrganizationType?.ToLower() == "provider" ||
  organization.OrganizationType?.ToLower() == "prov";

        var identifierSystem = isProvider
        ? "http://nphies.sa/license/provider-license"
             : "http://nphies.sa/license/payer-license";

        var fhirOrg = new FhirOrganization
        {
            Id = organization.Id,
            Identifier = new List<Identifier>
  {
    new Identifier
           {
     System = identifierSystem,
    Value = organization.LicenseNumber
        }
    },
            Name = organization.OrganizationName,
            Active = organization.Status?.ToLower() == "active",
            Type = new List<CodeableConcept>
      {
  new CodeableConcept
       {
  Coding = new List<Coding>
      {
      new Coding
          {
    System = "http://terminology.hl7.org/CodeSystem/organization-type",
        Code = isProvider ? "prov" : "ins",
       Display = isProvider ? "Healthcare Provider" : "Insurance Company"
         }
  }
      }
 },
            Meta = new Meta
            {
                Profile = new[] { isProvider ? NphiesProfiles.ProviderOrganization : NphiesProfiles.InsurerOrganization }
            }
        };

        // Add telecom if available
        var telecom = new List<ContactPoint>();
        if (!string.IsNullOrWhiteSpace(organization.PhoneNumber))
        {
            telecom.Add(new ContactPoint
            {
                System = ContactPoint.ContactPointSystem.Phone,
                Value = organization.PhoneNumber
            });
        }
        if (!string.IsNullOrWhiteSpace(organization.Email))
        {
            telecom.Add(new ContactPoint
            {
                System = ContactPoint.ContactPointSystem.Email,
                Value = organization.Email
            });
        }
        if (telecom.Any())
            fhirOrg.Telecom = telecom;

        return fhirOrg;
    }

    /// <summary>
    /// Map Practitioner entity to FHIR Practitioner resource
    /// </summary>
    public FhirPractitioner MapToFhirPractitioner(DomainPractitioner practitioner)
    {
        _logger.LogDebug("Mapping practitioner {PractitionerId} to FHIR resource", practitioner.Id);

        return new FhirPractitioner
        {
            Id = practitioner.Id,
            Identifier = new List<Identifier>
      {
     new Identifier
    {
   System = "http://nphies.sa/license/practitioner-license",
             Value = practitioner.LicenseNumber
                }
   },
            Name = new List<HumanName>
       {
 new HumanName
             {
      Family = practitioner.LastName,
    Given = new[] { practitioner.FirstName },
 Text = $"{practitioner.FirstName} {practitioner.LastName}"
      }
            },
            Active = practitioner.Status?.ToLower() == "active",
            Qualification = new List<Practitioner.QualificationComponent>
    {
                new Practitioner.QualificationComponent
                {
       Code = new CodeableConcept
               {
            Coding = new List<Coding>
                 {
         new Coding
       {
         System = "http://nphies.sa/terminology/CodeSystem/practice-codes",
     Code = practitioner.Specialization,
    Display = practitioner.Specialization
           }
  }
     }
        }
        },
            Meta = new Meta
            {
                Profile = new[] { NphiesProfiles.Practitioner }
            }
        };
    }

    /// <summary>
    /// Map Encounter entity to FHIR Encounter resource
    /// </summary>
    public FhirEncounter MapToFhirEncounter(DomainEncounter encounter)
    {
        _logger.LogDebug("Mapping encounter {EncounterId} to FHIR resource", encounter.Id);

        var fhirEncounter = new FhirEncounter
        {
            Id = encounter.Id,
            Identifier = new List<Identifier>
      {
         new Identifier
        {
 System = "http://nphies.sa/identifier/encounter-id",
   Value = encounter.Id // Use Id since EncounterNumber doesn't exist
        }
      },
            Status = Enum.TryParse<Encounter.EncounterStatus>(encounter.Status.Replace("-", string.Empty), true, out var status)
                ? status
                : Encounter.EncounterStatus.Finished,
            Class = new Coding
        {
                System = "http://terminology.hl7.org/CodeSystem/v3-ActCode",
                Code = encounter.Class,
                Display = encounter.Class
            },
            Subject = new ResourceReference($"Patient/{encounter.PatientId}"),
            Period = new Period
            {
                Start = encounter.PeriodStart.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                End = encounter.PeriodEnd?.ToString("yyyy-MM-ddTHH:mm:ssZ")
            },
            Hospitalization = string.IsNullOrWhiteSpace(encounter.AdmitSource)
                ? null
                : new FhirEncounter.HospitalizationComponent
                {
                    AdmitSource = new CodeableConcept
                    {
                        Coding = new List<Coding>
                        {
                            new()
                            {
                                System = encounter.AdmitSourceSystem,
                                Code = encounter.AdmitSource,
                                Display = encounter.AdmitSource
                            }
                        }
                    }
                },
            ServiceProvider = string.IsNullOrWhiteSpace(encounter.ServiceProviderId)
                ? null
                : new ResourceReference($"Organization/{encounter.ServiceProviderId}"),
      Meta = new Meta
      {
  Profile = new[] { NphiesProfiles.Encounter }
        }
        };

        return fhirEncounter;
    }

    public FhirLocation MapToFhirLocation(DomainLocation location)
    {
        return new FhirLocation
        {
            Id = location.Id,
            Name = location.LocationName,
            Status = location.Status.Equals("active", StringComparison.OrdinalIgnoreCase)
                ? FhirLocation.LocationStatus.Active
                : FhirLocation.LocationStatus.Inactive,
            Identifier = new List<Identifier>
            {
                new()
                {
                    System = location.LicenseSystem,
                    Value = location.LocationLicense
                }
            },
            Address = new Address
            {
                Line = new[] { location.AddressLine1, location.AddressLine2 }
                    .Where(line => !string.IsNullOrWhiteSpace(line)).ToArray(),
                City = location.City,
                State = location.State,
                PostalCode = location.PostalCode,
                Country = location.Country
            },
            ManagingOrganization = new ResourceReference($"Organization/{location.OrganizationId}"),
            Meta = new Meta
            {
                Profile = new[] { NphiesProfiles.Location }
            }
        };
    }

    /// <summary>
    /// Create MessageHeader for NPHIES bundle
    /// </summary>
    public MessageHeader CreateMessageHeader(string eventCode, DomainOrganization source)
    {
        _logger.LogDebug("Creating MessageHeader with event code {EventCode}", eventCode);

        return new MessageHeader
   {
 Id = Guid.NewGuid().ToString(),
    Event = new Coding // Use Event not EventCoding for FHIR R4
        {
  System = "http://nphies.sa/terminology/CodeSystem/ksa-message-events",
   Code = eventCode
     },
      Source = new MessageHeader.MessageSourceComponent
    {
       Name = source.OrganizationName,
             Endpoint = source.LicenseNumber // Using license as endpoint identifier
            },
 Destination = new List<MessageHeader.MessageDestinationComponent>
  {
  new MessageHeader.MessageDestinationComponent
          {
   Name = "NPHIES",
   Endpoint = NphiesIdentifierSystems.NphiesEndpoint
    }
        },
            Meta = new Meta
    {
     Profile = new[] { NphiesProfiles.MessageHeader }
 }
        };
    }

    /// <summary>
    /// Map CoverageEligibilityRequest entity to FHIR resource
    /// </summary>
    public CoverageEligibilityRequest MapToFhirEligibilityRequest(
        DomainCoverageEligibilityRequest request,
      string patientRef,
        string coverageRef,
  string providerRef,
        string insurerRef)
    {
        _logger.LogDebug("Mapping eligibility request {RequestId} to FHIR resource", request.RequestId);

        return new CoverageEligibilityRequest
        {
            Id = request.Id,
            Identifier = new List<Identifier>
            {
            new Identifier
         {
        System = "http://nphies.sa/identifier/request-id",
     Value = request.RequestId
       }
   },
  Status = FinancialResourceStatusCodes.Active,
     Priority = new CodeableConcept
     {
 Coding = new List<Coding>
     {
   new Coding
     {
  System = "http://terminology.hl7.org/CodeSystem/processpriority",
  Code = request.Priority?.ToLower() ?? "normal",
      Display = request.Priority ?? "Normal"
    }
         }
      },
       Purpose = new List<CoverageEligibilityRequest.EligibilityRequestPurpose?>
      {
 CoverageEligibilityRequest.EligibilityRequestPurpose.Discovery,
    CoverageEligibilityRequest.EligibilityRequestPurpose.Benefits,
      CoverageEligibilityRequest.EligibilityRequestPurpose.Validation
            },
         Patient = new ResourceReference(patientRef),
          Serviced = new FhirDateTime(request.ServiceDate.ToString("yyyy-MM-dd")), // Use Serviced property
   Created = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Enterer = new ResourceReference(providerRef),
            Provider = new ResourceReference(providerRef),
   Insurer = new ResourceReference(insurerRef),
      Insurance = new List<CoverageEligibilityRequest.InsuranceComponent>
   {
 new CoverageEligibilityRequest.InsuranceComponent
    {
    Coverage = new ResourceReference(coverageRef),
   Focal = true
          }
    },
        Meta = new Meta
     {
                Profile = new[] { NphiesProfiles.EligibilityRequest }
}
        };
    }

    /// <summary>
    /// Map Claim entity to FHIR Claim resource
    /// </summary>
    public FhirClaim MapToFhirClaim(
        DomainClaim claim,
     string patientRef,
        string providerRef,
        string insurerRef,
      string coverageRef,
        string? encounterRef = null)
    {
        _logger.LogDebug("Mapping claim {ClaimNumber} to FHIR resource", claim.ClaimNumber);
        var currency = Enum.TryParse<Money.Currencies>(claim.TotalCurrency, true, out var parsedCurrency)
            ? parsedCurrency
            : Money.Currencies.SAR;

        var claimDate = claim.BillablePeriodStart ?? claim.CreatedAt;
        var fhirClaim = new FhirClaim
        {
            Id = claim.Id,
            Identifier = new List<Identifier>
            {
                new()
                {
                    System = claim.ClaimIdentifierSystem ?? "http://nphies.sa/identifier/claim-id",
                    Value = claim.ClaimIdentifierValue ?? claim.ClaimNumber
                }
            },
            Status = FinancialResourceStatusCodes.Active,
            Type = new CodeableConcept
            {
                Coding = new List<Coding>
                {
                    new()
                    {
                        System = claim.ClaimTypeSystem ?? "http://terminology.hl7.org/CodeSystem/claim-type",
                        Code = claim.ClaimType?.ToLowerInvariant() ?? "institutional",
                        Display = claim.ClaimType
                    }
                }
            },
            SubType = string.IsNullOrWhiteSpace(claim.ClaimSubType)
                ? null
                : new CodeableConcept
                {
                    Coding = new List<Coding>
                    {
                        new()
                        {
                            System = claim.ClaimSubTypeSystem ?? "http://nphies.sa/terminology/CodeSystem/claim-subtype",
                            Code = claim.ClaimSubType.ToLowerInvariant(),
                            Display = claim.ClaimSubType
                        }
                    }
                },
            Use = claim.Use?.ToLowerInvariant() == "preauthorization"
                ? ClaimUseCode.Preauthorization
                : ClaimUseCode.Claim,
            Patient = new ResourceReference(patientRef),
            BillablePeriod = new Period
            {
                Start = (claim.BillablePeriodStart ?? claimDate).ToString("yyyy-MM-dd"),
                End = (claim.BillablePeriodEnd ?? claimDate).ToString("yyyy-MM-dd")
            },
            Created = claim.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Provider = new ResourceReference(providerRef),
            Insurer = new ResourceReference(insurerRef),
            Facility = string.IsNullOrWhiteSpace(claim.LocationId)
                ? null
                : new ResourceReference($"Location/{claim.LocationId}"),
            Priority = new CodeableConcept
            {
                Coding = new List<Coding>
                {
                    new()
                    {
                        System = "http://terminology.hl7.org/CodeSystem/processpriority",
                        Code = claim.Priority?.ToLowerInvariant() ?? "normal",
                        Display = claim.Priority ?? "Normal"
                    }
                }
            },
            Insurance = new List<Claim.InsuranceComponent>
            {
                new()
                {
                    Sequence = 1,
                    Focal = true,
                    Coverage = new ResourceReference(coverageRef)
                }
            },
            Total = new Money
            {
                Value = claim.Total,
                Currency = currency
            },
            Meta = new Meta
            {
                Profile = new[] { NphiesProfiles.ForClaim(claim.ClaimType?.ToLowerInvariant() ?? "institutional", claim.Use?.ToLowerInvariant() == "preauthorization") }
            }
        };

        if (claim.Diagnoses.Count > 0)
        {
            fhirClaim.Diagnosis = claim.Diagnoses.OrderBy(d => d.Sequence)
                .Select(d => new FhirClaim.DiagnosisComponent
                {
                    Sequence = d.Sequence,
                    Diagnosis = new CodeableConcept
                    {
                        Coding = new List<Coding>
                        {
                            new()
                            {
                                System = d.DiagnosisSystem ?? "http://hl7.org/fhir/sid/icd-10",
                                Code = d.DiagnosisCode,
                                Display = d.DiagnosisDisplay
                            }
                        }
                    },
                    Type = string.IsNullOrWhiteSpace(d.DiagnosisType)
                        ? null
                        : new List<CodeableConcept>
                        {
                            new CodeableConcept
                            {
                                Coding = new List<Coding>
                                {
                                    new()
                                    {
                                        System = d.DiagnosisTypeSystem,
                                        Code = d.DiagnosisType,
                                        Display = d.DiagnosisType
                                    }
                                }
                            }
                        }
                }).ToList();
        }

        if (claim.CareTeam.Count > 0)
        {
            fhirClaim.CareTeam = claim.CareTeam.OrderBy(member => member.Sequence)
                .Select(member => new FhirClaim.CareTeamComponent
                {
                    Sequence = member.Sequence,
                    Provider = new ResourceReference($"Practitioner/{member.PractitionerId}"),
                    Role = string.IsNullOrWhiteSpace(member.Role)
                        ? null
                        : new CodeableConcept
                        {
                            Coding = new List<Coding>
                            {
                                new()
                                {
                                    System = member.RoleSystem,
                                    Code = member.Role,
                                    Display = member.RoleDisplay
                                }
                            }
                        },
                    Qualification = string.IsNullOrWhiteSpace(member.Qualification)
                        ? null
                        : new CodeableConcept
                        {
                            Coding = new List<Coding>
                            {
                                new()
                                {
                                    System = member.QualificationSystem,
                                    Code = member.Qualification,
                                    Display = member.QualificationDisplay
                                }
                            }
                        }
                }).ToList();
        }

        if (claim.Items.Count > 0)
        {
            fhirClaim.Item = claim.Items.OrderBy(item => item.Sequence)
                .Select(item => new FhirClaim.ItemComponent
                {
                    Sequence = item.Sequence,
                    ProductOrService = new CodeableConcept
                    {
                        Coding = new List<Coding>
                        {
                            new()
                            {
                                System = item.ProductOrServiceSystem,
                                Code = item.ProductOrServiceCode,
                                Display = item.ProductOrServiceDisplay
                            }
                        }
                    },
                    Serviced = item.ServicedDate.HasValue
                        ? new FhirDateTime(item.ServicedDate.Value.ToString("yyyy-MM-dd"))
                        : null,
                    Quantity = item.Quantity.HasValue
                        ? new Quantity(item.Quantity.Value, "1", "http://unitsofmeasure.org")
                        : null,
                    UnitPrice = item.UnitPrice.HasValue
                        ? new Money { Value = item.UnitPrice.Value, Currency = currency }
                        : null,
                    Net = item.Net.HasValue
                        ? new Money { Value = item.Net.Value, Currency = currency }
                        : null,
                    Encounter = string.IsNullOrWhiteSpace(encounterRef)
                        ? null
                        : new List<ResourceReference> { new(encounterRef) },
                    CareTeamSequence = item.CareTeamSequence.HasValue
                        ? new List<int?> { item.CareTeamSequence.Value }
                        : null,
                    DiagnosisSequence = string.IsNullOrWhiteSpace(item.DiagnosisSequence)
                        ? null
                        : item.DiagnosisSequence.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(value => int.TryParse(value.Trim(), out var sequence) ? sequence : (int?)null)
                            .Where(sequence => sequence.HasValue)
                            .Select(sequence => sequence)
                            .ToList(),
                    Detail = item.Details.OrderBy(detail => detail.Sequence)
                        .Select(detail => new FhirClaim.DetailComponent
                        {
                            Sequence = detail.Sequence,
                            ProductOrService = new CodeableConcept
                            {
                                Coding = new List<Coding>
                                {
                                    new()
                                    {
                                        System = detail.ProductOrServiceSystem,
                                        Code = detail.ProductOrServiceCode,
                                        Display = detail.ProductOrServiceDisplay
                                    }
                                }
                            },
                            Quantity = detail.Quantity.HasValue
                                ? new Quantity(detail.Quantity.Value, "1", "http://unitsofmeasure.org")
                                : null,
                            UnitPrice = detail.UnitPrice.HasValue
                                ? new Money { Value = detail.UnitPrice.Value, Currency = currency }
                                : null,
                            Net = detail.Net.HasValue
                                ? new Money { Value = detail.Net.Value, Currency = currency }
                                : null
                        }).ToList()
                }).ToList();
        }

        if (claim.SupportingInfo.Count > 0)
        {
            fhirClaim.SupportingInfo = claim.SupportingInfo.OrderBy(info => info.Sequence)
                .Select(info => new FhirClaim.SupportingInformationComponent
                {
                    Sequence = info.Sequence,
                    Category = new CodeableConcept
                    {
                        Coding = new List<Coding>
                        {
                            new()
                            {
                                System = info.CategorySystem,
                                Code = info.Category,
                                Display = info.CategoryDisplay
                            }
                        }
                    },
                    Code = string.IsNullOrWhiteSpace(info.CodeValue)
                        ? null
                        : new CodeableConcept
                        {
                            Coding = new List<Coding>
                            {
                                new()
                                {
                                    System = info.CodeSystem,
                                    Code = info.CodeValue
                                }
                            }
                        },
                    Value = info.QuantityValue.HasValue
                        ? new Quantity(
                            info.QuantityValue.Value,
                            info.QuantityUnit ?? "1",
                            info.QuantitySystem ?? "http://unitsofmeasure.org")
                        : !string.IsNullOrWhiteSpace(info.StringValue)
                            ? new FhirString(info.StringValue)
                            : null
                }).ToList();
        }

     return fhirClaim;
    }

    /// <summary>
    /// Create NPHIES-specific extension
    /// </summary>
    public Extension CreateNphiesExtension(string url, DataType value)
    {
        return new Extension
        {
            Url = url,
            Value = value
        };
    }

    /// <summary>
    /// Create NPHIES identifier
    /// </summary>
    public Identifier CreateNphiesIdentifier(string system, string value)
    {
        return new Identifier
        {
            System = system,
            Value = value
        };
    }
}
