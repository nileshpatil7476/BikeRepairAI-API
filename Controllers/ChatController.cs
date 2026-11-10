
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
            var answer = await _rag.AskAsync(request.Question);
            return Ok(new
            {
                Question = request.Question,
                Answer = answer
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
            "BikeRepairAI Chat API failed");

            Console.Error.WriteLine(ex.ToString());

            return StatusCode(500, new
            {
                Message = "Chat processing failed. Check server logs."
            });
        }
    }
}
