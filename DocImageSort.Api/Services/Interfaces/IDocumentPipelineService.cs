namespace DocImageSort.Api.Services.Interfaces;

public interface IDocumentPipelineService
{
    Task ProcessFileAsync(string filePath, CancellationToken cancellationToken = default);
}
