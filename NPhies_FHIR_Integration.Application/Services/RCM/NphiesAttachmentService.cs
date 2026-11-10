using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NPhies_FHIR_Integration.Application.Services.RCM
{
    /// <summary>
    /// NPHIES Attachment Management Service
    /// Manages claim attachments and supporting documents
 /// Implements NPHIES attachment requirements
    /// </summary>
    public interface INphiesAttachmentService
    {
        Task<AttachmentResponse> UploadAttachmentAsync(AttachmentUploadDto upload);
        Task<AttachmentDto> GetAttachmentAsync(string attachmentId);
        Task<List<AttachmentDto>> GetClaimAttachmentsAsync(string claimId);
        Task<bool> ValidateAttachmentAsync(AttachmentUploadDto attachment);
        Task<bool> DeleteAttachmentAsync(string attachmentId);
        Task<DocumentReferenceDto> CreateDocumentReferenceAsync(string attachmentId, string claimId);
 Task<AttachmentStatistics> GetAttachmentStatisticsAsync(string providerId, DateTime? fromDate = null);
    }

    /// <summary>
    /// Attachment upload DTO
    /// </summary>
    public class AttachmentUploadDto
    {
        public string ClaimId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // pdf, jpeg, png, tiff
public byte[] FileContent { get; set; }
        public long FileSizeBytes { get; set; }
      public string DocumentType { get; set; } = string.Empty; // EOB, receipt, prescription, etc.
     public string ProviderId { get; set; } = string.Empty;
        public DateTime? DocumentDate { get; set; }
    }

    /// <summary>
    /// Attachment response DTO
    /// </summary>
    public class AttachmentResponse
    {
      public bool Success { get; set; }
        public string AttachmentId { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
      public string Message { get; set; } = string.Empty;
        public string DocumentReferenceId { get; set; } = string.Empty;
}

    /// <summary>
    /// Attachment DTO
    /// </summary>
    public class AttachmentDto
    {
        public string AttachmentId { get; set; } = string.Empty;
        public string ClaimId { get; set; } = string.Empty;
      public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime UploadDate { get; set; }
   public string DocumentType { get; set; } = string.Empty;
   public string ProviderId { get; set; } = string.Empty;
   public DateTime? DocumentDate { get; set; }
        public string Status { get; set; } = "active"; // active, archived, deleted
        public string DocumentReferenceId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Document reference DTO
    /// </summary>
    public class DocumentReferenceDto
    {
        public string DocumentReferenceId { get; set; } = string.Empty;
  public string AttachmentId { get; set; } = string.Empty;
        public string ClaimId { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; } = "current";
        public List<string> RelatedResources { get; set; } = new();
    }

    /// <summary>
    /// Attachment statistics DTO
    /// </summary>
    public class AttachmentStatistics
    {
     public string ProviderId { get; set; } = string.Empty;
        public int TotalAttachments { get; set; }
  public int AttachmentsByType { get; set; }
        public long TotalStorageBytes { get; set; }
        public double AverageFileSizeBytes { get; set; }
      public int ActiveAttachments { get; set; }
        public int ArchivedAttachments { get; set; }
        public DateTime ReportDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
/// NPHIES Attachment Management Service Implementation
    /// </summary>
    public class NphiesAttachmentService : INphiesAttachmentService
    {
        private readonly ILogger<NphiesAttachmentService> _logger;

        // In-memory storage (would use blob storage in production)
        private readonly Dictionary<string, AttachmentDto> _attachmentStore = new();
     private readonly Dictionary<string, DocumentReferenceDto> _documentReferences = new();
    private readonly List<AttachmentDto> _allAttachments = new();

        // NPHIES Allowed File Types
   private static readonly HashSet<string> AllowedFileTypes = new()
      {
            "pdf", "jpeg", "jpg", "png", "tiff", "tif"
        };

        // Maximum file size: 15 MB
        private const long MaxFileSizeBytes = 15 * 1024 * 1024;

    public NphiesAttachmentService(ILogger<NphiesAttachmentService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Uploads an attachment
        /// </summary>
        public async Task<AttachmentResponse> UploadAttachmentAsync(AttachmentUploadDto upload)
        {
    try
      {
  _logger.LogInformation($"Uploading attachment for claim: {upload.ClaimId}");

      if (upload == null)
           {
     return new AttachmentResponse { Success = false, Message = "Upload is required" };
    }

   // Validate attachment
             var validationResult = await ValidateAttachmentAsync(upload);
                if (!validationResult)
      {
      return new AttachmentResponse { Success = false, Message = "Attachment validation failed" };
   }

      // Generate Attachment ID
                var attachmentId = GenerateAttachmentId();
                var documentRefId = GenerateDocumentReferenceId();

      // Create attachment
                var attachment = new AttachmentDto
     {
               AttachmentId = attachmentId,
   ClaimId = upload.ClaimId,
          FileName = upload.FileName,
 FileType = upload.FileType.ToLower(),
           FileSizeBytes = upload.FileSizeBytes,
       UploadDate = DateTime.UtcNow,
 DocumentType = upload.DocumentType,
      ProviderId = upload.ProviderId,
               DocumentDate = upload.DocumentDate ?? DateTime.UtcNow,
      Status = "active",
        DocumentReferenceId = documentRefId
         };

   // Store attachment
  _attachmentStore[attachmentId] = attachment;
   _allAttachments.Add(attachment);

         // Create document reference
         var docRef = new DocumentReferenceDto
         {
          DocumentReferenceId = documentRefId,
      AttachmentId = attachmentId,
         ClaimId = upload.ClaimId,
       DocumentType = upload.DocumentType,
   CreatedDate = DateTime.UtcNow,
Status = "current",
             RelatedResources = new List<string> { upload.ClaimId }
    };

    _documentReferences[documentRefId] = docRef;

           _logger.LogInformation($"Attachment uploaded successfully: {attachmentId}");

         return new AttachmentResponse
           {
        Success = true,
     AttachmentId = attachmentId,
       UploadDate = DateTime.UtcNow,
              Message = "Attachment uploaded successfully",
        DocumentReferenceId = documentRefId
  };
    }
       catch (Exception ex)
     {
_logger.LogError(ex, "Error uploading attachment");
   return new AttachmentResponse { Success = false, Message = $"Error: {ex.Message}" };
 }
 }

        /// <summary>
        /// Gets an attachment by ID
        /// </summary>
        public async Task<AttachmentDto> GetAttachmentAsync(string attachmentId)
        {
    try
   {
                _logger.LogInformation($"Getting attachment: {attachmentId}");

    if (string.IsNullOrWhiteSpace(attachmentId))
          {
     return null;
     }

      if (_attachmentStore.TryGetValue(attachmentId, out var attachment))
            {
     return attachment;
     }

    _logger.LogWarning($"Attachment not found: {attachmentId}");
                return null;
      }
        catch (Exception ex)
            {
             _logger.LogError(ex, "Error getting attachment");
  return null;
    }
        }

        /// <summary>
        /// Gets all attachments for a claim
  /// </summary>
        public async Task<List<AttachmentDto>> GetClaimAttachmentsAsync(string claimId)
   {
try
            {
            _logger.LogInformation($"Getting attachments for claim: {claimId}");

    var attachments = _allAttachments
    .Where(a => a.ClaimId == claimId && a.Status == "active")
          .OrderByDescending(a => a.UploadDate)
 .ToList();

         return attachments;
          }
       catch (Exception ex)
            {
   _logger.LogError(ex, "Error getting claim attachments");
                return new List<AttachmentDto>();
 }
     }

      /// <summary>
 /// Validates an attachment
   /// </summary>
        public async Task<bool> ValidateAttachmentAsync(AttachmentUploadDto attachment)
        {
            try
            {
      _logger.LogInformation($"Validating attachment: {attachment.FileName}");

             if (attachment == null)
 {
         _logger.LogWarning("Attachment is null");
     return false;
                }

 // Check file name
     if (string.IsNullOrWhiteSpace(attachment.FileName))
    {
   _logger.LogWarning("File name is required");
       return false;
          }

       // Check file type
     if (string.IsNullOrWhiteSpace(attachment.FileType) || !AllowedFileTypes.Contains(attachment.FileType.ToLower()))
 {
            _logger.LogWarning($"Invalid file type: {attachment.FileType}");
        return false;
   }

          // Check file size
     if (attachment.FileSizeBytes <= 0 || attachment.FileSizeBytes > MaxFileSizeBytes)
         {
           _logger.LogWarning($"Invalid file size: {attachment.FileSizeBytes}");
        return false;
           }

       // Check file content
                if (attachment.FileContent == null || attachment.FileContent.Length == 0)
 {
              _logger.LogWarning("File content is empty");
     return false;
   }

       _logger.LogInformation("Attachment validation passed");
   return true;
            }
catch (Exception ex)
         {
      _logger.LogError(ex, "Error validating attachment");
                return false;
            }
 }

        /// <summary>
        /// Deletes an attachment
 /// </summary>
        public async Task<bool> DeleteAttachmentAsync(string attachmentId)
        {
         try
          {
        _logger.LogInformation($"Deleting attachment: {attachmentId}");

     if (_attachmentStore.TryGetValue(attachmentId, out var attachment))
     {
         attachment.Status = "deleted";
        return true;
       }

           return false;
            }
          catch (Exception ex)
            {
          _logger.LogError(ex, "Error deleting attachment");
    return false;
            }
        }

        /// <summary>
   /// Creates document reference for attachment
        /// </summary>
        public async Task<DocumentReferenceDto> CreateDocumentReferenceAsync(string attachmentId, string claimId)
   {
    try
            {
     _logger.LogInformation($"Creating document reference for attachment: {attachmentId}");

            var attachment = await GetAttachmentAsync(attachmentId);
    if (attachment == null)
  {
          return null;
                }

                var documentRefId = GenerateDocumentReferenceId();
            var docRef = new DocumentReferenceDto
            {
      DocumentReferenceId = documentRefId,
                AttachmentId = attachmentId,
  ClaimId = claimId,
          DocumentType = attachment.DocumentType,
          CreatedDate = DateTime.UtcNow,
         Status = "current",
          RelatedResources = new List<string> { claimId }
        };

    _documentReferences[documentRefId] = docRef;
     attachment.DocumentReferenceId = documentRefId;

    return docRef;
      }
    catch (Exception ex)
       {
       _logger.LogError(ex, "Error creating document reference");
           return null;
            }
        }

        /// <summary>
  /// Gets attachment statistics
        /// </summary>
        public async Task<AttachmentStatistics> GetAttachmentStatisticsAsync(string providerId, DateTime? fromDate = null)
        {
        try
  {
       _logger.LogInformation($"Getting attachment statistics for provider: {providerId}");

         var from = fromDate ?? DateTime.UtcNow.AddDays(-30);

    var relevantAttachments = _allAttachments
        .Where(a => a.ProviderId == providerId && a.UploadDate >= from)
        .ToList();

          var stats = new AttachmentStatistics
           {
     ProviderId = providerId,
            TotalAttachments = relevantAttachments.Count,
    TotalStorageBytes = relevantAttachments.Sum(a => a.FileSizeBytes),
  ActiveAttachments = relevantAttachments.Count(a => a.Status == "active"),
          ArchivedAttachments = relevantAttachments.Count(a => a.Status == "archived"),
        ReportDate = DateTime.UtcNow
          };

 // Calculate average file size
   if (stats.TotalAttachments > 0)
       {
  stats.AverageFileSizeBytes = (double)stats.TotalStorageBytes / stats.TotalAttachments;
            stats.AttachmentsByType = relevantAttachments.GroupBy(a => a.FileType).Count();
           }

 return stats;
            }
       catch (Exception ex)
        {
 _logger.LogError(ex, "Error getting attachment statistics");
    return new AttachmentStatistics { ProviderId = providerId };
     }
  }

        // Helper methods

        private string GenerateAttachmentId()
        {
        return $"ATT{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    }

        private string GenerateDocumentReferenceId()
        {
          return $"DOCREF{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
    }
    }
}
