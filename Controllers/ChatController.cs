
using BikeRepairAI.Models;
using BikeRepairAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace BikeRepairAI.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly RagService _rag;

    public ChatController(RagService rag)
    {
        _rag = rag;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask(AskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question is required.");

        var answer = await _rag.AskAsync(request.Question);

        return Ok(new
        {
            Question = request.Question,
            Answer = answer
        });
    }
}
