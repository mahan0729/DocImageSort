using DocImageSort.Api.Models;
using DocImageSort.Api.Services.Interfaces;

namespace DocImageSort.Api.Services;

/// <summary>
/// Routes processed PDFs into borrower folders under the configured FilesFolder.
/// PENDING folder holds documents awaiting borrower assignment.
/// </summary>
public class RoutingService : IRoutingService
{
    private readonly IConfiguration _config;
    private readonly ILogger<RoutingService> _logger;

    public RoutingService(IConfiguration config, ILogger<RoutingService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<string> RouteAsync(Document document, Borrower? borrower = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseFolder = _config["FilesFolder:Path"] ?? @"C:\DocImageSort\Files";

            var subFolder = borrower is not null
                ? Sanitize(borrower.FolderName)
                : "PENDING";

            var destinationFolder = Path.Combine(baseFolder, subFolder);
            Directory.CreateDirectory(destinationFolder);

            var fileName = Path.GetFileName(document.SourcePath);
            var destinationPath = Path.Combine(destinationFolder, fileName);

            // Avoid overwriting — append counter if file already exists.
            if (File.Exists(destinationPath) && destinationPath != document.SourcePath)
            {
                var counter = 1;
                var nameNoExt = Path.GetFileNameWithoutExtension(fileName);
                while (File.Exists(destinationPath))
                {
                    if (counter > 999) throw new InvalidOperationException($"Too many files with the same name in {destinationFolder}.");
                    destinationPath = Path.Combine(destinationFolder, $"{nameNoExt}_{counter++}.pdf");
                }
            }

            if (document.SourcePath != destinationPath && File.Exists(document.SourcePath))
            {
                File.Move(document.SourcePath, destinationPath);
                _logger.LogInformation("Routed {File} → {Folder}", fileName, subFolder);
            }

            return Task.FromResult(destinationPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Routing failed for document {Id}", document.Id);
            throw;
        }
    }

    private static string Sanitize(string input)
    {
        var invalid = Path.GetInvalidPathChars();
        return string.Concat(input.Select(c => invalid.Contains(c) ? '_' : c)).Trim();
    }
}
