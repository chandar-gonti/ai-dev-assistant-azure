using Azure.AI.OpenAI;
using Polly;
using Polly.Retry;

namespace AiDevAssistant.Services;

public interface IAzureOpenAIService
{
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken);
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken);
}

public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly OpenAIClient _client;
    private readonly string _chatDeployment;
    private readonly string _embeddingDeployment;
    private readonly ILogger<AzureOpenAIService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public AzureOpenAIService(
        OpenAIClient client,
        IConfiguration config,
        ILogger<AzureOpenAIService> logger)
    {
        _client = client;
        _chatDeployment = config["AzureOpenAI:DeploymentName"]
            ?? throw new InvalidOperationException("DeploymentName missing");
        _embeddingDeployment = config["AzureOpenAI:EmbeddingDeployment"]
            ?? throw new InvalidOperationException("EmbeddingDeployment missing");
        _logger = logger;

        _retryPolicy = Policy
            .Handle<Azure.RequestFailedException>(ex => ex.Status == 429 || ex.Status >= 500)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (ex, delay, attempt, _) =>
                    _logger.LogWarning(ex, "OpenAI retry {Attempt} after {Delay}s", attempt, delay.TotalSeconds));
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken)
    {
        var options = new ChatCompletionsOptions
        {
            DeploymentName = _chatDeployment,
            Temperature = 0.2f,
            MaxTokens = 2048,
            Messages =
            {
                new ChatRequestSystemMessage(
                    "You are an expert code reviewer for an enterprise team. " +
                    "Be precise, cite sources, and never invent APIs."),
                new ChatRequestUserMessage(prompt),
            },
        };

        var response = await _retryPolicy.ExecuteAsync(
            () => _client.GetChatCompletionsAsync(options, cancellationToken));

        return response.Value.Choices[0].Message.Content;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        var options = new EmbeddingsOptions(_embeddingDeployment, [text]);
        var response = await _retryPolicy.ExecuteAsync(
            () => _client.GetEmbeddingsAsync(options, cancellationToken));

        return response.Value.Data[0].Embedding.ToArray();
    }
}
