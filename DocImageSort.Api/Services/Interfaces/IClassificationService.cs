namespace DocImageSort.Api.Services.Interfaces;

public record ClassificationResult(
    string DocumentType,
    string? DocumentDate,
    string Notes,
    bool Success
);

public interface IClassificationService
{
    Task<ClassificationResult> ClassifyAsync(string filePath, CancellationToken cancellationToken = default);
}
