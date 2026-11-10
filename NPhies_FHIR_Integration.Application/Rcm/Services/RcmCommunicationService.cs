using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Application.Nphies.Builders;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Nphies.Models;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Rcm.Services;

public interface IRcmCommunicationService
{
    Task<RcmPagedResult<CommunicationRequestDto>> ListRequestsAsync(RcmQuery query, bool openOnly, CancellationToken ct = default);
    Task<CommunicationRequestDto?> GetRequestAsync(string id, CancellationToken ct = default);
    Task<RcmPagedResult<CommunicationDto>> ListAsync(RcmQuery query, string? claimId, CancellationToken ct = default);

    /// <summary>Solicited communication answering an insurer CommunicationRequest (basedOn).</summary>
    Task<CommunicationDto> RespondAsync(string communicationRequestId, SendCommunicationDto request, CancellationToken ct = default);

    /// <summary>Unsolicited communication about a claim / prior authorization.</summary>
    Task<CommunicationDto> SendAsync(SendCommunicationDto request, CancellationToken ct = default);
}

public sealed class RcmCommunicationService : IRcmCommunicationService
{
    private static readonly HashSet<string> Categories = new(StringComparer.Ordinal) { "alert", "notification", "reminder", "instruction" };
    private const long MaxAttachmentBytes = 5 * 1024 * 1024;

    private readonly ApplicationDbContext _db;
    private readonly IRcmInputMapper _mapper;
    private readonly INphiesBundleBuilder _builder;
    private readonly IRcmMessageDispatcher _dispatcher;

    public RcmCommunicationService(ApplicationDbContext db, IRcmInputMapper mapper, INphiesBundleBuilder builder, IRcmMessageDispatcher dispatcher)
    {
        _db = db;
        _mapper = mapper;
        _builder = builder;
        _dispatcher = dispatcher;
    }

    public async Task<RcmPagedResult<CommunicationRequestDto>> ListRequestsAsync(RcmQuery query, bool openOnly, CancellationToken ct = default)
    {
        var q = _db.CommunicationRequests.AsNoTracking().AsQueryable();
        if (openOnly) q = q.Where(r => r.RespondedCommunicationId == null && r.Status == "active");
        if (!string.IsNullOrWhiteSpace(query.ProviderId)) q = q.Where(r => r.RecipientId == query.ProviderId);
        if (!string.IsNullOrWhiteSpace(query.InsurerId)) q = q.Where(r => r.SenderId == query.InsurerId);
        if (!string.IsNullOrWhiteSpace(query.PatientId)) q = q.Where(r => r.SubjectPatientId == query.PatientId);
        if (!string.IsNullOrWhiteSpace(query.Status)) q = q.Where(r => r.Status == query.Status);
        if (query.From.HasValue) q = q.Where(r => r.CreatedAt >= query.From);
        if (query.To.HasValue) q = q.Where(r => r.CreatedAt <= query.To);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(r => r.ReceivedAt ?? r.CreatedAt).Skip(query.Skip).Take(query.Take).ToListAsync(ct);
        return new RcmPagedResult<CommunicationRequestDto> { Total = total, Page = Math.Max(1, query.Page), PageSize = query.Take, Items = items.Select(RcmProjections.ToDto).ToList() };
    }

    public async Task<CommunicationRequestDto?> GetRequestAsync(string id, CancellationToken ct = default)
    {
        var r = await _db.CommunicationRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return r is null ? null : RcmProjections.ToDto(r);
    }

