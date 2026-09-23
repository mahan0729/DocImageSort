using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DocImageSort.Api.Controllers;

/// <summary>
/// Reads and writes runtime-configurable settings in appsettings.json.
/// Path changes (DropFolder / FilesFolder) require an API restart to take effect.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(IWebHostEnvironment env, ILogger<SettingsController> logger)
    {
        _env = env;
        _logger = logger;
    }

    private string SettingsPath => Path.Combine(_env.ContentRootPath, "appsettings.json");

    /// <summary>GET /api/settings — returns current editable settings.</summary>
    [HttpGet]
    public IActionResult Get()
    {
        try
        {
            var json = System.IO.File.ReadAllText(SettingsPath);
            var root = JsonNode.Parse(json)!;

            return Ok(new SettingsDto(
                UseAzure:       root["FeatureFlags"]?["UseAzure"]?.GetValue<bool>()   ?? false,
                DropFolderPath: root["DropFolder"]?["Path"]?.GetValue<string>()       ?? string.Empty,
                FilesFolderPath:root["FilesFolder"]?["Path"]?.GetValue<string>()      ?? string.Empty,
                AnthropicApiKey:root["Anthropic"]?["ApiKey"]?.GetValue<string>()      ?? string.Empty
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read settings");
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>PUT /api/settings — persists editable settings to appsettings.json.</summary>
    [HttpPut]
    public IActionResult Update([FromBody] SettingsDto req)
    {
        try
        {
            var json = System.IO.File.ReadAllText(SettingsPath);
            var root = JsonNode.Parse(json)!;

            root["FeatureFlags"]!["UseAzure"]  = req.UseAzure;
            root["DropFolder"]!["Path"]         = req.DropFolderPath.Trim();
            root["FilesFolder"]!["Path"]        = req.FilesFolderPath.Trim();
            root["Anthropic"]!["ApiKey"]        = req.AnthropicApiKey.Trim();

            var options = new JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(SettingsPath, root.ToJsonString(options));

            _logger.LogInformation("Settings updated — UseAzure={UseAzure}", req.UseAzure);
            return Ok(req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write settings");
            return StatusCode(500, ex.Message);
        }
    }
}

public record SettingsDto(
    bool   UseAzure,
    string DropFolderPath,
    string FilesFolderPath,
    string AnthropicApiKey
);
