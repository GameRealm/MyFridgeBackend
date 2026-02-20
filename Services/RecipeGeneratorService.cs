using myFridge.DTOs.Recipe;
using myFridge.Services.Interfaces;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace myFridge.Services
{
    public class RecipeGeneratorService : IRecipeGeneratorService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public RecipeGeneratorService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["GEMINI_API_KEY"] ?? throw new Exception("API Key is missing");
        }

        public async Task<List<RecipeResponseDto>> GenerateRecipesAsync(RecipeRequestDto request)
        {
            var ingredientsStr = string.Join(", ", request.AvailableIngredients);
            var userPrompt = string.IsNullOrWhiteSpace(request.UserPrompt)? "Запропонуй 3 смачні страви."
                : request.UserPrompt;

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            var promptText = $@"You are a professional chef. Suggest 5-6 recipes based on the available ingredients and user preferences.

Ingredients available: [{ingredientsStr}].
User preference: '{userPrompt}'.
Language: Ukrainian.

STRICT RULES FOR RECIPE SELECTION:
1. EXACT MATCH (First 3 recipes): You MUST provide exactly 3 recipes that use ONLY the available ingredients. For these, 'MissingIngredients' must be empty [] and 'MatchPercentage' must be 100.
2. PARTIAL MATCH (Next 2-3 recipes): Provide 2 to 3 recipes that require 1 or 2 extra ingredients not in the available list. 'MissingIngredients' must contain exactly 1-2 items, and 'MatchPercentage' should be calculated accurately (e.g., if recipe needs 4 ingredients and user has 3, it's 75).
3. INSTRUCTIONS DETAIL: By default, provide a short 1-2 sentence overview/summary in the 'Instructions' array. ONLY provide detailed step-by-step instructions if the user explicitly asks for them (e.g., 'покроково', 'детально', 'step-by-step') in the 'User preference'.

Return ONLY a single JSON array of objects in this EXACT format (do not use markdown or json code blocks):
[
  {{
    ""Title"": ""string"",
    ""Difficulty"": ""Easy/Medium/Hard"",
    ""PrepTime"": ""string (e.g. 15 хв)"",
    ""MatchPercentage"": 100,
    ""UsedIngredients"": [""string""],
    ""MissingIngredients"": [""string""],
    ""Instructions"": [""string""]
  }}
]";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = promptText } } }
                },
                generationConfig = new { response_mime_type = "application/json" }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API Error: {error}");
            }

            var resultJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(resultJson);

            var responseText = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
            responseText = responseText!.Replace("```json", "").Replace("```", "").Trim();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };

            var recipes = JsonSerializer.Deserialize<List<RecipeResponseDto>>(responseText, options);
            return recipes ?? new List<RecipeResponseDto>();
        }
    }
}