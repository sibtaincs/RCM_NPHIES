using Hl7.Fhir.Model;
using NPhies_FHIR_Integration.Application.Nphies.Models;

namespace NPhies_FHIR_Integration.Application.Nphies.Builders;

/// <summary>
/// Builds NPHIES FHIR message Bundles (Bundle.type = message, MessageHeader first) matching the
/// NPHIES examples. Every method returns a complete, self-contained Bundle ready for
/// <c>INphiesMessagingClient.SendAsync</c>.
/// </summary>
public interface INphiesBundleBuilder
{
    /// <summary>eligibility-request (benefits, validation, discovery, auth-requirements).</summary>
    Bundle BuildEligibilityRequest(EligibilityRequestInput input);

    /// <summary>priorauth-request for any claim type (forces <see cref="NphiesClaimUse.PreAuthorization"/>).</summary>
    Bundle BuildPriorAuthRequest(ClaimRequestInput input);

    /// <summary>claim-request for any claim type, including shadow billing (package items with details).</summary>
    Bundle BuildClaimRequest(ClaimRequestInput input);

    /// <summary>batch-request wrapping up to 200 claim-request bundles for the same insurer.</summary>
    Bundle BuildBatchRequest(BatchRequestInput input);

    /// <summary>status-check Task for a previously submitted claim / prior authorization.</summary>
    Bundle BuildStatusCheck(ClaimTaskInput input);

    /// <summary>cancel-request Task (task-code cancel or nullify).</summary>
    Bundle BuildCancelRequest(ClaimTaskInput input);

    /// <summary>poll-request Task addressed to NPHIES.</summary>
    Bundle BuildPollRequest(PollRequestInput input);

    /// <summary>communication (solicited when BasedOnCommunicationRequest is set, otherwise unsolicited).</summary>
    Bundle BuildCommunication(CommunicationInput input);

    /// <summary>payment-notice: acknowledgement of a received payment-reconciliation.</summary>
    Bundle BuildPaymentNotice(PaymentNoticeInput input);
}
