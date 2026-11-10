using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;
using NPhies_FHIR_Integration.Infrastructure.Data.Seeders;

namespace NPhies_FHIR_Integration.Application.Nphies.Sandbox;

public sealed class RcmSampleActivityResult
{
    public bool Skipped { get; set; }
    public int EligibilityChecks { get; set; }
    public int Submitted { get; set; }
    public int Polls { get; set; }
    public int Communications { get; set; }
    public int Appeals { get; set; }
    public List<string> Notes { get; } = new();
}

/// <summary>
/// Development seeder: replays the <see cref="SampleDataSeeder"/> claims through the real RCM workflow services
/// against <see cref="NphiesSandboxTransport"/>, so the database holds genuine eligibility responses, adjudications,
/// denials, payments (incl. a discrepancy), payer communication requests, provider replies and appeals. Timestamps
/// are then shifted relative to each claim's service date so aging and dashboards look realistic. Idempotent.
/// </summary>
public static class RcmSampleActivitySeeder
{
    private static readonly byte[] SamplePdf = System.Text.Encoding.ASCII.GetBytes(
        "%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj 2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj " +
        "3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 200 200]>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF");

    public static async Task<RcmSampleActivityResult> SeedAsync(IServiceProvider services, ILogger logger, CancellationToken ct = default)
    {
        var result = new RcmSampleActivityResult();
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var prefix = SampleDataSeeder.ClaimPrefix;

        if (await db.Claims.AnyAsync(c => c.ClaimNumber.StartsWith(prefix) && c.NphiesStatus != null && c.NphiesStatus != RcmClaimStatus.Draft, ct))
        {
            result.Skipped = true;
            return result;
        }

        var samples = await db.Claims.AsNoTracking()
            .Where(c => c.ClaimNumber.StartsWith(prefix) && c.Use != null)
            .OrderBy(c => c.ClaimNumber)
            .Select(c => new { c.Id, c.ClaimNumber, c.Use, c.PatientId, c.CoverageId, c.ProviderId, c.InsurerId, c.ServicedPeriodStart })
            .ToListAsync(ct);
        if (samples.Count == 0)
        {
            result.Skipped = true;
            result.Notes.Add("No sample claims; run SampleDataSeeder first.");
            return result;
        }

        var eligibility = sp.GetRequiredService<IRcmEligibilityService>();
        var workflow = sp.GetRequiredService<IRcmClaimWorkflowService>();
        var poll = sp.GetRequiredService<IRcmPollService>();
        var communications = sp.GetRequiredService<IRcmCommunicationService>();
        var appeals = sp.GetRequiredService<IRcmAppealService>();

        using var sandbox = NphiesSandboxTransport.Activate();

        // 1) Eligibility for every sample member.
        foreach (var m in samples.Where(s => s.CoverageId != null).GroupBy(s => (s.PatientId, s.CoverageId, s.ProviderId)).Select(g => g.First()))
        {
            await Try(result, $"eligibility {m.ClaimNumber}", async () =>
            {
                await eligibility.CheckAsync(new EligibilityCheckRequestDto
                {
                    PatientId = m.PatientId!,
                    CoverageId = m.CoverageId,
                    ProviderId = m.ProviderId!,
                    InsurerId = m.InsurerId,
                    Purposes = new List<string> { "benefits", "validation" },
                    ServicedDate = m.ServicedPeriodStart ?? DateTime.UtcNow.Date,
                }, ct);
                result.EligibilityChecks++;
            });
        }

        // 2) Prior authorizations first, then claims (claims pending review stay Draft by design).
        foreach (var c in samples.OrderBy(s => s.Use == "preauthorization" ? 0 : 1).ThenBy(s => s.ClaimNumber))
        {
            await Try(result, $"submit {c.ClaimNumber}", async () =>
            {
                if (c.Use != "preauthorization")
                    await AdoptPreAuthRefAsync(db, c.Id, ct);
                await workflow.SubmitAsync(c.Id, ct);
                result.Submitted++;
            });
        }

        // 3) Poll each hospital: queued adjudications, payments (auto-acknowledged), communication requests.
        foreach (var provider in samples.Select(s => s.ProviderId).Distinct())
        {
            await poll.PollOnceAsync(provider, ct);
            result.Polls++;
        }

        // 4) Answer the payer's information requests (solicited communication with an attachment) ...
        var openRequests = await db.CommunicationRequests.AsNoTracking()
            .Where(r => r.ClaimId != null && r.RespondedAt == null)
            .OrderBy(r => r.ReceivedAt).Select(r => new { r.Id, r.ClaimId }).ToListAsync(ct);
        foreach (var r in openRequests.Take(Math.Max(1, openRequests.Count - 1))) // leave one open for the work queue
        {
            await Try(result, $"respond {r.Id}", async () =>
            {
                await communications.RespondAsync(r.Id, new SendCommunicationDto
                {
                    Texts = { "Please find the requested medical report attached." },
                    Attachments = { Pdf("Medical report") },
                }, ct);
                result.Communications++;
            });
        }

        // ... and send one unsolicited communication.
        var adjudicated = await db.Claims.AsNoTracking()
            .Where(c => c.ClaimNumber.StartsWith(prefix) && c.Use == "claim" && c.NphiesStatus == RcmClaimStatus.Approved)
            .OrderBy(c => c.ClaimNumber).Select(c => c.Id).FirstOrDefaultAsync(ct);
        if (adjudicated != null)
        {
            await Try(result, "unsolicited communication", async () =>
            {
                await communications.SendAsync(new SendCommunicationDto
                {
                    ClaimId = adjudicated,
                    Category = "notification",
                    Texts = { "Sharing the final radiology report for your records." },
                    Attachments = { Pdf("Radiology report") },
                }, ct);
                result.Communications++;
            });
        }

        // 5) Appeal up to two claims with open denials (resubmitted with related=prior).
        var denied = await db.ClaimDenials.AsNoTracking()
            .Where(d => d.Status == "Open" && d.Claim!.ClaimNumber.StartsWith(prefix) && d.Claim.Use == "claim")
            .GroupBy(d => d.ClaimId).Select(g => new { ClaimId = g.Key, Ids = g.Select(d => d.Id).ToList() })
            .Take(2).ToListAsync(ct);
        foreach (var d in denied)
        {
            await Try(result, $"appeal {d.ClaimId}", async () =>
            {
                await appeals.CreateAsync(new CreateAppealDto
                {
                    ClaimId = d.ClaimId,
                    DenialIds = d.Ids,
                    Reason = "Medical necessity is documented in the attached clinical notes.",
                    Texts = { "Requesting reconsideration; clinical notes attached." },
                    Attachments = { Pdf("Clinical notes") },
                    Resubmit = true,
                    CreatedBy = SampleDataSeeder.SeedUser,
                }, ct);
                result.Appeals++;
            });
        }

        // 6) Status check one claim, extend one approved prior authorization, cancel one approved claim.
        var approvedPa = await db.Claims.AsNoTracking()
            .Where(c => c.ClaimNumber.StartsWith(prefix) && c.Use == "preauthorization" && c.NphiesStatus == RcmClaimStatus.Approved)
            .OrderBy(c => c.ClaimNumber).Select(c => c.Id).FirstOrDefaultAsync(ct);
        if (approvedPa != null)
            await Try(result, "extend prior authorization", () =>
                workflow.ExtendPreAuthAsync(approvedPa, new DerivedClaimRequestDto { Reason = "Extended stay", NewPeriodEnd = DateTime.UtcNow.Date.AddDays(14) }, ct));
        if (adjudicated != null)
            await Try(result, "status check", () => workflow.StatusCheckAsync(adjudicated, ct));
        var unpaid = await db.Claims.AsNoTracking()
            .Where(c => c.ClaimNumber.StartsWith(prefix) && c.Use == "claim" && c.NphiesStatus == RcmClaimStatus.Approved && c.PaidAt == null)
            .OrderByDescending(c => c.ClaimNumber).Select(c => c.Id).FirstOrDefaultAsync(ct);
        if (unpaid != null)
            await Try(result, "cancel claim", () => workflow.CancelAsync(unpaid, new CancelClaimRequestDto { Reason = "WI" }, ct));

        // 7) Final poll picks up payments for resubmissions.
        foreach (var provider in samples.Select(s => s.ProviderId).Distinct())
        {
            await poll.PollOnceAsync(provider, ct);
            result.Polls++;
        }

        db.ChangeTracker.Clear();
        await BackdateAsync(db, prefix, ct);

        logger.LogInformation("Sample RCM activity: {Elig} eligibility checks, {Submitted} submissions, {Polls} polls, {Comms} communications, {Appeals} appeals, {Notes} notes",
            result.EligibilityChecks, result.Submitted, result.Polls, result.Communications, result.Appeals, result.Notes.Count);
        return result;
    }

