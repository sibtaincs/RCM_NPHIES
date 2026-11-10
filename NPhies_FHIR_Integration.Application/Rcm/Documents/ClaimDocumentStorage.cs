using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Rcm.Documents;

/// <summary>Claim document storage settings (section <c>Rcm:Documents</c>).</summary>
public sealed class RcmDocumentOptions
{
    public const string SectionName = "Rcm:Documents";
    public const string SqlStore = "Sql";
    public const string FileSystemStore = "FileSystem";

    /// <summary>Where new document content is written: <c>Sql</c> (table ClaimDocumentContents) or <c>FileSystem</c>.</summary>
    public string Store { get; set; } = SqlStore;

    /// <summary>Root folder (or UNC share) for the FileSystem store.</summary>
    public string? RootPath { get; set; }

    /// <summary>Largest accepted upload.</summary>
    public long MaxUploadBytes { get; set; } = 25L * 1024 * 1024;

    /// <summary>NPHIES rejects messages larger than this (10 MB).</summary>
    public long MaxMessageBytes { get; set; } = 10L * 1024 * 1024;

    /// <summary>Claim health warns once the estimated message passes this size.</summary>
    public long WarnMessageBytes { get; set; } = 8L * 1024 * 1024;

    /// <summary>Allowance for the non-attachment part of a claim bundle when estimating its size.</summary>
    public long BundleOverheadBytes { get; set; } = 64 * 1024;

    public string[] AllowedContentTypes { get; set; } = { "application/pdf", "image/jpeg", "image/png" };

    /// <summary>Path to the Ghostscript executable (gswin64c.exe / gs). Empty disables PDF compression.</summary>
    public string? GhostscriptPath { get; set; }

    /// <summary>Ghostscript -dPDFSETTINGS preset (/ebook = 150 dpi, /screen = 72 dpi).</summary>
    public string GhostscriptPreset { get; set; } = "/ebook";

    /// <summary>Only try to compress PDFs larger than this.</summary>
    public long CompressAboveBytes { get; set; } = 512 * 1024;
}

/// <summary>Holds document binaries. Writes go to the configured store; reads use the provider recorded on the document.</summary>
public interface IClaimDocumentStorage
{
    string DefaultProvider { get; }

    /// <summary>Stores <paramref name="data"/> under <paramref name="key"/>. SQL content is committed with the caller's SaveChanges.</summary>
    Task<string> WriteAsync(string key, byte[] data, CancellationToken ct = default);

    Task<byte[]?> ReadAsync(string provider, string key, CancellationToken ct = default);

    /// <summary>Removes content (SQL removal is committed with the caller's SaveChanges).</summary>
    Task DeleteAsync(string provider, string key, CancellationToken ct = default);
}

public sealed class ClaimDocumentStorage : IClaimDocumentStorage
{
    private readonly ApplicationDbContext _db;
    private readonly RcmDocumentOptions _options;

    public ClaimDocumentStorage(ApplicationDbContext db, IOptions<RcmDocumentOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public string DefaultProvider =>
        string.Equals(_options.Store, RcmDocumentOptions.FileSystemStore, StringComparison.OrdinalIgnoreCase)
            ? RcmDocumentOptions.FileSystemStore : RcmDocumentOptions.SqlStore;

    public async Task<string> WriteAsync(string key, byte[] data, CancellationToken ct = default)
    {
        var provider = DefaultProvider;
        if (provider == RcmDocumentOptions.FileSystemStore)
        {
            var path = FilePath(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, data, ct);
        }
        else
        {
            _db.ClaimDocumentContents.Add(new ClaimDocumentContent { Key = key, Data = data });
        }
        return provider;
    }

    public async Task<byte[]?> ReadAsync(string provider, string key, CancellationToken ct = default)
    {
        if (string.Equals(provider, RcmDocumentOptions.FileSystemStore, StringComparison.OrdinalIgnoreCase))
        {
            var path = FilePath(key);
            return File.Exists(path) ? await File.ReadAllBytesAsync(path, ct) : null;
        }
        var local = _db.ClaimDocumentContents.Local.FirstOrDefault(c => c.Key == key);
        if (local is not null) return local.Data;
        return await _db.ClaimDocumentContents.AsNoTracking().Where(c => c.Key == key).Select(c => c.Data).FirstOrDefaultAsync(ct);
    }

    public async Task DeleteAsync(string provider, string key, CancellationToken ct = default)
    {
        if (string.Equals(provider, RcmDocumentOptions.FileSystemStore, StringComparison.OrdinalIgnoreCase))
        {
            var path = FilePath(key);
            if (File.Exists(path)) File.Delete(path);
            return;
        }
        var row = await _db.ClaimDocumentContents.FirstOrDefaultAsync(c => c.Key == key, ct);
        if (row is not null) _db.ClaimDocumentContents.Remove(row);
    }

    private string FilePath(string key)
    {
        if (string.IsNullOrWhiteSpace(_options.RootPath))
            throw new InvalidOperationException("Rcm:Documents:RootPath must be set when the FileSystem document store is used.");
        var root = Path.GetFullPath(_options.RootPath);
        var full = Path.GetFullPath(Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid document storage key.");
        return full;
    }

    public static string Sha256Hex(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

    /// <summary>Bytes the content occupies once base64 encoded in a FHIR attachment.</summary>
    public static long EncodedSize(long rawBytes) => (rawBytes + 2) / 3 * 4;
}

/// <summary>Shrinks PDFs before they are sent to NPHIES. Returns null when compression is unavailable or did not help.</summary>
public interface IPdfCompressor
{
    Task<byte[]?> CompressAsync(byte[] pdf, CancellationToken ct = default);
}

/// <summary>Compresses PDFs with Ghostscript (pdfwrite) when <see cref="RcmDocumentOptions.GhostscriptPath"/> is configured.</summary>
public sealed class GhostscriptPdfCompressor : IPdfCompressor
{
    private readonly RcmDocumentOptions _options;
    private readonly ILogger<GhostscriptPdfCompressor> _logger;

    public GhostscriptPdfCompressor(IOptions<RcmDocumentOptions> options, ILogger<GhostscriptPdfCompressor> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<byte[]?> CompressAsync(byte[] pdf, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.GhostscriptPath) || pdf.LongLength < _options.CompressAboveBytes) return null;

        var dir = Path.Combine(Path.GetTempPath(), "rcm-gs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var input = Path.Combine(dir, "in.pdf");
        var output = Path.Combine(dir, "out.pdf");
        try
        {
            await File.WriteAllBytesAsync(input, pdf, ct);
            var psi = new ProcessStartInfo(_options.GhostscriptPath!)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            foreach (var arg in new[] { "-sDEVICE=pdfwrite", "-dCompatibilityLevel=1.5", $"-dPDFSETTINGS={_options.GhostscriptPreset}",
                         "-dNOPAUSE", "-dQUIET", "-dBATCH", "-dSAFER", $"-sOutputFile={output}", input })
                psi.ArgumentList.Add(arg);

            using var process = Process.Start(psi)!;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(120));
            var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(true); } catch { /* already exited */ }
                throw;
            }
            await Task.WhenAll(stdout, stderr);
            if (process.ExitCode != 0 || !File.Exists(output))
            {
                _logger.LogWarning("Ghostscript failed ({ExitCode}): {Error}", process.ExitCode, stderr.Result);
                return null;
            }
            var result = await File.ReadAllBytesAsync(output, ct);
            return result.LongLength > 0 && result.LongLength < pdf.LongLength ? result : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "PDF compression skipped");
            return null;
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { /* best effort */ }
        }
    }
}
