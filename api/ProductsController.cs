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

    public ProductsController(IProductService repository)
    {
        _service = repository;
    }

    // Helper: Витягуємо ID юзера з токена
    // Цей метод викликається там, де потрібно фільтрувати дані по юзеру
    private string GetUserId()
    {
        return User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("User ID not found");
    }


    // 1. GET ALL (Отримати всі продукти користувача)
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

    // 2. GET BY ID (Отримати конкретний продукт)
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

    // 3. CREATE (Створити продукт)
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
            // Тут ловимо помилки валідації або доступу до StoragePlace
            return BadRequest(new { error = ex.Message });
        }
    }

    // 4. UPDATE (Оновити продукт)
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

    // 5. DELETE (Розумне видалення: soft або hard)
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
}