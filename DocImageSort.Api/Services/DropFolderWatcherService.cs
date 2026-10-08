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

    /// <summary>
    /// Starts the <see cref="FileSystemWatcher"/> on the configured drop folder.
    /// Creates the folder if it does not exist. Returns immediately; file events are handled asynchronously.
    /// </summary>
    /// <param name="stoppingToken">Triggered when the host is shutting down.</param>
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

    /// <summary>
    /// Invoked when a new file appears in the drop folder.
    /// Waits 500 ms for the write to complete, then delegates to <see cref="IDocumentPipelineService"/>.
    /// </summary>
    /// <param name="filePath">Absolute path of the newly created file.</param>
    /// <param name="cancellationToken">Cancellation token from the host.</param>
    private async Task OnFileCreatedAsync(string filePath, CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(filePath);
        if (!SupportedExtensions.Contains(ext))
        {
            _logger.LogWarning("Unsupported file type dropped, skipping: {File}", filePath);
            return;
        }

        await WaitForFileReadyAsync(filePath, cancellationToken);

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

    /// <summary>
    /// Waits until the file can be opened exclusively, retrying up to 10 times with 500 ms gaps.
    /// Handles large files copied over network shares that are still being written.
    /// </summary>
    private async Task WaitForFileReadyAsync(string filePath, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                await using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                return;
            }
            catch (IOException)
            {
                _logger.LogDebug("File not ready yet (attempt {Attempt}): {File}", attempt + 1, Path.GetFileName(filePath));
                await Task.Delay(500, ct);
            }
        }
        _logger.LogWarning("File may still be locked after 10 attempts — proceeding anyway: {File}", Path.GetFileName(filePath));
    }

    public override void Dispose()
    {
        _watcher?.Dispose();
        base.Dispose();
    }
}
