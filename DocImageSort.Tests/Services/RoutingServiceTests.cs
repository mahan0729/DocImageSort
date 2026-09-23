using DocImageSort.Api.Models;
using DocImageSort.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocImageSort.Tests.Services;

public class RoutingServiceTests : IDisposable
{
    private readonly string _filesFolder;
    private readonly RoutingService _sut;

    public RoutingServiceTests()
    {
        _filesFolder = Path.Combine(Path.GetTempPath(), $"DocImageSort_Test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_filesFolder);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FilesFolder:Path"] = _filesFolder
            })
            .Build();

        _sut = new RoutingService(config, NullLogger<RoutingService>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_filesFolder))
            Directory.Delete(_filesFolder, recursive: true);
    }

    [Fact]
    public async Task RouteAsync_WithBorrower_MovesToBorrowerSubfolder()
    {
        var sourceFile = await CreateTempFileInAsync(_filesFolder, "PENDING_Pay Stub_2026-08.pdf");
        var doc = new Document { SourcePath = sourceFile, RenamedFileName = Path.GetFileName(sourceFile) };
        var borrower = new Borrower { LastName = "Smith", FirstName = "John" };

        var result = await _sut.RouteAsync(doc, borrower);

        Assert.True(File.Exists(result));
        Assert.Contains("Smith,John", result);
        Assert.False(File.Exists(sourceFile));
    }

    [Fact]
    public async Task RouteAsync_WithoutBorrower_MovesToPendingSubfolder()
    {
        var sourceFile = await CreateTempFileInAsync(_filesFolder, "PENDING_W-2_2026-01.pdf");
        var doc = new Document { SourcePath = sourceFile, RenamedFileName = Path.GetFileName(sourceFile) };

        var result = await _sut.RouteAsync(doc, borrower: null);

        Assert.True(File.Exists(result));
        Assert.Contains("PENDING", result);
    }

    [Fact]
    public async Task RouteAsync_CreatesDestinationFolderIfMissing()
    {
        var sourceFile = await CreateTempFileInAsync(_filesFolder, "PENDING_1040_2026-04.pdf");
        var doc = new Document { SourcePath = sourceFile, RenamedFileName = Path.GetFileName(sourceFile) };
        var borrower = new Borrower { LastName = "New", FirstName = "Borrower" };

        var result = await _sut.RouteAsync(doc, borrower);

        Assert.True(Directory.Exists(Path.GetDirectoryName(result)));
    }

    [Fact]
    public async Task RouteAsync_DuplicateFileName_AppendsCounter()
    {
        var pendingDir = Path.Combine(_filesFolder, "PENDING");
        Directory.CreateDirectory(pendingDir);

        // Pre-create a file at the destination to force the counter path.
        var conflictFile = Path.Combine(pendingDir, "PENDING_W-2_2026-01.pdf");
        await File.WriteAllTextAsync(conflictFile, "existing");

        var sourceFile = await CreateTempFileInAsync(_filesFolder, "PENDING_W-2_2026-01.pdf");
        var doc = new Document { SourcePath = sourceFile, RenamedFileName = Path.GetFileName(sourceFile) };

        var result = await _sut.RouteAsync(doc, borrower: null);

        Assert.True(File.Exists(result));
        Assert.True(File.Exists(conflictFile));  // Original not overwritten.
        Assert.NotEqual(conflictFile, result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<string> CreateTempFileInAsync(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        await File.WriteAllTextAsync(path, "test pdf content");
        return path;
    }
}
