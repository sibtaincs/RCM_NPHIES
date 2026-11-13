using NPhies_FHIR_Integration.Application.Rcm;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

/// <summary>NPHIES rejection-code categories (IG V2.6 section 6.3.10.2): AD, CV, MN, SE, BE.</summary>
[TestClass]
public class DenialCategoryTests
{
    [DataTestMethod]
    [DataRow("AD-1-4", "Administrative")]
    [DataRow("CV-1-3", "Coverage")]
    [DataRow("MN-1-1", "MedicalNecessity")]
    [DataRow("SE-1-6", "SupportingEvidence")]
    [DataRow("BE-1-4", "BillingError")]
    [DataRow("be-1-1", "BillingError")]
    [DataRow("se-1-1", "SupportingEvidence")]
    [DataRow("GE-00012", "Technical")]
    [DataRow(null, "Other")]
    public void Maps_rejection_codes_to_their_nphies_category(string? code, string expected) =>
        Assert.AreEqual(expected, RcmResponseIngestor.DenialCategory(code));
}
