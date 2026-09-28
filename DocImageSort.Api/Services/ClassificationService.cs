using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using DocImageSort.Api.Services.Interfaces;

namespace DocImageSort.Api.Services;

/// <summary>
/// Uses Claude to identify document type from file content (PDF, JPG, PNG).
/// </summary>
public class ClassificationService : IClassificationService
{
    private static readonly string ClassificationPrompt = """
        You are a mortgage document classifier. Examine this document and respond with JSON only.

        Identify the document type from this list:
        Pay Stub, Bank Statement, W2, Tax Return (1040), 1003 Loan Application,
        Insurance Declaration, Title Report, Appraisal, Purchase Contract,
        Credit Report, VOE (Verification of Employment), VOD (Verification of Deposits),
        Driver License, Social Security Card, Gift Letter, HOA Documents,
        Flood Certification, Unknown

        Respond with this exact JSON structure:
        {
          "documentType": "<type from list above>",
          "documentDate": "<YYYY-MM-DD if exact day found, YYYY-MM if only month found, YYYY if only year found, null if not found>",
          "notes": "<brief description of what you see, max 100 chars>",
          "subjectName": "<W2 only: employee full name as printed on the form; null for all other document types>",
          "accountType": "<Bank Statement only: account type such as Checking, Savings, Money Market, etc.; null for all other document types>",
          "institutionName": "<Bank Statement only: bank or institution name such as Chase, Wells Fargo, etc.; null for all other document types>"
        }
        """;

    private readonly IConfiguration _config;
    private readonly ILogger<ClassificationService> _logger;

    public ClassificationService(IConfiguration config, ILogger<ClassificationService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ClassificationResult> ClassifyAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = _config["Anthropic:ApiKey"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Anthropic API key not configured — skipping classification.");
                return new ClassificationResult("Unknown", null, "API key not configured.", false);
            }

            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
            var base64 = Convert.ToBase64String(fileBytes);

            ContentBase fileContent;

            if (ext == ".pdf")
            {
                fileContent = new DocumentContent
                {
                    Source = new DocumentSource
                    {
                        Type = SourceType.base64,
                        Data = base64,
                        MediaType = "application/pdf"
                    }
                };
            }
            else
            {
                var mediaType = ext switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    _ => null
                };

                if (mediaType is null)
                    return new ClassificationResult("Unknown", null, "Unsupported file type.", false);

                fileContent = new ImageContent
                {
                    Source = new ImageSource
                    {
                        MediaType = mediaType,
                        Data = base64
                    }
                };
            }

            var client = new AnthropicClient(apiKey);

            var request = new MessageParameters
            {
                Model = AnthropicModels.Claude46Sonnet,
                MaxTokens = 512,
                Messages = new List<Message>
                {
                    new()
                    {
                        Role = RoleType.User,
                        Content = new List<ContentBase>
                        {
                            fileContent,
                            new TextContent { Text = ClassificationPrompt }
                        }
                    }
                }
            };

            var response = await client.Messages.GetClaudeMessageAsync(request, cancellationToken);
            var raw = response.Content.OfType<TextContent>().FirstOrDefault()?.Text ?? "{}";

            return ParseResponse(raw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Classification failed for: {File}", filePath);
            return new ClassificationResult("Unknown", null, $"Classification error: {ex.Message}", false);
        }
    }

    private static ClassificationResult ParseResponse(string json)
    {
        try
        {
            var clean = json.Trim();

            // Strip markdown code fences if Claude wraps the JSON.
            if (clean.StartsWith("```"))
            {
                var start = clean.IndexOf('{');
                var end = clean.LastIndexOf('}');
                if (start >= 0 && end > start)
                    clean = clean.Substring(start, end - start + 1);
            }

            using var doc = System.Text.Json.JsonDocument.Parse(clean);
            var root = doc.RootElement;

            var docType         = root.TryGetProperty("documentType",   out var dt)  ? dt.GetString()  ?? "Unknown" : "Unknown";
            var docDate         = root.TryGetProperty("documentDate",   out var dd)  ? dd.GetString()              : null;
            var notes           = root.TryGetProperty("notes",          out var n)   ? n.GetString()   ?? ""       : "";
            var subjectName     = root.TryGetProperty("subjectName",    out var sn)  ? sn.GetString()              : null;
            var accountType     = root.TryGetProperty("accountType",    out var at)  ? at.GetString()              : null;
            var institutionName = root.TryGetProperty("institutionName", out var ins) ? ins.GetString()             : null;

            return new ClassificationResult(docType, docDate, notes, true, subjectName, accountType, institutionName);
        }
        catch
        {
            return new ClassificationResult("Unknown", null, "Could not parse classification response.", false);
        }
    }
}
