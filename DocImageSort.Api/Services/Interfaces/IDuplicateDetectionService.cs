namespace DocImageSort.Api.Services.Interfaces;

public interface IDuplicateDetectionService
{
    /// <summary>Computes the SHA-256 hash of the file at <paramref name="filePath"/>.</summary>
    Task<string> ComputeHashAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Returns true if another document with the same hash already exists in the database.
    /// The <paramref name="excludeDocumentId"/> is the current document being ingested (exclude it from the check).
    /// </summary>
    Task<bool> IsDuplicateAsync(string fileHash, int excludeDocumentId, CancellationToken ct = default);
}
