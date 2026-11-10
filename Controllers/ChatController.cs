
using BikeRepairAI.Models;
using BikeRepairAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace BikeRepairAI.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly RagService _rag;
    private readonly ILogger<ChatController> _logger;

    public ChatController(RagService rag, ILogger<ChatController> logger)
    {
        _rag = rag;
        _logger = logger;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask(AskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question is required.");

        try
        {
            _logger.LogInformation(
                "Chat API request started.");

            var answer = await _rag.AskAsync(request.Question);

            _logger.LogInformation(
                "Chat API request completed successfully.");

            return Ok(new
            {
                Question = request.Question,
                Answer = answer
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred in ChatController Ask method. Question: {Question}, TraceId: {TraceId}",
                request.Question,
                HttpContext.TraceIdentifier);

            return StatusCode(500, new
            {
                success = false,
                message = "Something went wrong while processing your question.",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }
}
