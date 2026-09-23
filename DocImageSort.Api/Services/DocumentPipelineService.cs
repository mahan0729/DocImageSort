using DocImageSort.Api.Data;
using DocImageSort.Api.Models;
using DocImageSort.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using AppLogLevel = DocImageSort.Api.Models.LogLevel;

namespace DocImageSort.Api.Services;

/// <summary>
/// Orchestrates the full document pipeline: ingest → classify → convert → rename → route.
/// </summary>
public class DocumentPipelineService : IDocumentPipelineService
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpg", ".jpeg", ".png" };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentPipelineService> _logger;

    public DocumentPipelineService(
        IServiceScopeFactory scopeFactory,
        ILogger<DocumentPipelineService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task ProcessFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(filePath);
        var fileName = Path.GetFileName(filePath);

        if (!SupportedExtensions.Contains(ext))
        {
            _logger.LogWarning("Skipping unsupported file type: {File}", fileName);
            return;
        }

        _logger.LogInformation("Ingesting file: {File}", fileName);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var document = new Document
            {
                OriginalFileName = fileName,
                SourcePath = filePath,
                FileExtension = ext,
                Status = DocumentStatus.Pending,
                CreatedBy = "system",
                UpdatedBy = "system"
            };

            db.Documents.Add(document);
            await db.SaveChangesAsync(cancellationToken);

            await LogAsync(db, document.Id, fileName, "Ingest", "Success",
                $"File received and queued for classification.", AppLogLevel.Info, cancellationToken);

            _logger.LogInformation("Document {Id} ingested: {File}", document.Id, fileName);

            // TODO: Step 2 — AI classification (item 5)
            // TODO: Step 3 — PDF conversion (item 7)
            // TODO: Step 4 — Auto-rename (item 8)
            // TODO: Step 5 — Auto-route (item 9)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest file: {File}", fileName);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await LogAsync(db, null, fileName, "Ingest", "Error", ex.Message, AppLogLevel.Error, cancellationToken);
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Failed to write error log for: {File}", fileName);
            }
        }
    }

    private static async Task LogAsync(
        AppDbContext db,
        int? documentId,
        string fileName,
        string action,
        string outcome,
        string message,
        AppLogLevel level,
        CancellationToken cancellationToken)
    {
        db.ProcessingLogs.Add(new ProcessingLog
        {
            DocumentId = documentId,
            FileName = fileName,
            Action = action,
            Outcome = outcome,
            Message = message,
            Level = level,
            CreatedBy = "system",
            UpdatedBy = "system"
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
