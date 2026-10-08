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
        Flood Certification, 1099-INT, 1099-DIV, 1099-B, 1099-MISC, 1099-NEC,
        1099 Composite, Mortgage Statement, Lease Agreement, Award Letter,
        Retirement Statement, Investment Account Statement, P&L Statement,
        Schedule C, Schedule E, K-1, SSA-89, LOE (Letter of Explanation),
        Divorce Decree, Bankruptcy (Chapter 7), Bankruptcy (Chapter 13),
        Child Support Order, Alimony Agreement, Business License,
        Power of Attorney, Death Certificate, Quitclaim Deed, Trust Document,
        Unknown

        If the document does not match any type above but has a visible title printed on it,
        use that title (in Title Case, max 50 chars) as the documentType instead of Unknown.

        Respond with this exact JSON structure:
        {
          "documentType": "<type from list above, or document's printed title if no match>",
          "documentDate": "<start date: YYYY-MM-DD if exact day found, YYYY-MM if only month found, YYYY if only year found, null if not found>",
          "documentEndDate": "<end date for period documents (bank statements, etc.): YYYY-MM-DD, YYYY-MM, or YYYY format. null if document covers a single date or end date is not found>",
          "notes": "<brief description of what you see, max 100 chars>",
          "subjectName": "<W2 and legal documents only: primary person's full name as printed; null for all other document types>",
          "accountType": "<Bank Statement and Investment/Retirement only: account type such as Checking, Savings, 401K, IRA, etc.; null for all other document types>",
          "institutionName": "<Bank Statement, Investment, and Retirement only: institution name such as Chase, Fidelity, Vanguard, etc.; null for all other document types>"
        }
        """;

    private static readonly string PageNumberPrompt = """
        You are examining a single page from a multi-page document.
        Your only task is to find the page number printed on this page.

        Look for patterns like:
        - "Page 2 of 4" or "Page 2"
        - "2 of 4" or "2/4"
        - A lone page number in the header or footer
        - Roman numerals (i=1, ii=2, iii=3, iv=4, v=5)

        Respond with JSON only:
        {
          "pageNumber": <integer page number, or null if not visible>,
          "totalPages": <integer total if explicitly shown, or null>,
          "notes": "<brief note, max 40 chars>"
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

            var fileContent = await BuildFileContentAsync(filePath, cancellationToken);
            if (fileContent is null)
                return new ClassificationResult("Unknown", null, "Unsupported file type.", false);

            var raw = await SendToClaudeAsync(apiKey, fileContent, ClassificationPrompt, 512, cancellationToken);
            return ParseResponse(raw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Classification failed for: {File}", filePath);
            return new ClassificationResult("Unknown", null, $"Classification error: {ex.Message}", false);
        }
    }

    /// <inheritdoc/>
    public async Task<MergePageResult> ClassifyPageNumberAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = _config["Anthropic:ApiKey"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(apiKey))
                return new MergePageResult(null, null, "API key not configured.", false);

            var fileContent = await BuildFileContentAsync(filePath, cancellationToken);
            if (fileContent is null)
                return new MergePageResult(null, null, "Unsupported file type.", false);

            var raw = await SendToClaudeAsync(apiKey, fileContent, PageNumberPrompt, 128, cancellationToken);
            return ParsePageNumberResponse(raw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Page number classification failed for: {File}", filePath);
            return new MergePageResult(null, null, $"Error: {ex.Message}", false);
        }
    }

    private static async Task<ContentBase?> BuildFileContentAsync(string filePath, CancellationToken ct)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var fileBytes = await File.ReadAllBytesAsync(filePath, ct);
        var base64 = Convert.ToBase64String(fileBytes);

        if (ext == ".pdf")
        {
            return new DocumentContent
            {
                Source = new DocumentSource
                {
                    Type = SourceType.base64,
                    Data = base64,
                    MediaType = "application/pdf"
                }
            };
        }

        var mediaType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            _                 => null
        };

        if (mediaType is null) return null;

        return new ImageContent
        {
            Source = new ImageSource
            {
                MediaType = mediaType,
                Data = base64
            }
        };
    }

    private static async Task<string> SendToClaudeAsync(
        string apiKey,
        ContentBase fileContent,
        string prompt,
        int maxTokens,
        CancellationToken ct)
    {
        var client  = new AnthropicClient(apiKey);
        var request = new MessageParameters
        {
            Model     = AnthropicModels.Claude46Sonnet,
            MaxTokens = maxTokens,
            Messages  = new List<Message>
            {
                new()
                {
                    Role    = RoleType.User,
                    Content = new List<ContentBase> { fileContent, new TextContent { Text = prompt } }
                }
            }
        };

        var response = await client.Messages.GetClaudeMessageAsync(request, ct);
        return response.Content.OfType<TextContent>().FirstOrDefault()?.Text ?? "{}";
    }

    private static MergePageResult ParsePageNumberResponse(string json)
    {
        try
        {
            var clean = json.Trim();
            if (clean.StartsWith("```"))
            {
                var start = clean.IndexOf('{');
                var end   = clean.LastIndexOf('}');
                if (start >= 0 && end > start)
                    clean = clean.Substring(start, end - start + 1);
            }

            using var doc  = System.Text.Json.JsonDocument.Parse(clean);
            var root       = doc.RootElement;
            var pageNumber = root.TryGetProperty("pageNumber", out var pn) && pn.ValueKind == System.Text.Json.JsonValueKind.Number
                ? pn.GetInt32() : (int?)null;
            var totalPages = root.TryGetProperty("totalPages", out var tp) && tp.ValueKind == System.Text.Json.JsonValueKind.Number
                ? tp.GetInt32() : (int?)null;
            var notes      = root.TryGetProperty("notes", out var n) ? n.GetString() ?? "" : "";

            return new MergePageResult(pageNumber, totalPages, notes, true);
        }
        catch
        {
            return new MergePageResult(null, null, "Could not parse page number response.", false);
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

            var docType         = root.TryGetProperty("documentType",    out var dt)  ? dt.GetString()  ?? "Unknown" : "Unknown";
            var docDate         = root.TryGetProperty("documentDate",    out var dd)  ? dd.GetString()              : null;
            var docEndDate      = root.TryGetProperty("documentEndDate", out var ded) ? ded.GetString()             : null;
            var notes           = root.TryGetProperty("notes",           out var n)   ? n.GetString()   ?? ""       : "";
            var subjectName     = root.TryGetProperty("subjectName",     out var sn)  ? sn.GetString()              : null;
            var accountType     = root.TryGetProperty("accountType",     out var at)  ? at.GetString()              : null;
            var institutionName = root.TryGetProperty("institutionName", out var ins) ? ins.GetString()             : null;

            return new ClassificationResult(docType, docDate, notes, true, subjectName, accountType, institutionName, docEndDate);
        }
        catch
        {
            return new ClassificationResult("Unknown", null, "Could not parse classification response.", false);
        }
    }
}
