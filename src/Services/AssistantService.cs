using AiDevAssistant.Models;

namespace AiDevAssistant.Services;

public interface IAssistantService
{
    Task<PullRequestSummary> SummarizePullRequestAsync(
        string repository,
        int pullRequestId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ReviewComment>> ReviewCodeAsync(
        string diff,
        string language,
        CancellationToken cancellationToken);

    Task<GeneratedTests> GenerateUnitTestsAsync(
        string sourceCode,
        string language,
        string framework,
        CancellationToken cancellationToken);
}

public class AssistantService : IAssistantService
{
    private readonly IAzureOpenAIService _openAi;
    private readonly IVectorSearchService _vectorSearch;
    private readonly IContentSafetyService _contentSafety;
    private readonly ILogger<AssistantService> _logger;

    public AssistantService(
        IAzureOpenAIService openAi,
        IVectorSearchService vectorSearch,
        IContentSafetyService contentSafety,
        ILogger<AssistantService> logger)
    {
        _openAi = openAi;
        _vectorSearch = vectorSearch;
        _contentSafety = contentSafety;
        _logger = logger;
    }

    public async Task<PullRequestSummary> SummarizePullRequestAsync(
        string repository,
        int pullRequestId,
        CancellationToken cancellationToken)
    {
        // 1. Fetch PR diff (left as integration point — Azure DevOps / GitHub API)
        var diff = await FetchPullRequestDiffAsync(repository, pullRequestId, cancellationToken);

        // 2. Content Safety check on inputs
        await _contentSafety.EnsureSafeAsync(diff, cancellationToken);

        // 3. RAG: retrieve related code/docs from vector search
        var context = await _vectorSearch.SearchAsync(
            query: diff,
            topK: 8,
            cancellationToken: cancellationToken);

        // 4. Generate summary with GPT-4o
        var prompt = BuildSummaryPrompt(diff, context);
        var completion = await _openAi.CompleteAsync(prompt, cancellationToken);

        // 5. Content Safety check on output
        await _contentSafety.EnsureSafeAsync(completion, cancellationToken);

        return new PullRequestSummary(
            Repository: repository,
            PullRequestId: pullRequestId,
            Summary: completion,
            Citations: context.Select(c => c.Source).ToList(),
            ModelVersion: "gpt-4o");
    }

    public async Task<IReadOnlyList<ReviewComment>> ReviewCodeAsync(
        string diff,
        string language,
        CancellationToken cancellationToken)
    {
        await _contentSafety.EnsureSafeAsync(diff, cancellationToken);

        var context = await _vectorSearch.SearchAsync(diff, topK: 5, cancellationToken);

        var prompt = $$"""
            You are a senior {{language}} engineer reviewing a code change.
            Use the following internal code snippets as reference for our coding standards:

            {{string.Join("\n---\n", context.Select(c => c.Content))}}

            Review the diff below and respond with JSON array of objects:
            [{ "line": <int>, "severity": "info|warn|error", "comment": "..." }]

            Diff:
            {{diff}}
            """;

        var json = await _openAi.CompleteAsync(prompt, cancellationToken);
        return ReviewComment.ParseJson(json);
    }

    public async Task<GeneratedTests> GenerateUnitTestsAsync(
        string sourceCode,
        string language,
        string framework,
        CancellationToken cancellationToken)
    {
        await _contentSafety.EnsureSafeAsync(sourceCode, cancellationToken);

        var prompt = $$"""
            Generate {{framework}} unit tests in {{language}} for the code below.
            Cover happy paths, edge cases, and error conditions. Aim for 90%+ branch coverage.

            Source:
            {{sourceCode}}
            """;

        var tests = await _openAi.CompleteAsync(prompt, cancellationToken);
        return new GeneratedTests(language, framework, tests);
    }

    private static string BuildSummaryPrompt(string diff, IReadOnlyList<SearchResult> context)
    {
        var refs = string.Join("\n---\n", context.Select(c => $"[{c.Source}]\n{c.Content}"));
        return $$"""
            Summarize the following pull request for a code reviewer. Be concise.
            Reference internal context where relevant and cite by [source].

            Internal context:
            {{refs}}

            Diff:
            {{diff}}

            Respond with: 1) one-sentence purpose, 2) key changes (bulleted), 3) risks.
            """;
    }

    private Task<string> FetchPullRequestDiffAsync(
        string repository,
        int pullRequestId,
        CancellationToken cancellationToken)
    {
        // TODO: integrate with Azure DevOps / GitHub APIs.
        _logger.LogDebug("Fetching diff for {Repo}#{Pr}", repository, pullRequestId);
        return Task.FromResult($"// diff placeholder for {repository}#{pullRequestId}");
    }
}
