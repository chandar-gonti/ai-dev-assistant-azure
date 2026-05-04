using AiDevAssistant.Models;
using AiDevAssistant.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AiDevAssistant.Tests;

public class AssistantServiceTests
{
    private readonly Mock<IAzureOpenAIService> _openAi = new();
    private readonly Mock<IVectorSearchService> _vectorSearch = new();
    private readonly Mock<IContentSafetyService> _contentSafety = new();

    private AssistantService BuildService() => new(
        _openAi.Object,
        _vectorSearch.Object,
        _contentSafety.Object,
        NullLogger<AssistantService>.Instance);

    [Fact]
    public async Task SummarizePullRequestAsync_ReturnsSummaryWithCitations()
    {
        // Arrange
        _vectorSearch
            .Setup(v => v.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SearchResult>
            {
                new("docs/style-guide.md", "Use async/await consistently.", 0.95),
                new("src/Helpers.cs", "public static T Guard<T>(...) { ... }", 0.88),
            });

        _openAi
            .Setup(o => o.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("This PR adds null-checks and a retry helper.");

        var service = BuildService();

        // Act
        var result = await service.SummarizePullRequestAsync("acme/widgets", 42, CancellationToken.None);

        // Assert
        result.Repository.Should().Be("acme/widgets");
        result.PullRequestId.Should().Be(42);
        result.Summary.Should().Contain("retry helper");
        result.Citations.Should().HaveCount(2);
        result.ModelVersion.Should().Be("gpt-4o");
    }

    [Fact]
    public async Task SummarizePullRequestAsync_RunsContentSafetyOnInputAndOutput()
    {
        _vectorSearch
            .Setup(v => v.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SearchResult>());

        _openAi
            .Setup(o => o.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Summary text");

        var service = BuildService();

        await service.SummarizePullRequestAsync("acme/widgets", 1, CancellationToken.None);

        _contentSafety.Verify(
            c => c.EnsureSafeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }

    [Fact]
    public void ReviewComment_ParseJson_HandlesCleanArray()
    {
        var json = """[{"line": 5, "severity": "warn", "comment": "Avoid var here"}]""";

        var comments = ReviewComment.ParseJson(json);

        comments.Should().HaveCount(1);
        comments[0].Line.Should().Be(5);
        comments[0].Severity.Should().Be("warn");
    }

    [Fact]
    public void ReviewComment_ParseJson_ReturnsEmptyForInvalidJson()
    {
        var comments = ReviewComment.ParseJson("not json at all");
        comments.Should().BeEmpty();
    }
}
