using DocImageSort.Api.Data;
using DocImageSort.Api.Models;
using DocImageSort.Api.Services.Interfaces;
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
    private readonly IClassificationService _classifier;
    private readonly IConversionService _converter;
    private readonly IRenameService _renamer;
    private readonly IRoutingService _router;
    private readonly IDuplicateDetectionService _duplicateDetector;
    private readonly ILogger<DocumentPipelineService> _logger;

    public DocumentPipelineService(
        IServiceScopeFactory scopeFactory,
        IClassificationService classifier,
        IConversionService converter,
        IRenameService renamer,
        IRoutingService router,
        IDuplicateDetectionService duplicateDetector,
        ILogger<DocumentPipelineService> logger)
    {
        _scopeFactory = scopeFactory;
        _classifier = classifier;
        _converter = converter;
        _renamer = renamer;
        _router = router;
        _duplicateDetector = duplicateDetector;
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

            // Step 1b — Duplicate detection
            var fileHash = await _duplicateDetector.ComputeHashAsync(filePath, cancellationToken);
            document.FileHash = fileHash;

            if (await _duplicateDetector.IsDuplicateAsync(fileHash, document.Id, cancellationToken))
            {
                document.Status = DocumentStatus.Duplicate;
                document.UpdatedBy = "system";
                document.UpdatedDate = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);

                await LogAsync(db, document.Id, fileName, "DuplicateCheck", "Duplicate",
                    $"File hash {fileHash[..8]}… matches an existing document. Skipping pipeline.",
                    AppLogLevel.Warning, cancellationToken);

                _logger.LogWarning("Document {Id} is a duplicate — skipping pipeline.", document.Id);
                return;
            }

            document.UpdatedBy = "system";
            document.UpdatedDate = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            // Step 2 — AI classification
            var classification = await _classifier.ClassifyAsync(filePath, cancellationToken);

            document.DocumentType = classification.DocumentType;
            document.AiClassificationNotes = classification.Notes;
            document.Status = classification.Success ? DocumentStatus.Classified : DocumentStatus.Error;
            if (DateTime.TryParse(classification.DocumentDate, out var parsedDate))
                document.DocumentDate = parsedDate;
            document.UpdatedBy = "system";
            document.UpdatedDate = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);

            await LogAsync(db, document.Id, fileName, "Classify", classification.Success ? "Success" : "Error",
                $"{classification.DocumentType} — {classification.Notes}", AppLogLevel.Info, cancellationToken);

            _logger.LogInformation("Document {Id} classified as: {Type}", document.Id, classification.DocumentType);

            // Step 3 — PDF conversion (images only)
            var pdfPath = await _converter.ConvertToPdfAsync(filePath, cancellationToken);
            if (pdfPath != filePath)
            {
                document.FileExtension = ".pdf";
                document.SourcePath = pdfPath;
                document.Status = DocumentStatus.Converted;
                document.UpdatedBy = "system";
                document.UpdatedDate = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);

                await LogAsync(db, document.Id, fileName, "Convert", "Success",
                    $"Converted to PDF: {Path.GetFileName(pdfPath)}", AppLogLevel.Info, cancellationToken);
            }

            // Step 4 — Auto-rename (PENDING prefix until borrower assigned in review UI)
            var renamedPath = await _renamer.RenameFileAsync(document, borrower: null, cancellationToken);
            document.RenamedFileName = Path.GetFileName(renamedPath);
            document.SourcePath = renamedPath;
            document.UpdatedBy = "system";
            document.UpdatedDate = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            await LogAsync(db, document.Id, fileName, "Rename", "Success",
                $"Renamed to: {document.RenamedFileName}", AppLogLevel.Info, cancellationToken);

            // Step 5 — Auto-route to PENDING folder (final route after borrower assigned in review UI)
            var filedPath = await _router.RouteAsync(document, borrower: null, cancellationToken);
            document.FiledPath = filedPath;
            document.SourcePath = filedPath;
            document.Status = DocumentStatus.Filed;
            document.UpdatedBy = "system";
            document.UpdatedDate = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            await LogAsync(db, document.Id, fileName, "Route", "Success",
                $"Filed to: {filedPath}", AppLogLevel.Info, cancellationToken);

            _logger.LogInformation("Document {Id} pipeline complete. Filed: {Path}", document.Id, filedPath);
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
