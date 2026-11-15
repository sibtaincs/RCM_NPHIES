namespace NPhies_FHIR_Integration.Application.Rcm;

/// <summary>
/// Plain-language explanations for the front office: what each <c>siteEligibility</c> code means for this visit and
/// what to do about the payer error codes an eligibility request commonly gets back (Eligibility use case, IG 6.5).
/// </summary>
public static class RcmEligibilityGuidance
{
    public const string Eligible = "eligible";

    private static readonly Dictionary<string, string> SiteEligibility = new(StringComparer.OrdinalIgnoreCase)
    {
        ["eligible"] = "The patient is covered at this facility.",
        ["not-active"] = "The policy is not active on the service date. Ask the patient for a current policy or treat as self-pay.",
        ["not-covered"] = "The policy does not cover the patient at this facility. Check the plan, or treat as self-pay.",
        ["not-direct-billing"] = "The policy is active, but this facility cannot bill the payer directly. The patient pays and claims reimbursement.",
        ["out-network"] = "This facility is outside the policy's network. The patient may pay more, or need the payer's approval first.",
        ["limit-exhausted"] = "The policy's benefit limit is used up. Further services are not covered.",
        ["coverage-suspended"] = "The payer has suspended the coverage. Ask the patient to contact the insurer.",
        ["provider-contract-suspended"] = "The payer has suspended its contract with this facility. Contact the payer's provider relations.",
    };

    private static readonly Dictionary<string, string> PayerErrors = new(StringComparer.Ordinal)
    {
        ["1658"] = "The payer found no member with this identity. Check the document type and number and the member id, or run a discovery request.",
        ["1659"] = "This facility has no contract with the payer on the service date. The visit cannot be billed directly.",
        ["1660"] = "The request went to a TPA that does not handle this insurer. Send it to the insurer's own TPA or to the insurer.",
        ["1664"] = "The policy is cancelled or expired. Ask the patient for a current policy.",
        ["1665"] = "The newborn is more than 30 days old and must have its own policy. Check eligibility with the baby's own member id, not the mother's coverage.",
        ["1666"] = "The payer does not recognise the provider licence. Check the licence number in the facility's master data.",
        ["1680"] = "The identity document number has the wrong format. A national id or Iqama has 10 digits.",
        ["1681"] = "The membership number has the wrong format. Copy it exactly from the insurance card.",
        ["1682"] = "The patient's details do not match the member the payer holds. Check the name, date of birth, gender and document number.",
    };

    /// <summary>What a <c>siteEligibility</c> code means for the visit, or null for an unknown or missing code.</summary>
    public static string? SiteEligibilityReason(string? code) =>
        code != null && SiteEligibility.TryGetValue(code, out var reason) ? reason : null;

    /// <summary>What to do about a payer error code, or null when the code has no specific advice.</summary>
    public static string? PayerErrorExplanation(string? code) =>
        code != null && PayerErrors.TryGetValue(code.Trim(), out var explanation) ? explanation : null;
}
