using Azure.AI.ContentSafety;

namespace AiDevAssistant.Services;

public interface IContentSafetyService
{
    Task EnsureSafeAsync(string text, CancellationToken cancellationToken);
}

public class ContentSafetyService : IContentSafetyService
{
    private readonly ContentSafetyClient _client;
    private readonly ILogger<ContentSafetyService> _logger;

    // Severity threshold (0=safe, 6=highest). Block at >= 4.
    private const int BlockSeverityThreshold = 4;

    public ContentSafetyService(
        ContentSafetyClient client,
        ILogger<ContentSafetyService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task EnsureSafeAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var options = new AnalyzeTextOptions(text);
        var response = await _client.AnalyzeTextAsync(options, cancellationToken);

        foreach (var category in response.Value.CategoriesAnalysis)
        {
            if (category.Severity >= BlockSeverityThreshold)
            {
                _logger.LogWarning(
                    "Content blocked: category={Category} severity={Severity}",
                    category.Category,
                    category.Severity);
                throw new InvalidOperationException(
                    $"Content failed safety check ({category.Category}).");
            }
        }
    }
}
