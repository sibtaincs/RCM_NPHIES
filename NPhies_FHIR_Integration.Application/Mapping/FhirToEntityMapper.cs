using System.Text.Json;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.FHIR;

namespace NPhies_FHIR_Integration.Application.Mapping;

/// <summary>
/// Maps FHIR JSON data to domain entities
/// Handles conversion from FHIR bundle resources to strongly-typed entities
/// </summary>
public interface IFhirToEntityMapper
{
    /// <summary>
    /// Map FHIR Patient JSON to Patient entity
    /// </summary>
    Patient? MapFhirPatient(JsonElement patientJson);

    /// <summary>
    /// Map FHIR Coverage JSON to Coverage entity
    /// </summary>
    Coverage? MapFhirCoverage(JsonElement coverageJson);

    /// <summary>
    /// Map FHIR Organization JSON to Organization entity
    /// </summary>
    Organization? MapFhirOrganization(JsonElement organizationJson);

    /// <summary>
    /// Map FHIR Location JSON to Location entity
    /// </summary>
    Location? MapFhirLocation(JsonElement locationJson);

    /// <summary>
    /// Map FHIR Practitioner JSON to Practitioner entity
    /// </summary>
    Practitioner? MapFhirPractitioner(JsonElement practitionerJson);

    /// <summary>
    /// Map FHIR MessageHeader JSON to MessageHeader entity
    /// </summary>
    MessageHeader? MapFhirMessageHeader(JsonElement messageHeaderJson);

    /// <summary>
    /// Map FHIR CoverageEligibilityRequest JSON to entity
    /// </summary>
    CoverageEligibilityRequest? MapFhirCoverageEligibilityRequest(JsonElement requestJson);

    /// <summary>
    /// Map FHIR CoverageEligibilityResponse JSON to entity
    /// </summary>
    CoverageEligibilityResponse? MapFhirCoverageEligibilityResponse(JsonElement responseJson);

    /// <summary>
    /// Map FHIR Bundle to extract all resources
    /// </summary>
    Task<(CoverageEligibilityRequest? request, Dictionary<string, object> resources)> MapFhirBundleAsync(string bundleJson);
}

/// <summary>
/// Implementation of FHIR to Entity mapper
/// </summary>
public class FhirToEntityMapper : IFhirToEntityMapper
{
    /// <summary>
    /// Map FHIR Patient JSON to Patient entity
    /// </summary>
    public Patient? MapFhirPatient(JsonElement patientJson)
    {
     try
        {
  var patient = new Patient
            {
          Id = GetPropertyValue(patientJson, "id"),
       MRN = GetIdentifierValue(patientJson, "http://nphies.sa/identifier/member-id"),
                NationalId = GetIdentifierValue(patientJson, "urn:oid:2.16.840.1.113883.3.571.1.1"),
          FirstName = GetNameGiven(patientJson),
    LastName = GetNameFamily(patientJson),
    DateOfBirth = DateTime.TryParse(GetPropertyValue(patientJson, "birthDate"), out var dob) ? dob : DateTime.MinValue,
       Gender = GetPropertyValue(patientJson, "gender") switch
     {
   "male" => "M",
     "female" => "F",
          "other" => "O",
              "unknown" => "U",
        _ => "U"
     },
       Email = GetTelecomValue(patientJson, "email"),
 Phone = GetTelecomValue(patientJson, "phone"),
   AddressLine1 = GetAddressLine1(patientJson),
       AddressLine2 = GetAddressLine2(patientJson),
  City = GetAddressCity(patientJson),
        State = GetAddressState(patientJson),
            PostalCode = GetAddressPostalCode(patientJson),
           Country = GetAddressCountry(patientJson),
          Status = "active"
    };

    return patient;
        }
        catch (Exception ex)
     {
            throw new InvalidOperationException("Failed to map FHIR Patient to entity", ex);
        }
    }

