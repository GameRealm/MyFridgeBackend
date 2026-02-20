namespace myFridge.DTOs.Recipe;

public class RecipeRequestDto
{
    public List<string> AvailableIngredients { get; set; } = new();
    public string? UserPrompt { get; set; } 
}

public class RecipeResponseDto
{
    public string Title { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string PrepTime { get; set; } = string.Empty;
    public int MatchPercentage { get; set; }
    public List<string> UsedIngredients { get; set; } = new();
    public List<string> MissingIngredients { get; set; } = new();
    public List<string> Instructions { get; set; } = new();
}