    /// <summary>Shifts workflow timestamps from "now" to plausible dates after each claim's service date (never in the future).</summary>
    /// <summary>
    /// Sample claims are generated before their prior authorization is adjudicated, so they carry a placeholder
    /// pre-auth number. Replace it with the approval number the linked (associated) pre-auth actually received.
    /// </summary>
    private static async Task AdoptPreAuthRefAsync(ApplicationDbContext db, string claimId, CancellationToken ct)
    {
        var paId = await db.Set<NPhies_FHIR_Integration.Domain.Entities.ClaimRelated>().Where(r => r.ClaimId == claimId && r.ReferencedClaimId != null)
            .Select(r => r.ReferencedClaimId).FirstOrDefaultAsync(ct);
        if (paId is null) return;
        var paRef = await db.ClaimResponses.Where(r => r.ClaimId == paId && r.PreAuthRef != null)
            .OrderByDescending(r => r.ReceivedAt).Select(r => r.PreAuthRef).FirstOrDefaultAsync(ct);
        if (paRef is null) return;
        var claim = await db.Claims.FirstAsync(c => c.Id == claimId, ct);
        claim.PreAuthorizationRef = paRef;
        await db.SaveChangesAsync(ct);
    }

    private static async Task BackdateAsync(ApplicationDbContext db, string prefix, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        DateTime Cap(DateTime d) => d > now ? now : d;

        var claims = await db.Claims.Where(c => c.ClaimNumber.StartsWith(prefix) && c.SubmittedAt != null).ToListAsync(ct);
        var shifts = new Dictionary<string, (DateTime Submitted, DateTime Adjudicated, DateTime Paid)>();
        foreach (var c in claims)
        {
            var seed = Math.Abs(c.ClaimNumber.GetHashCode() % 1000);
            var service = (c.Use == "preauthorization" ? c.CreatedDate : c.ServicedPeriodEnd ?? c.CreatedDate).Date;
            if (service > now) service = now.Date;
            var submitted = Cap(service.AddDays(1 + seed % 3).AddHours(9 + seed % 8));
            var adjudicated = Cap(submitted.AddDays(c.Use == "preauthorization" ? 0 : 2 + seed % 9).AddHours(2));
            var paid = Cap(adjudicated.AddDays(15 + seed % 25));
            shifts[c.Id] = (submitted, adjudicated, paid);

            c.SubmittedAt = submitted;
            if (c.AdjudicatedAt != null) c.AdjudicatedAt = adjudicated;
            if (c.PaidAt != null) c.PaidAt = paid;
        }

        var ids = shifts.Keys.ToList();
        foreach (var r in await db.ClaimResponses.Where(r => ids.Contains(r.ClaimId)).ToListAsync(ct))
            r.ReceivedAt = shifts[r.ClaimId].Adjudicated;
        foreach (var d in await db.ClaimDenials.Where(d => ids.Contains(d.ClaimId)).ToListAsync(ct))
        {
            var window = d.AppealDeadline - d.DenialDate.Date;
            d.DenialDate = shifts[d.ClaimId].Adjudicated;
            d.AppealDeadline = d.DenialDate.Date + window;
        }
        foreach (var r in await db.CommunicationRequests.Where(r => r.ClaimId != null && ids.Contains(r.ClaimId)).ToListAsync(ct))
        {
            r.ReceivedAt = Cap(shifts[r.ClaimId!].Adjudicated.AddDays(1));
            if (r.RespondedAt != null) r.RespondedAt = Cap(r.ReceivedAt.Value.AddDays(2));
        }
        foreach (var t in await db.NphiesTransactions.Where(t => t.EntityId != null && ids.Contains(t.EntityId) && t.Direction == "outbound").ToListAsync(ct))
        {
            t.SentAt = shifts[t.EntityId!].Submitted;
            if (t.CompletedAt != null) t.CompletedAt = t.SentAt.AddSeconds(3);
        }

        var details = await db.PaymentReconciliationDetails.Where(d => d.MatchedClaimId != null && ids.Contains(d.MatchedClaimId)).ToListAsync(ct);
        foreach (var group in details.GroupBy(d => d.PaymentReconciliationId))
        {
            var paid = group.Max(d => shifts[d.MatchedClaimId!].Paid);
            foreach (var d in group) d.DetailDate = paid.Date;
            var recon = await db.PaymentReconciliations.FirstAsync(p => p.Id == group.Key, ct);
            recon.PaymentDate = paid.Date;
            recon.CreatedDate = paid.Date;
            recon.ReceivedAt = paid;
            recon.PeriodStart = paid.Date.AddDays(-30);
            recon.PeriodEnd = paid.Date;
            if (recon.AcknowledgedAt != null) recon.AcknowledgedAt = paid.AddMinutes(5);
        }
        await db.SaveChangesAsync(ct);
    }

    private static AttachmentDto Pdf(string title) => new()
    {
        ContentType = "application/pdf",
        Title = title,
        Data = Convert.ToBase64String(SamplePdf),
        Creation = DateTime.UtcNow,
    };

    private static async Task Try(RcmSampleActivityResult result, string step, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (RcmWorkflowException ex)
        {
            result.Notes.Add($"{step}: {ex.Message}");
        }
    }
}
