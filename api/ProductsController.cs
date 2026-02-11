using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using myFridge.DTOs.Products;
using myFridge.DTOs.StoragePlaces;
using myFridge.DTOs.Users;
namespace myFridge.Api;


[ApiController]
[Route("api/[controller]")]
public class productsController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;

    public productsController(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _config = config;
    }
    //Great
    [HttpGet]
    public async Task<IActionResult> GetAllProducts([FromQuery] Guid userId)
    {
        var supabaseUrl = _config["SUPABASE_URL"];
        var supabaseKey = _config["SUPABASE_API_KEY"];

        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            return StatusCode(500, new { error = "Supabase URL or API key not set in environment variables" });

        try
        {
            var client = _httpFactory.CreateClient();

            // 🔥 ФІЛЬТР ПО КОРИСТУВАЧУ
            var url = $"{supabaseUrl}/rest/v1/products" +
                      $"?user_id=eq.{userId}" +
                      $"&select=name,quantity,unit,expiration_date,users(email),storage_places(name)";

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apikey", supabaseKey);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", supabaseKey);

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

            var json = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            var products = JsonSerializer.Deserialize<List<ProductDto>>(json, options);

            return Ok(products);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    //Great
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductById(Guid id)
    {
        try
        {
            var client = _httpFactory.CreateClient();

            var supabaseUrl = _config["SUPABASE_URL"];
            var supabaseKey = _config["SUPABASE_API_KEY"];

            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
                return StatusCode(500, new { error = "Supabase URL or API key not set" });

            var url = $"{supabaseUrl}/rest/v1/products?id=eq.{id}&select=name,quantity,unit,expiration_date,users(email),storage_places(name)";

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apikey", supabaseKey);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", supabaseKey);

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

            var json = await response.Content.ReadAsStringAsync();

            // Supabase повертає масив
            var products = JsonSerializer.Deserialize<List<object>>(json);

            if (products == null || products.Count == 0)
                return NotFound(new { error = "Product not found" });

            return Ok(products[0]);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
    {
        try
        {
            var client = _httpFactory.CreateClient();

            var supabaseUrl = _config["SUPABASE_URL"];
            var supabaseKey = _config["SUPABASE_API_KEY"];

            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
                return StatusCode(500, new { error = "Supabase URL or API key not set" });

            var url = $"{supabaseUrl}/rest/v1/products";

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apikey", supabaseKey);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", supabaseKey);

            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            // важливо щоб Supabase повернув створений запис
            client.DefaultRequestHeaders.Add("Prefer", "return=representation");

            var response = await client.PostAsync(url, content);

            var resp = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, resp);

            return Ok(JsonSerializer.Deserialize<object>(resp));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateProduct(Guid id, UpdateProductDto dto)
    {
        var supabaseUrl = _config["SUPABASE_URL"];
        var supabaseKey = _config["SUPABASE_API_KEY"];

        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            return StatusCode(500, new { error = "Supabase URL or API key not set" });

        try
        {
            var client = _httpFactory.CreateClient();

            var url = $"{supabaseUrl}/rest/v1/products?id=eq.{id}";

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apikey", supabaseKey);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", supabaseKey);
            client.DefaultRequestHeaders.Add("Prefer", "return=representation");

            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PatchAsync(url, content);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

            var result = await response.Content.ReadAsStringAsync();
            return Ok(JsonSerializer.Deserialize<object>(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

}
