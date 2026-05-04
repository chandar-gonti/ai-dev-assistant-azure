using AiDevAssistant.Models;
using AiDevAssistant.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiDevAssistant.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssistantController : ControllerBase
{
    private readonly IAssistantService _assistantService;
    private readonly ILogger<AssistantController> _logger;

    public AssistantController(
        IAssistantService assistantService,
        ILogger<AssistantController> logger)
    {
        _assistantService = assistantService;
        _logger = logger;
    }

    /// <summary>Summarize a pull request using GPT-4o + RAG over related code.</summary>
    [HttpPost("summarize-pr")]
    [ProducesResponseType(typeof(PullRequestSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SummarizePullRequest(
        [FromBody] SummarizePrRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Repository))
        {
            return BadRequest("Repository is required.");
        }

        _logger.LogInformation(
            "Summarizing PR {PrId} in {Repo}",
            request.PullRequestId,
            request.Repository);

        var summary = await _assistantService.SummarizePullRequestAsync(
            request.Repository,
            request.PullRequestId,
            cancellationToken);

        return Ok(summary);
    }

    /// <summary>Generate code review comments for a diff.</summary>
    [HttpPost("review")]
    [ProducesResponseType(typeof(IReadOnlyList<ReviewComment>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReviewCode(
        [FromBody] ReviewRequest request,
        CancellationToken cancellationToken)
    {
        var comments = await _assistantService.ReviewCodeAsync(
            request.Diff,
            request.Language,
            cancellationToken);

        return Ok(comments);
    }

    /// <summary>Generate unit tests for a function or class.</summary>
    [HttpPost("generate-tests")]
    [ProducesResponseType(typeof(GeneratedTests), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateTests(
        [FromBody] GenerateTestsRequest request,
        CancellationToken cancellationToken)
    {
        var tests = await _assistantService.GenerateUnitTestsAsync(
            request.SourceCode,
            request.Language,
            request.Framework,
            cancellationToken);

        return Ok(tests);
    }
}
