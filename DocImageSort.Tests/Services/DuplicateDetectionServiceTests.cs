using DocImageSort.Api.Data;
using DocImageSort.Api.Models;
using DocImageSort.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DocImageSort.Tests.Services;

public class DuplicateDetectionServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly DuplicateDetectionService _sut;

    public DuplicateDetectionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new DuplicateDetectionService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── ComputeHashAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task ComputeHashAsync_SameContent_ReturnsSameHash()
    {
        var file1 = await TempFileAsync("identical content");
        var file2 = await TempFileAsync("identical content");

        try
        {
            var hash1 = await _sut.ComputeHashAsync(file1);
            var hash2 = await _sut.ComputeHashAsync(file2);

            Assert.Equal(hash1, hash2);
        }
        finally { File.Delete(file1); File.Delete(file2); }
    }

    [Fact]
    public async Task ComputeHashAsync_DifferentContent_ReturnsDifferentHash()
    {
        var file1 = await TempFileAsync("content A");
        var file2 = await TempFileAsync("content B");

        try
        {
            var hash1 = await _sut.ComputeHashAsync(file1);
            var hash2 = await _sut.ComputeHashAsync(file2);

            Assert.NotEqual(hash1, hash2);
        }
        finally { File.Delete(file1); File.Delete(file2); }
    }

    [Fact]
    public async Task ComputeHashAsync_ReturnsLowercaseHex()
    {
        var file = await TempFileAsync("test");
        try
        {
            var hash = await _sut.ComputeHashAsync(file);

            Assert.Equal(64, hash.Length); // SHA-256 = 32 bytes = 64 hex chars
            Assert.Equal(hash, hash.ToLowerInvariant());
        }
        finally { File.Delete(file); }
    }

    // ── IsDuplicateAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task IsDuplicateAsync_NoMatchingHash_ReturnsFalse()
    {
        var result = await _sut.IsDuplicateAsync("abc123", excludeDocumentId: 99);

        Assert.False(result);
    }

    [Fact]
    public async Task IsDuplicateAsync_MatchingHashOnDifferentDocument_ReturnsTrue()
    {
        _db.Documents.Add(new Document
        {
            FileHash = "deadbeef",
            OriginalFileName = "existing.pdf",
            CreatedBy = "test",
            UpdatedBy = "test"
        });
        await _db.SaveChangesAsync();

        var result = await _sut.IsDuplicateAsync("deadbeef", excludeDocumentId: 999);

        Assert.True(result);
    }

    [Fact]
    public async Task IsDuplicateAsync_MatchingHashOnSameDocument_ReturnsFalse()
    {
        var doc = new Document
        {
            FileHash = "deadbeef",
            OriginalFileName = "same.pdf",
            CreatedBy = "test",
            UpdatedBy = "test"
        };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();

        // Exclude the document itself — should not count as its own duplicate.
        var result = await _sut.IsDuplicateAsync("deadbeef", excludeDocumentId: doc.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task IsDuplicateAsync_EmptyHash_ReturnsFalse()
    {
        var result = await _sut.IsDuplicateAsync(string.Empty, excludeDocumentId: 1);

        Assert.False(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<string> TempFileAsync(string content)
    {
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, content);
        return path;
    }
}
