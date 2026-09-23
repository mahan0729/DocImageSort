using DocImageSort.Api.Services.Interfaces;

namespace DocImageSort.Api.Services;

/// <summary>
/// Background service that monitors the drop folder for incoming documents.
/// Supported types: PDF, JPG, JPEG, PNG.
/// </summary>
public class DropFolderWatcherService : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DropFolderWatcherService> _logger;
    private FileSystemWatcher? _watcher;

    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpg", ".jpeg", ".png" };

    public DropFolderWatcherService(
        IConfiguration config,
        IServiceScopeFactory scopeFactory,
        ILogger<DropFolderWatcherService> logger)
    {
        _config = config;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var dropPath = _config["DropFolder:Path"] ?? "C:\\DocImageSort\\Drop";

        try
        {
            Directory.CreateDirectory(dropPath);

            _watcher = new FileSystemWatcher(dropPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            _watcher.Created += async (_, e) => await OnFileCreatedAsync(e.FullPath, stoppingToken);

            _logger.LogInformation("Drop folder watcher started: {Path}", dropPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start drop folder watcher for path: {Path}", dropPath);
        }

        return Task.CompletedTask;
    }

    private async Task OnFileCreatedAsync(string filePath, CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(filePath);
        if (!SupportedExtensions.Contains(ext))
        {
            _logger.LogWarning("Unsupported file type dropped, skipping: {File}", filePath);
            return;
        }

        // Wait briefly for the file write to complete before processing.
        await Task.Delay(500, cancellationToken);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<IDocumentPipelineService>();
            await pipeline.ProcessFileAsync(filePath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing dropped file: {File}", filePath);
        }
    }

    public override void Dispose()
    {
        _watcher?.Dispose();
        base.Dispose();
    }
}
