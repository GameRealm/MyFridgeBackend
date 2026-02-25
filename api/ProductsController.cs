using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myFridge.DTOs.Products;
using myFridge.Services.Interfaces;
using System.Security.Claims;
namespace myFridge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    public ProductsController(IProductService repository) =>  _service = repository;
    
    private string GetUserId()
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User ID not found");

        return userId;
    }

    [HttpGet] 
    public async Task<IActionResult> GetProducts([FromQuery] ProductFilterDto filter)
    {
        try
        {
            var userId = GetUserId();
            var products = await _service.GetProductsAsync(userId, filter);
            return Ok(products);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var product = await _service.GetByIdAsync(id);

            if (product == null)
                return NotFound(new { error = "Product not found" });

            return Ok(product);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        try
        {
            var userId = GetUserId();
            var product = await _service.CreateAsync(dto, userId);
            return Ok(product);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto)
    {
        try
        {
            var updatedProduct = await _service.UpdateAsync(id, dto);

            if (updatedProduct == null)
                return NotFound(new { error = "Product not found or update failed" });

            return Ok(updatedProduct);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var success = await _service.DeleteSmartAsync(id);

            if (!success)
                return NotFound(new { error = "Product not found" });

            return Ok(new { message = "Processed successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPatch("{id}/favorite")]
    public async Task<IActionResult> SetFavorite(Guid id, [FromBody] bool isFavorite)
    {
        try
        {
            var success = await _service.UpdateFavoriteAsync(id, isFavorite);

            if (!success)
                return NotFound(new { error = "Product not found" });

            return Ok(new { message = "Updated successfully", isFavorite });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("batch")]
    public async Task<IActionResult> CreateProductsBatch([FromBody] List<CreateProductDto> productsDto)
    {
        if (productsDto == null || !productsDto.Any())
        {
            return BadRequest(new { message = "Масив продуктів порожній." });
        }

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { message = "Не вдалося розпізнати користувача з токена." });
        }

        var productsToCreate = productsDto.Select(p => new CreateProductDto
        {
            Name = p.Name,
           
            Quantity = p.Quantity,
            Unit = p.Unit,
            Expiration_Date = p.Expiration_Date,
            Storage_Place_Id = p.Storage_Place_Id,
            Comment = p.Comment,
            UserId = userId 
        }).ToList();

        await _service.CreateProductsBatchAsync(productsToCreate);

        return Ok(new { message = $"Успішно збережено {productsToCreate.Count} продуктів!" });
    }
}