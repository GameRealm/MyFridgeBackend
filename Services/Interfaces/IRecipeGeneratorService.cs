using myFridge.DTOs.Recipe;
namespace myFridge.Services.Interfaces;

public interface IRecipeGeneratorService
{
    Task<List<RecipeResponseDto>> GenerateRecipesAsync(RecipeRequestDto request);
}