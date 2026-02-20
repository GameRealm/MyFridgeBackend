using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myFridge.DTOs.Recipe;
using myFridge.Services.Interfaces;
namespace myFridge.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] 
public class RecipeController : ControllerBase
{
    private readonly IRecipeGeneratorService _recipeService;

    public RecipeController(IRecipeGeneratorService recipeService)
    {
        _recipeService = recipeService;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateRecipes([FromBody] RecipeRequestDto request)
    {
        if (request.AvailableIngredients == null || !request.AvailableIngredients.Any())
        {
            return BadRequest("Список інгредієнтів не може бути порожнім.");
        }

        try
        {
            var recipes = await _recipeService.GenerateRecipesAsync(request);
            return Ok(recipes);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Помилка генерації рецептів", error = ex.Message });
        }
    }
}