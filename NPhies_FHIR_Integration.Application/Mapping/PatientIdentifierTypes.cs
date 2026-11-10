using NPhies_FHIR_Integration.Application.Nphies.Models;
using NPhies_FHIR_Integration.Application.Rcm;

namespace NPhies_FHIR_Integration.Application.Mapping;

/// <summary>
/// Converts between the API-facing identifier type (NationalId, Iqama, Passport, ...) and the
/// NPHIES identifier-type code stored on <c>Patient.NationalIdType</c> (NI, PRC, PPN, BN, DP, MR).
/// </summary>
public static class PatientIdentifierTypes
{
    public static PatientIdentifierKind Resolve(string? storedType, string? value) =>
        RcmInputMapper.ParseIdentifierKind(storedType, value);

    public static string Describe(string? storedType, string? value) => Resolve(storedType, value).ToString();

    /// <summary>Returns the NPHIES code for a user-supplied type, or null when none was supplied.</summary>
    public static string? ToCode(string? identifierType, string? value)
    {
        if (string.IsNullOrWhiteSpace(identifierType)) return null;
        return Resolve(identifierType, value) switch
        {
            PatientIdentifierKind.Iqama => "PRC",
            PatientIdentifierKind.Passport => "PPN",
            PatientIdentifierKind.BorderNumber => "BN",
            PatientIdentifierKind.DisplacedPerson => "DP",
            PatientIdentifierKind.MedicalRecord => "MR",
            _ => "NI",
        };
    }
}
