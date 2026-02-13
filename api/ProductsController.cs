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

        // Отримуємо userId з токена користувача
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return Unauthorized(new { error = "Missing or invalid Authorization header" });

        var userToken = authHeader.Substring("Bearer ".Length).Trim();
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(userToken);
        var userId = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { error = "Invalid token: no user id" });

        try
        {
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apikey", supabaseKey);
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", supabaseKey); // сервісний ключ

            // 🔹 select з join на users та storage_places, і фільтр по конкретному користувачу
            var url = $"{supabaseUrl}/rest/v1/products?select=id,name,quantity,unit,expiration_date,users(id,email),storage_places(id,name)&user_id=eq.{userId}&order=created_at.asc";

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
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return Unauthorized(new { error = "Missing Authorization header" });

        var token = authHeader.Substring("Bearer ".Length).Trim();

        // 👉 читаємо JWT
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var userIdFromToken = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        Console.WriteLine("=================================");
        Console.WriteLine("TOKEN USER ID: " + userIdFromToken);
        Console.WriteLine("PRODUCT ID: " + id);
        Console.WriteLine("=================================");

        var client = _httpFactory.CreateClient();

        var supabaseUrl = _config["SUPABASE_URL"];
        var supabaseKey = _config["SUPABASE_API_KEY"];

        // =====================================================
        // 🔥 1. дивимось що в БАЗІ через service key (без RLS)
        // =====================================================
        var adminClient = _httpFactory.CreateClient();
        adminClient.DefaultRequestHeaders.Clear();
        adminClient.DefaultRequestHeaders.Add("apikey", supabaseKey);
        adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", supabaseKey);

        var adminUrl = $"{supabaseUrl}/rest/v1/products?id=eq.{id}&select=user_id";

        var adminResp = await adminClient.GetAsync(adminUrl);
        var adminJson = await adminResp.Content.ReadAsStringAsync();

        Console.WriteLine("DB RESPONSE (SERVICE ROLE): " + adminJson);

        // =====================================================
        // 🔥 2. тепер пробуємо як користувач
        // =====================================================
        var url = $"{supabaseUrl}/rest/v1/products?id=eq.{id}&select=name,quantity,unit,expiration_date,users(email),storage_places(name)";

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("apikey", supabaseKey);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(url);
        var json = await response.Content.ReadAsStringAsync();

        Console.WriteLine("USER RESPONSE (WITH RLS): " + json);
        Console.WriteLine("=================================");

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, json);

        var products = JsonSerializer.Deserialize<List<object>>(json);

        if (products == null || products.Count == 0)
            return NotFound(new { error = "Product not found or not yours" });

        return Ok(products[0]);
    }


    //Good
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
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return Unauthorized(new { error = "Missing Authorization header" });

        var token = authHeader.Substring("Bearer ".Length).Trim();

        var supabaseUrl = _config["SUPABASE_URL"];
        var supabaseKey = _config["SUPABASE_API_KEY"];

        try
        {
            var client = _httpFactory.CreateClient();

            var url = $"{supabaseUrl}/rest/v1/products?id=eq.{id}";

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apikey", supabaseKey);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token); // 🔥 USER TOKEN
            client.DefaultRequestHeaders.Add("Prefer", "return=representation");

            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PatchAsync(url, content);

            var resp = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, resp);

            if (resp == "[]")
                return NotFound(new { error = "Product not found or not yours" });

            return Ok(JsonSerializer.Deserialize<object>(resp));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        try
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader))
                return Unauthorized(new { error = "Missing Authorization header" });

            var token = authHeader.Replace("Bearer ", "");

            var client = _httpFactory.CreateClient();
            var supabaseUrl = _config["SUPABASE_URL"];

            var deleteUrl = $"{supabaseUrl}/rest/v1/products?id=eq.{id}";
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token); // JWT користувача
            client.DefaultRequestHeaders.Add("apikey", _config["SUPABASE_API_KEY"]); // REST API key для запиту
            client.DefaultRequestHeaders.Add("Prefer", "return=representation"); // щоб DELETE повертав JSON

            var deleteResp = await client.DeleteAsync(deleteUrl);
            var deleteJson = await deleteResp.Content.ReadAsStringAsync();

            // Якщо Supabase повернув порожній рядок
            if (string.IsNullOrWhiteSpace(deleteJson))
            {
                return Ok(new { message = "Deleted successfully or nothing to delete" });
            }

            return Ok(JsonSerializer.Deserialize<object>(deleteJson));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> GetProducts([FromQuery] string? search = null)
    {
        try
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader))
                return Unauthorized(new { error = "Missing Authorization header" });

            var token = authHeader.Replace("Bearer ", "");

            var client = _httpFactory.CreateClient();
            var supabaseUrl = _config["SUPABASE_URL"];

            // Формуємо URL для запиту
            var url = $"{supabaseUrl}/rest/v1/products";
            if (!string.IsNullOrWhiteSpace(search))
            {
                url += $"?name=ilike.*{search}*";
            }

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token); 
            client.DefaultRequestHeaders.Add("apikey", _config["SUPABASE_API_KEY"]);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var resp = await client.GetAsync(url);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return StatusCode((int)resp.StatusCode, json);

            if (string.IsNullOrWhiteSpace(json))
                return Ok(new List<object>());

            return Ok(JsonSerializer.Deserialize<object>(json));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // GET /products/expiring?days=
    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiringProducts([FromQuery] int days = 3)
    {
        try
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader))
                return Unauthorized(new { error = "Missing Authorization header" });

            var token = authHeader.Replace("Bearer ", "");

            var client = _httpFactory.CreateClient();
            var supabaseUrl = _config["SUPABASE_URL"];

            // Формуємо URL для фільтру по даті закінчення
            // PostgreSQL: expiration_date <= CURRENT_DATE + interval 'days'
            var url = $"{supabaseUrl}/rest/v1/products?expiration_date=lte.{DateTime.UtcNow.AddDays(days):yyyy-MM-dd}";

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token); // JWT користувача
            client.DefaultRequestHeaders.Add("apikey", _config["SUPABASE_API_KEY"]);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var resp = await client.GetAsync(url);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return StatusCode((int)resp.StatusCode, json);

            if (string.IsNullOrWhiteSpace(json))
                return Ok(new List<object>());

            return Ok(JsonSerializer.Deserialize<object>(json));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