    /// <summary>
    /// Map FHIR Coverage JSON to Coverage entity
    /// </summary>
    public Coverage? MapFhirCoverage(JsonElement coverageJson)
    {
        try
        {
 var coverage = new Coverage
       {
        Id = GetPropertyValue(coverageJson, "id"),
       PolicyNumber = GetIdentifierValue(coverageJson, "http://nphies.sa/identifier/policy-number"),
              MemberID = GetIdentifierValue(coverageJson, "http://nphies.sa/license/payer-member-id"),
   CoverageType = GetPropertyValue(coverageJson, "type.coding[0].code"),
                Status = GetPropertyValue(coverageJson, "status"),
      CoverageStartDate = DateTime.TryParse(GetPropertyValue(coverageJson, "period.start"), out var start) ? start : DateTime.MinValue,
        CoverageEndDate = DateTime.TryParse(GetPropertyValue(coverageJson, "period.end"), out var end) ? end : DateTime.MaxValue,
       RelationToSubscriber = GetPropertyValue(coverageJson, "relationship.coding[0].code") ?? "Self"
      };

       return coverage;
     }
        catch (Exception ex)
     {
  throw new InvalidOperationException("Failed to map FHIR Coverage to entity", ex);
        }
    }

  /// <summary>
    /// Map FHIR Organization JSON to Organization entity
    /// </summary>
    public Organization? MapFhirOrganization(JsonElement organizationJson)
    {
        try
        {
            var organization = new Organization
   {
    Id = GetPropertyValue(organizationJson, "id"),
                OrganizationName = GetPropertyValue(organizationJson, "name"),
      LicenseNumber = GetIdentifierValue(organizationJson, "http://nphies.sa/license/provider-license") 
 ?? GetIdentifierValue(organizationJson, "http://nphies.sa/license/payer-license"),
            OrganizationType = GetPropertyValue(organizationJson, "type[0].coding[0].code"),
  Email = GetContactEmail(organizationJson),
       PhoneNumber = GetContactPhone(organizationJson),
       Status = GetPropertyValue(organizationJson, "active") == "true" ? "active" : "inactive"
            };

            return organization;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to map FHIR Organization to entity", ex);
      }
    }

/// <summary>
    /// Map FHIR Location JSON to Location entity
    /// </summary>
    public Location? MapFhirLocation(JsonElement locationJson)
    {
     try
        {
       var location = new Location
            {
     Id = GetPropertyValue(locationJson, "id"),
        LocationName = GetPropertyValue(locationJson, "name"),
    LocationLicense = GetIdentifierValue(locationJson, "http://nphies.sa/license/location-license"),
   FacilityType = GetPropertyValue(locationJson, "type[0].coding[0].code"),
            Status = GetPropertyValue(locationJson, "status")
  };

            return location;
        }
        catch (Exception ex)
    {
            throw new InvalidOperationException("Failed to map FHIR Location to entity", ex);
        }
    }

  /// <summary>
    /// Map FHIR Practitioner JSON to Practitioner entity
    /// </summary>
    public Practitioner? MapFhirPractitioner(JsonElement practitionerJson)
    {
        try
    {
     var practitioner = new Practitioner
    {
         Id = GetPropertyValue(practitionerJson, "id"),
 FirstName = GetNameGiven(practitionerJson),
        LastName = GetNameFamily(practitionerJson),
           LicenseNumber = GetIdentifierValue(practitionerJson, "http://nphies.sa/license/practitioner-license"),
           Specialization = GetPropertyValue(practitionerJson, "qualification[0].code.coding[0].code"),
     Status = "active"
         };

     return practitioner;
   }
   catch (Exception ex)
        {
    throw new InvalidOperationException("Failed to map FHIR Practitioner to entity", ex);
        }
}

    /// <summary>
    /// Map FHIR MessageHeader JSON to MessageHeader entity
    /// </summary>
    public MessageHeader? MapFhirMessageHeader(JsonElement messageHeaderJson)
    {
        try
 {
 var messageHeader = new MessageHeader
  {
          Id = GetPropertyValue(messageHeaderJson, "id"),
        MessageUUID = GetPropertyValue(messageHeaderJson, "id"),
   EventCode = GetPropertyValue(messageHeaderJson, "eventCoding.code"),
       SourceName = GetPropertyValue(messageHeaderJson, "source.name"),
        MessageTimestamp = DateTime.UtcNow,
   Status = "received"
 };

         return messageHeader;
     }
        catch (Exception ex)
   {
            throw new InvalidOperationException("Failed to map FHIR MessageHeader to entity", ex);
        }
    }

