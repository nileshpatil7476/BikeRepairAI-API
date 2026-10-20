
using BikeRepairAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace BikeRepairAI.Controllers;

[ApiController]
[Route("api/knowledge")]
public class KnowledgeController : ControllerBase
{
    private readonly RagService _rag;

    public KnowledgeController(RagService rag)
    {
        _rag = rag;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Please upload a PDF.");

        if (!Path.GetExtension(file.FileName)
            .Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only PDF files are allowed.");

        var path = Path.Combine(
            Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");

        try
        {
            await using (var stream = System.IO.File.Create(path))
            {
                await file.CopyToAsync(stream);
            }

            var count = await _rag.IngestPdfAsync(
                path, Path.GetFileName(file.FileName));

            return Ok(new
            {
                Message = "PDF indexed successfully.",
                ChunksStored = count
            });
        }
        finally
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }
}
