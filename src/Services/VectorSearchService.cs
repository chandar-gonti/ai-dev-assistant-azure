using Azure.Search.Documents;
using Azure.Search.Documents.Models;

namespace AiDevAssistant.Services;

public record SearchResult(string Source, string Content, double Score);

public interface IVectorSearchService
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int topK,
        CancellationToken cancellationToken);
}

public class VectorSearchService : IVectorSearchService
{
    private readonly SearchClient _searchClient;
    private readonly IAzureOpenAIService _openAi;

    public VectorSearchService(SearchClient searchClient, IAzureOpenAIService openAi)
    {
        _searchClient = searchClient;
        _openAi = openAi;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int topK,
        CancellationToken cancellationToken)
    {
        // Generate query embedding
        var embedding = await _openAi.EmbedAsync(query, cancellationToken);

        // Hybrid search: vector + BM25 keyword
        var options = new SearchOptions
        {
            Size = topK,
            VectorSearch = new VectorSearchOptions
            {
                Queries =
                {
                    new VectorizedQuery(embedding)
                    {
                        KNearestNeighborsCount = topK,
                        Fields = { "contentVector" },
                    },
                },
            },
        };
        options.Select.Add("source");
        options.Select.Add("content");

        var response = await _searchClient.SearchAsync<SearchDocument>(
            searchText: query,
            options: options,
            cancellationToken: cancellationToken);

        var results = new List<SearchResult>();
        await foreach (var hit in response.Value.GetResultsAsync())
        {
            results.Add(new SearchResult(
                Source: hit.Document["source"]?.ToString() ?? "unknown",
                Content: hit.Document["content"]?.ToString() ?? string.Empty,
                Score: hit.Score ?? 0.0));
        }

        return results;
    }
}