    /// <summary>
    /// Map FHIR CoverageEligibilityRequest JSON to entity
    /// </summary>
    public CoverageEligibilityRequest? MapFhirCoverageEligibilityRequest(JsonElement requestJson)
    {
        try
    {
            var request = new CoverageEligibilityRequest
     {
                Id = GetPropertyValue(requestJson, "id"),
    RequestId = GetIdentifierValue(requestJson, "http://nphies.sa/identifier/request-id"),
     Status = GetPropertyValue(requestJson, "status"),
    Priority = GetPropertyValue(requestJson, "priority.coding[0].code") ?? "normal",
  ServiceDate = DateTime.TryParse(GetPropertyValue(requestJson, "servicedDate"), out var sd) ? sd : DateTime.MinValue,
  RequestCreatedAt = DateTime.UtcNow,
           SubmittedAt = DateTime.UtcNow
            };

            return request;
        }
        catch (Exception ex)
        {
       throw new InvalidOperationException("Failed to map FHIR CoverageEligibilityRequest to entity", ex);
        }
    }

    /// <summary>
    /// Map FHIR CoverageEligibilityResponse JSON to entity
    /// </summary>
    public CoverageEligibilityResponse? MapFhirCoverageEligibilityResponse(JsonElement responseJson)
    {
   try
        {
        var response = new CoverageEligibilityResponse
        {
        Id = GetPropertyValue(responseJson, "id"),
            ResponseUUID = GetPropertyValue(responseJson, "id"),
                Status = GetPropertyValue(responseJson, "status"),
        Outcome = GetPropertyValue(responseJson, "outcome"),
    EligibilityStatus = GetPropertyValue(responseJson, "insurance[0].inforce") == "true" ? "active" : "inactive",
    IsInForce = GetPropertyValue(responseJson, "insurance[0].inforce") == "true",
  ResponseCreatedAt = DateTime.UtcNow,
                ResponseReceivedAt = DateTime.UtcNow
            };

            return response;
        }
    catch (Exception ex)
        {
     throw new InvalidOperationException("Failed to map FHIR CoverageEligibilityResponse to entity", ex);
        }
    }

    /// <summary>
    /// Map FHIR Bundle to extract all resources
    /// </summary>
 public async Task<(CoverageEligibilityRequest? request, Dictionary<string, object> resources)> MapFhirBundleAsync(string bundleJson)
    {
        return await Task.Run(() =>
        {
            try
     {
     var bundle = FhirJsonSerializer.Deserialize<FhirBundle>(bundleJson);
       if (bundle?.Entry == null || bundle.Entry.Count == 0)
    return (null, new Dictionary<string, object>());

   CoverageEligibilityRequest? request = null;
  var resources = new Dictionary<string, object>();

       foreach (var entry in bundle.Entry)
    {
        var resourceType = GetPropertyValue(entry.Resource, "resourceType");
              var id = GetPropertyValue(entry.Resource, "id");

      switch (resourceType)
       {
       case "CoverageEligibilityRequest":
      request = MapFhirCoverageEligibilityRequest(entry.Resource);
                if (request != null)
     resources[id] = request;
       break;

  case "Patient":
           var patient = MapFhirPatient(entry.Resource);
 if (patient != null)
             resources[id] = patient;
 break;

         case "Coverage":
          var coverage = MapFhirCoverage(entry.Resource);
          if (coverage != null)
    resources[id] = coverage;
           break;

   case "Organization":
       var org = MapFhirOrganization(entry.Resource);
               if (org != null)
             resources[id] = org;
     break;

    case "Location":
var location = MapFhirLocation(entry.Resource);
    if (location != null)
resources[id] = location;
    break;

        case "Practitioner":
       var practitioner = MapFhirPractitioner(entry.Resource);
          if (practitioner != null)
       resources[id] = practitioner;
       break;
             }
        }

        return (request, resources);
     }
          catch (Exception ex)
   {
     throw new InvalidOperationException("Failed to map FHIR Bundle to entities", ex);
            }
     });
    }

    // Helper methods

    private static string? GetPropertyValue(JsonElement element, string path)
    {
        var parts = path.Split('.');
     var current = element;

      foreach (var part in parts)
        {
      if (part.Contains('['))
            {
                var bracketIndex = part.IndexOf('[');
  var propName = part[..bracketIndex];
     var index = int.Parse(part[(bracketIndex + 1)..].TrimEnd(']'));

              if (current.TryGetProperty(propName, out var arrayElement))
                {
   if (arrayElement.ValueKind == JsonValueKind.Array && index < arrayElement.GetArrayLength())
   {
           current = arrayElement[index];
    }
     else
     return null;
      }
          else
     return null;
            }
      else
        {
    if (current.TryGetProperty(part, out var nextElement))
          current = nextElement;
      else
     return null;
 }
        }

        return current.ValueKind switch
        {
       JsonValueKind.String => current.GetString(),
 JsonValueKind.Number => current.GetDecimal().ToString(),
            JsonValueKind.True => "true",
   JsonValueKind.False => "false",
          _ => null
        };
    }

