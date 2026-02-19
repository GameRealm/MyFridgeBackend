using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myFridge.Services.Interfaces;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ScanController : ControllerBase
{
    private readonly IImageAnalysisService _scanService;

    public ScanController(IImageAnalysisService scanService)
    {
        _scanService = scanService;
    }

    [HttpPost("product")]
    public async Task<IActionResult> ScanProduct(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No image uploaded.");

        // Обмеження розміру (щоб не слали 20Мб фотки), наприклад до 5Мб
        if (file.Length > 5 * 1024 * 1024)
            return BadRequest("Image too large. Max 5MB.");

        try
        {
            var result = await _scanService.AnalyzeProductImageAsync(file);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}