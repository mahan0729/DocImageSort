using DocImageSort.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocImageSort.Tests.Services;

public class ClassificationServiceTests
{
    private ClassificationService BuildService(string? apiKey = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Anthropic:ApiKey"] = apiKey ?? string.Empty
            })
            .Build();
        return new ClassificationService(config, NullLogger<ClassificationService>.Instance);
    }

    [Fact]
    public async Task ClassifyAsync_NoApiKey_ReturnsFailureWithUnknownType()
    {
        var sut = BuildService(apiKey: "");
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "dummy content");

        try
        {
            var result = await sut.ClassifyAsync(tempFile);

            Assert.False(result.Success);
            Assert.Equal("Unknown", result.DocumentType);
        }
        finally { File.Delete(tempFile); }
    }

    [Fact]
    public async Task ClassifyAsync_WhitespaceApiKey_ReturnsFailure()
    {
        var sut = BuildService(apiKey: "   ");
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "dummy");

        try
        {
            var result = await sut.ClassifyAsync(tempFile);

            Assert.False(result.Success);
        }
        finally { File.Delete(tempFile); }
    }

    [Fact]
    public async Task ClassifyAsync_FileNotFound_ReturnsFailureWithoutThrowing()
    {
        var sut = BuildService(apiKey: "sk-ant-test-key");

        // Should not throw — pipeline relies on ClassifyAsync never throwing.
        var result = await sut.ClassifyAsync(@"C:\nonexistent\file.pdf");

        Assert.False(result.Success);
        Assert.Equal("Unknown", result.DocumentType);
    }
}
