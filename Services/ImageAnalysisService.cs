using myFridge.DTOs.AIPhoto;
using myFridge.Services.Interfaces;
using System.Text;
using System.Text.Json;

public class ImageAnalysisService : IImageAnalysisService
{
    private readonly HttpClient? _httpClient;
    private readonly string? _apiKey;

    public ImageAnalysisService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["GEMINI_API_KEY"];
    }

    public async Task<List<ScannedProductDto>> AnalyzeProductImageAsync(IFormFile imageFile)
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        using var ms = new MemoryStream();
        await imageFile.CopyToAsync(ms);
        var imageBytes = ms.ToArray();
        var base64Image = Convert.ToBase64String(imageBytes);

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={_apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                   parts = new object[]
                    {
                        new { text = $@"Analyze this image and identify ALL visible grocery products. 
Extract details for each product into a JSON array.

Current Date: {today}

1. NAME: Full product name.
2. CATEGORY (CRITICAL: You MUST choose ONLY ONE from this exact list):
   - Молочні продукти
   - М'ясо
   - Риба
   - Овочі
   - Фрукти
   - Крупи
   - Хлібобулочні
   - Заморожені
   - Напої
   - Солодощі
   - Соуси
   - Консерви
   - Горіхи
   - Яйця
   - Готові страви
   - Алкоголь
   - Інше
3. EXPIRY DATE Logic:
   - Look for printed date. 
   - If not found, estimate conservatively based on product type.
4. GROUPING & QUANTITY Logic (CRITICAL):
   - IF you see multiple IDENTICAL items (same brand, name, and size), GROUP them into one entry.
   - Set 'Quantity' to the total number of individual items found.
   - Set 'Volume' to the weight or volume of ONE single item (e.g., if there are five 200g yogurts, Quantity = 5, Volume = 200, Unit = 'g').
5. LIQUIDS & UNITS:
   - For liquids (milk, juice, oil), use 'ml' or 'l'. 
   - UNIT RULE: Use 'g'/'ml' if <1000, 'kg'/'l' if >=1000.
   - If no weight is visible, use Unit: 'pcs' (Exception: eggs are always 'pcs').

Return ONLY a JSON array of objects in this exact format:
[
  {{ 
    ""Name"": ""string"", 
    ""ExpiryDate"": ""yyyy-MM-dd"", 
    ""Quantity"": 5, 
    ""Volume"": 200, 
    ""Unit"": ""g"", 
    ""Category"": ""Молочні продукти"" 
  }}
]" },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = imageFile.ContentType,
                                data = base64Image
                            }
                        }
                    }
                }
            },
            generationConfig = new
            {
                response_mime_type = "application/json"
            }
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient!.PostAsync(url, jsonContent);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API Error: {error}");
        }

        var resultJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(resultJson);

        var text = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
        text = text!.Replace("```json", "").Replace("```", "").Trim();

        // 2. Десеріалізуємо в List<ScannedProductDto>
        var scannedProducts = JsonSerializer.Deserialize<List<ScannedProductDto>>(text, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        });

        // 3. Повертаємо список (або порожній список, якщо раптом null)
        return scannedProducts ?? new List<ScannedProductDto>();
    }
}