    public async Task<RcmPagedResult<CommunicationDto>> ListAsync(RcmQuery query, string? claimId, CancellationToken ct = default)
    {
        var q = _db.Communications.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(claimId)) q = q.Where(c => c.ClaimId == claimId);
        if (!string.IsNullOrWhiteSpace(query.ProviderId)) q = q.Where(c => c.SenderId == query.ProviderId || c.RecipientId == query.ProviderId);
        if (!string.IsNullOrWhiteSpace(query.PatientId)) q = q.Where(c => c.SubjectPatientId == query.PatientId);
        if (!string.IsNullOrWhiteSpace(query.Status)) q = q.Where(c => c.ProcessingStatus == query.Status);
        if (query.From.HasValue) q = q.Where(c => c.CreatedAt >= query.From);
        if (query.To.HasValue) q = q.Where(c => c.CreatedAt <= query.To);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(c => c.CreatedAt).Skip(query.Skip).Take(query.Take).ToListAsync(ct);
        return new RcmPagedResult<CommunicationDto> { Total = total, Page = Math.Max(1, query.Page), PageSize = query.Take, Items = items.Select(RcmProjections.ToDto).ToList() };
    }

    public async Task<CommunicationDto> RespondAsync(string communicationRequestId, SendCommunicationDto request, CancellationToken ct = default)
    {
        var cr = await _db.CommunicationRequests.FirstOrDefaultAsync(r => r.Id == communicationRequestId, ct)
            ?? throw RcmWorkflowException.NotFound("CommunicationRequest", communicationRequestId);
        if (cr.RespondedCommunicationId != null)
        {
            var previous = await _db.Communications.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cr.RespondedCommunicationId, ct);
            if (previous?.ProcessingStatus == RcmTransactionStatus.Completed)
                throw RcmWorkflowException.Conflict("This communication request has already been answered.");
        }
        var claimId = cr.ClaimId ?? request.ClaimId;
        if (string.IsNullOrWhiteSpace(claimId))
            throw RcmWorkflowException.Invalid("The communication request is not linked to a claim; pass claimId.");
        if (string.IsNullOrWhiteSpace(cr.IdentifierValue))
            throw RcmWorkflowException.Invalid("The communication request has no identifier to reference.");

        var dto = await SendCoreAsync(claimId!, request, cr, ct);
        if (dto.ProcessingStatus is RcmTransactionStatus.Completed or RcmTransactionStatus.Queued)
        {
            cr.RespondedCommunicationId = dto.Id;
            cr.RespondedAt = DateTime.UtcNow;
            cr.Status = "completed";
            cr.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        return dto;
    }

    public Task<CommunicationDto> SendAsync(SendCommunicationDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ClaimId)) throw RcmWorkflowException.Invalid("claimId is required.");
        return SendCoreAsync(request.ClaimId!, request, null, ct);
    }

    private async Task<CommunicationDto> SendCoreAsync(string claimId, SendCommunicationDto request, CommunicationRequest? basedOn, CancellationToken ct)
    {
        var texts = request.Texts.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        if (texts.Count == 0 && request.Attachments.Count == 0)
            throw RcmWorkflowException.Invalid("A communication needs at least one text or attachment payload.");
        var errors = new List<string>();
        foreach (var a in request.Attachments)
        {
            if (string.IsNullOrWhiteSpace(a.Data) && string.IsNullOrWhiteSpace(a.Url)) errors.Add($"Attachment '{a.Title}': data or url is required.");
            if (!string.IsNullOrWhiteSpace(a.Data))
            {
                try { if (Convert.FromBase64String(a.Data).LongLength > MaxAttachmentBytes) errors.Add($"Attachment '{a.Title}' exceeds 5 MB."); }
                catch (FormatException) { errors.Add($"Attachment '{a.Title}': data must be base64."); }
            }
        }
        var category = (request.Category ?? basedOn?.Category ?? "instruction").Trim().ToLowerInvariant();
        if (!Categories.Contains(category)) errors.Add($"Unsupported category '{category}'.");
        if (errors.Count > 0) throw RcmWorkflowException.Invalid("Invalid communication.", errors);

        var claim = await _db.Claims.Include(c => c.Patient).Include(c => c.Provider).Include(c => c.Insurer).FirstOrDefaultAsync(c => c.Id == claimId, ct)
            ?? throw RcmWorkflowException.NotFound("Claim", claimId);
        if (claim.SubmittedAt is null || string.IsNullOrWhiteSpace(claim.ClaimIdentifierValue))
            throw RcmWorkflowException.Conflict("Communications can only reference a claim that was sent to NPHIES.");
        if (claim.Patient is null || claim.Provider is null || claim.Insurer is null)
            throw RcmWorkflowException.Invalid("Claim patient, provider or insurer is missing.");

        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
        var entity = new Communication
        {
            CommunicationId = Guid.NewGuid().ToString("N"),
            IdentifierSystem = _mapper.IdentifierSystem("communication"),
            IdentifierValue = $"Comm_{stamp}",
            Direction = "outbound",
            Status = "completed",
            Category = category,
            CategorySystem = NphiesCodeSystems.CommunicationCategory,
            Priority = request.Priority,
            SubjectPatientId = claim.PatientId,
            AboutResourceType = "Claim",
            AboutIdentifierSystem = claim.ClaimIdentifierSystem,
            AboutIdentifierValue = claim.ClaimIdentifierValue,
            BasedOnResourceType = basedOn is null ? null : "CommunicationRequest",
            BasedOnIdentifierSystem = basedOn?.IdentifierSystem,
            BasedOnIdentifierValue = basedOn?.IdentifierValue,
            SenderId = claim.ProviderId,
            RecipientId = claim.InsurerId,
            ClaimId = claim.Id,
            CommunicationRequestId = basedOn?.Id,
            PayloadContent = texts.FirstOrDefault(),
            PayloadAttachmentContentType = request.Attachments.FirstOrDefault()?.ContentType,
            PayloadAttachmentTitle = request.Attachments.FirstOrDefault()?.Title,
            PayloadsJson = JsonSerializer.Serialize(new
            {
                texts,
                attachments = request.Attachments.Select(a => new { a.ContentType, a.Title, a.Url, a.Creation, size = a.Data is null ? 0 : (a.Data.Length * 3 / 4) }),
            }),
            ProcessingStatus = RcmTransactionStatus.Pending,
        };
        _db.Communications.Add(entity);
        await _db.SaveChangesAsync(ct);

        var input = new CommunicationInput
        {
            CommunicationId = RcmInputMapper.SafeId(entity.Id),
            Identifier = new NphiesIdentifier(entity.IdentifierSystem!, entity.IdentifierValue!),
            BasedOnCommunicationRequest = basedOn is null ? null : new NphiesIdentifier(basedOn.IdentifierSystem ?? _mapper.IdentifierSystem("communicationrequest"), basedOn.IdentifierValue!),
            AboutClaim = new NphiesIdentifier(claim.ClaimIdentifierSystem ?? _mapper.IdentifierSystem("claim"), claim.ClaimIdentifierValue!),
            Category = category,
            Priority = request.Priority ?? "routine",
            Reason = string.IsNullOrWhiteSpace(request.ReasonCode) ? null : new NphiesCoding(NphiesCodeSystems.CommunicationReason, request.ReasonCode!),
            Patient = _mapper.MapPatient(claim.Patient),
            Provider = _mapper.MapOrganization(claim.Provider, NphiesOrganizationKind.Provider),
            Insurer = _mapper.MapOrganization(claim.Insurer, NphiesOrganizationKind.Insurer),
            Payloads = texts.Select(t => new CommunicationPayloadInput { ContentString = t })
                .Concat(request.Attachments.Select(a => new CommunicationPayloadInput
                {
                    ContentAttachment = new AttachmentInput
                    {
                        ContentType = string.IsNullOrWhiteSpace(a.ContentType) ? "application/pdf" : a.ContentType,
                        Title = a.Title,
                        Creation = (a.Creation ?? DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        Data = a.Data,
                        Url = a.Url,
                    },
                })).ToList(),
        };

        Hl7.Fhir.Model.Bundle bundle;
        try { bundle = _builder.BuildCommunication(input); }
        catch (ArgumentException ex)
        {
            entity.ProcessingStatus = RcmTransactionStatus.ValidationFailed;
            await _db.SaveChangesAsync(ct);
            throw RcmWorkflowException.Invalid(ex.Message);
        }

        var dispatch = await _dispatcher.DispatchAsync(new RcmDispatchRequest
        {
            Message = bundle,
            Operation = RcmOperations.Communication,
            EntityType = RcmEntityTypes.Communication,
            EntityId = entity.Id,
            RequestIdentifierSystem = input.Identifier.System,
            RequestIdentifierValue = input.Identifier.Value,
            ProviderId = claim.ProviderId,
        }, ct);

        entity.ProcessingStatus = dispatch.Status;
        entity.MessageHeaderId = dispatch.Transaction.MessageId;
        if (dispatch.Sent) entity.SentAt = DateTime.UtcNow;
        entity.ProcessedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var dto = RcmProjections.ToDto(entity);
        dto.TransactionStatus = dispatch.Status;
        dto.Errors = dispatch.Errors;
        return dto;
    }
}
