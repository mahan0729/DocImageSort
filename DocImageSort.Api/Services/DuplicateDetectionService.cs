using DocImageSort.Api.Data;
using DocImageSort.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace DocImageSort.Api.Services;

/// <summary>
/// Detects duplicate documents by comparing SHA-256 file hashes against existing records.
/// </summary>
public class DuplicateDetectionService : IDuplicateDetectionService
{
    private readonly AppDbContext _db;

    public DuplicateDetectionService(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task<string> ComputeHashAsync(string filePath, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(filePath);
        var bytes = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <inheritdoc/>
    public async Task<bool> IsDuplicateAsync(string fileHash, int excludeDocumentId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(fileHash)) return false;

        return await _db.Documents
            .Where(d => d.Id != excludeDocumentId && d.FileHash == fileHash)
            .AnyAsync(ct);
    }
}
