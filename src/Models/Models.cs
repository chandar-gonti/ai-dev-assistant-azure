using System.Text.Json;

namespace AiDevAssistant.Models;

public record SummarizePrRequest(string Repository, int PullRequestId);

public record ReviewRequest(string Diff, string Language);

public record GenerateTestsRequest(string SourceCode, string Language, string Framework);

public record PullRequestSummary(
    string Repository,
    int PullRequestId,
    string Summary,
    IReadOnlyList<string> Citations,
    string ModelVersion);

public record GeneratedTests(string Language, string Framework, string Code);

public record ReviewComment(int Line, string Severity, string Comment)
{
    public static IReadOnlyList<ReviewComment> ParseJson(string json)
    {
        try
        {
            var trimmed = json.Trim().TrimStart('`').TrimEnd('`');
            // Strip optional ```json fences
            if (trimmed.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[4..].TrimStart();
            }

            return JsonSerializer.Deserialize<List<ReviewComment>>(
                trimmed,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
