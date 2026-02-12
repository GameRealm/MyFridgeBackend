using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myFridge.DTOs.Products;
using myFridge.DTOs.StoragePlaces;
using myFridge.DTOs.Users;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
    public async Task<IActionResult> GetAllProducts()
    {
        var supabaseUrl = _config["SUPABASE_URL"];
        var supabaseKey = _config["SUPABASE_API_KEY"];

        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            return StatusCode(500, new { error = "Supabase URL or API key not set" });

        try
        {
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apikey", supabaseKey);
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", supabaseKey); // сервісний ключ

            // 🔹 select з join на users та storage_places
            var url = $"{supabaseUrl}/rest/v1/products?select=id,name,quantity,unit,expiration_date,users(id,email),storage_places(id,name)&order=created_at.asc";

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, err);
            }

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
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader))
                return Unauthorized(new { error = "Missing Authorization header" });

            var token = authHeader.Replace("Bearer ", "");

            // читаємо JWT користувача
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            var userId = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "Invalid token: no user id" });

            var client = _httpFactory.CreateClient();
            var supabaseUrl = _config["SUPABASE_URL"];
            var supabaseKey = _config["SUPABASE_API_KEY"];

            // 🔹 Перевіряємо, що storage_place_id належить цьому користувачу
            var storageCheckUrl = $"{supabaseUrl}/rest/v1/storage_places?id=eq.{dto.Storage_Place_Id}&user_id=eq.{userId}";
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apikey", supabaseKey);
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", supabaseKey); // сервісний ключ для bypass RLS
            var storageCheckResp = await client.GetAsync(storageCheckUrl);
            var storageJson = await storageCheckResp.Content.ReadAsStringAsync();
            if (!storageCheckResp.IsSuccessStatusCode || storageJson == "[]")
                return BadRequest(new
                {
                    error = "Invalid storage_place_id for this user",
                    storagePlaceId = dto.Storage_Place_Id,
                    userId,
                    storageResponse = storageJson,
                    storageCheckUrl
                });

            // 🔹 формуємо payload для вставки
            var body = new
            {
                name = dto.Name,
                quantity = dto.Quantity,
                unit = dto.Unit,
                expiration_date = dto.Expiration_Date,
                storage_place_id = dto.Storage_Place_Id,
                user_id = userId
            };

            var url = $"{supabaseUrl}/rest/v1/products";
            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apikey", supabaseKey);
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", supabaseKey); // сервісний ключ
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
