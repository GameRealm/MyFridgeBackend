using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myFridge.Services.Interfaces;
using System.Text.Json;

namespace myFridge.api.Controllers;

[Authorize] 
[ApiController]
[Route("api/[controller]")]
public class StoragePlaceController : ControllerBase
{
    private readonly IStoragePlaceService _storageService;

    public StoragePlaceController(IStoragePlaceService storageService)
    {
        _storageService = storageService;
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetMyStoragePlaces()
    {
        try
        {
            // Витягуємо токен чисто (без "Bearer ")
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            var resultJson = await _storageService.GetMyStoragePlacesAsync(token);

            // Десеріалізуємо, щоб повернути гарний JSON, а не рядок у лапках
            var data = JsonSerializer.Deserialize<object>(resultJson);

            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] Guid storagePlaceId)
    {
        try
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            var resultJson = await _storageService.GetProductsInStoragePlaceAsync(token, storagePlaceId);

            var data = JsonSerializer.Deserialize<object>(resultJson);

            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}