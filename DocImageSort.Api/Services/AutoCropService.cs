using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using DocImageSort.Api.Services.Interfaces;

namespace DocImageSort.Api.Services;

/// <summary>
/// Detects document boundaries in phone-camera images using Claude's vision API
/// and crops to remove desk/background clutter before pipeline processing.
/// Applies only to JPG and PNG files; PDFs pass through unchanged.
/// Windows-only: uses System.Drawing.Common (GDI+) for image manipulation.
/// </summary>
[SupportedOSPlatform("windows")]
public class AutoCropService : IAutoCropService
{
    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };

    /// <summary>
    /// Skips cropping when the detected document already fills this fraction of the image —
    /// the photo was well-framed and cropping would add no value.
    /// </summary>
    private const double WellFramedThreshold = 0.92;

    private static readonly string CropPrompt = """
        You are analyzing a photo of a paper document taken by a phone camera.
        The document (a sheet of paper, form, or statement) is lying on a surface.
        Find the edges of the document and return its bounding rectangle.

        Express the rectangle as fractions of the full image size (0.0 = left/top, 1.0 = right/bottom).
        Add about 1% margin so you do not clip any document content.

        Respond with JSON only:
        {
          "x": <left edge fraction, e.g. 0.05>,
          "y": <top edge fraction, e.g. 0.08>,
          "width": <document width fraction, e.g. 0.90>,
          "height": <document height fraction, e.g. 0.84>,
          "confidence": "<high|medium|low>",
          "notes": "<brief note, max 40 chars>"
        }

        Return confidence "low" when:
        - No clear document boundary is visible
        - The document already fills nearly the entire frame
        - You are unsure of the edges
        """;

    private readonly IConfiguration _config;
    private readonly ILogger<AutoCropService> _logger;

    public AutoCropService(IConfiguration config, ILogger<AutoCropService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> CropToDocumentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (!ImageExtensions.Contains(ext))
            return filePath; // PDFs and unsupported types are unchanged

        try
        {
            var apiKey = _config["Anthropic:ApiKey"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(apiKey))
                return filePath;

            var bounds = await DetectDocumentBoundsAsync(filePath, apiKey, cancellationToken);

            if (bounds is null)
            {
                _logger.LogDebug("AutoCrop: no bounds detected for {File} — skipping", Path.GetFileName(filePath));
                return filePath;
            }

            // Skip when the document already fills nearly the full frame
            var coverage = bounds.Width * bounds.Height;
            if (coverage >= WellFramedThreshold)
            {
                _logger.LogDebug("AutoCrop: {File} already well-framed ({Coverage:P0}) — skipping",
                    Path.GetFileName(filePath), coverage);
                return filePath;
            }

            ApplyCrop(filePath, bounds, ext);
            _logger.LogInformation("AutoCrop: cropped {File} to {X:F2},{Y:F2} {W:F2}×{H:F2}",
                Path.GetFileName(filePath), bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AutoCrop: failed for {File} — proceeding with original", Path.GetFileName(filePath));
        }

        return filePath;
    }

    private async Task<CropBounds?> DetectDocumentBoundsAsync(
        string filePath, string apiKey, CancellationToken ct)
    {
        var ext       = Path.GetExtension(filePath).ToLowerInvariant();
        var fileBytes = await File.ReadAllBytesAsync(filePath, ct);
        var base64    = Convert.ToBase64String(fileBytes);
        var mediaType = ext == ".png" ? "image/png" : "image/jpeg";

        var fileContent = new ImageContent
        {
            Source = new ImageSource { MediaType = mediaType, Data = base64 }
        };

        var client  = new AnthropicClient(apiKey);
        var request = new MessageParameters
        {
            Model     = AnthropicModels.Claude46Sonnet,
            MaxTokens = 128,
            Messages  = new List<Message>
            {
                new()
                {
                    Role    = RoleType.User,
                    Content = new List<ContentBase> { fileContent, new TextContent { Text = CropPrompt } }
                }
            }
        };

        var response = await client.Messages.GetClaudeMessageAsync(request, ct);
        var raw      = response.Content.OfType<TextContent>().FirstOrDefault()?.Text ?? "{}";

        return ParseBounds(raw);
    }

    private CropBounds? ParseBounds(string json)
    {
        try
        {
            var clean = json.Trim();
            if (clean.StartsWith("```"))
            {
                var s = clean.IndexOf('{');
                var e = clean.LastIndexOf('}');
                if (s >= 0 && e > s) clean = clean.Substring(s, e - s + 1);
            }

            using var doc  = System.Text.Json.JsonDocument.Parse(clean);
            var root       = doc.RootElement;
            var confidence = root.TryGetProperty("confidence", out var c) ? c.GetString() : "low";

            // Only crop when AI is highly confident — Chance's requirement: when in doubt, don't crop
            if (!string.Equals(confidence, "high", StringComparison.OrdinalIgnoreCase))
                return null;

            double Get(string name) => root.TryGetProperty(name, out var p) ? p.GetDouble() : 0;

            var x = Clamp(Get("x"));
            var y = Clamp(Get("y"));
            var w = Clamp(Get("width"));
            var h = Clamp(Get("height"));

            // Ensure rectangle doesn't exceed image bounds
            w = Math.Min(w, 1.0 - x);
            h = Math.Min(h, 1.0 - y);

            if (w <= 0 || h <= 0) return null;

            return new CropBounds(x, y, w, h);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "AutoCrop: could not parse bounding box response");
            return null;
        }
    }

    private static void ApplyCrop(string filePath, CropBounds bounds, string ext)
    {
        var tempPath = filePath + ".crop_tmp";
        try
        {
            using (var bmp = new Bitmap(filePath))
            {
                var pixX = Math.Max(0, (int)(bounds.X * bmp.Width));
                var pixY = Math.Max(0, (int)(bounds.Y * bmp.Height));
                var pixW = Math.Max(1, Math.Min((int)(bounds.Width  * bmp.Width),  bmp.Width  - pixX));
                var pixH = Math.Max(1, Math.Min((int)(bounds.Height * bmp.Height), bmp.Height - pixY));

                using var cropped = bmp.Clone(new Rectangle(pixX, pixY, pixW, pixH), bmp.PixelFormat);
                var format = ext == ".png" ? ImageFormat.Png : ImageFormat.Jpeg;
                cropped.Save(tempPath, format);
            }
            // Bitmap is disposed — safe to replace the file
            File.Move(tempPath, filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static double Clamp(double v) => Math.Max(0.0, Math.Min(1.0, v));

    private record CropBounds(double X, double Y, double Width, double Height);
}