    private static string? GetIdentifierValue(JsonElement element, string system)
    {
        if (element.TryGetProperty("identifier", out var identifierArray) && identifierArray.ValueKind == JsonValueKind.Array)
      {
       foreach (var identifier in identifierArray.EnumerateArray())
            {
   if (identifier.TryGetProperty("system", out var systemProp) && 
     systemProp.GetString() == system &&
 identifier.TryGetProperty("value", out var valueProp))
     {
           return valueProp.GetString();
                }
       }
     }
        return null;
    }

    private static string? GetNameFamily(JsonElement element)
    {
        if (element.TryGetProperty("name", out var nameArray) && nameArray.ValueKind == JsonValueKind.Array && nameArray.GetArrayLength() > 0)
        {
     var name = nameArray[0];
     if (name.TryGetProperty("family", out var family))
        return family.GetString();
        }
  return null;
    }

    private static string? GetNameGiven(JsonElement element)
    {
        if (element.TryGetProperty("name", out var nameArray) && nameArray.ValueKind == JsonValueKind.Array && nameArray.GetArrayLength() > 0)
        {
    var name = nameArray[0];
  if (name.TryGetProperty("given", out var givenArray) && givenArray.ValueKind == JsonValueKind.Array && givenArray.GetArrayLength() > 0)
    return givenArray[0].GetString();
     }
        return null;
    }

    private static string? GetTelecomValue(JsonElement element, string system)
    {
        if (element.TryGetProperty("telecom", out var telecomArray) && telecomArray.ValueKind == JsonValueKind.Array)
        {
  foreach (var telecom in telecomArray.EnumerateArray())
        {
  if (telecom.TryGetProperty("system", out var sysProp) && 
      sysProp.GetString() == system &&
       telecom.TryGetProperty("value", out var valueProp))
                {
           return valueProp.GetString();
             }
            }
        }
    return null;
    }

    private static string? GetAddressLine1(JsonElement element)
    {
  return GetAddressProperty(element, "line", 0);
    }

    private static string? GetAddressLine2(JsonElement element)
    {
   return GetAddressProperty(element, "line", 1);
    }

    private static string? GetAddressCity(JsonElement element)
 {
        if (element.TryGetProperty("address", out var addressArray) && addressArray.ValueKind == JsonValueKind.Array && addressArray.GetArrayLength() > 0)
        {
         var address = addressArray[0];
       if (address.TryGetProperty("city", out var city))
          return city.GetString();
        }
        return null;
    }

    private static string? GetAddressState(JsonElement element)
    {
        if (element.TryGetProperty("address", out var addressArray) && addressArray.ValueKind == JsonValueKind.Array && addressArray.GetArrayLength() > 0)
        {
    var address = addressArray[0];
            if (address.TryGetProperty("state", out var state))
         return state.GetString();
   }
        return null;
    }

 private static string? GetAddressPostalCode(JsonElement element)
    {
     if (element.TryGetProperty("address", out var addressArray) && addressArray.ValueKind == JsonValueKind.Array && addressArray.GetArrayLength() > 0)
        {
      var address = addressArray[0];
            if (address.TryGetProperty("postalCode", out var postalCode))
         return postalCode.GetString();
        }
        return null;
    }

 private static string? GetAddressCountry(JsonElement element)
    {
        if (element.TryGetProperty("address", out var addressArray) && addressArray.ValueKind == JsonValueKind.Array && addressArray.GetArrayLength() > 0)
        {
 var address = addressArray[0];
        if (address.TryGetProperty("country", out var country))
            return country.GetString();
    }
    return null;
    }

    private static string? GetAddressProperty(JsonElement element, string property, int index)
    {
        if (element.TryGetProperty("address", out var addressArray) && addressArray.ValueKind == JsonValueKind.Array && addressArray.GetArrayLength() > 0)
        {
            var address = addressArray[0];
     if (address.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.Array && prop.GetArrayLength() > index)
      return prop[index].GetString();
        }
  return null;
    }

    private static string? GetContactEmail(JsonElement element)
    {
        return GetTelecomValue(element, "email");
    }

  private static string? GetContactPhone(JsonElement element)
    {
        return GetTelecomValue(element, "phone");
    }
}
