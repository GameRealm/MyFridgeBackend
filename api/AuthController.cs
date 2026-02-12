using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using myFridge.DTOs.Users;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
namespace myFridge.api;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;

    public AuthController(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var supabaseUrl = _config["SUPABASE_URL"];
        var supabaseKey = _config["SUPABASE_API_KEY"];

        var client = _httpFactory.CreateClient();

        var url = $"{supabaseUrl}/auth/v1/token?grant_type=password";

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("apikey", supabaseKey);

        var json = JsonSerializer.Serialize(dto);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);
        var result = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, result);

        return Ok(JsonSerializer.Deserialize<object>(result));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var supabaseUrl = _config["SUPABASE_URL"];
        var supabaseKey = _config["SUPABASE_API_KEY"];

        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            return StatusCode(500, "Supabase settings missing");

        var client = _httpFactory.CreateClient();

        // 🔥 signup в Auth
        var url = $"{supabaseUrl}/auth/v1/signup";

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("apikey", supabaseKey);

        var body = new
        {
            email = dto.Email,
            password = dto.Password
        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);
        var resp = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, resp);

        using var doc = JsonDocument.Parse(resp);
        var root = doc.RootElement;

        if (!root.TryGetProperty("user", out var userElement))
            return BadRequest("User not returned. Possibly email confirmation required.");

        var userId = userElement.GetProperty("id").GetString();

        string? accessToken = null;
        if (root.TryGetProperty("session", out var sessionElement))
            accessToken = sessionElement.GetProperty("access_token").GetString();

        // 🔥 вставка в таблицю users через сервісний ключ
        var usersClient = _httpFactory.CreateClient();
        usersClient.DefaultRequestHeaders.Clear();
        usersClient.DefaultRequestHeaders.Add("apikey", supabaseKey);
        usersClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", supabaseKey);
        usersClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

        var usersUrl = $"{supabaseUrl}/rest/v1/users";

        var usersBody = new
        {
            id = userId,
            email = dto.Email,
            password_hash = "", // або null
            created_at = DateTime.UtcNow
        };

        var usersJson = JsonSerializer.Serialize(usersBody);
        var usersContent = new StringContent(usersJson, Encoding.UTF8, "application/json");

        var usersResp = await usersClient.PostAsync(usersUrl, usersContent);

        if (!usersResp.IsSuccessStatusCode)
        {
            var err = await usersResp.Content.ReadAsStringAsync();
            Console.WriteLine("USERS TABLE ERROR: " + err);
            // можна повернути повідомлення фронту або просто логувати
        }

        return Ok(new
        {
            access_token = accessToken,
            user_id = userId,
            message = accessToken != null ? "Registration successful" : "Registration successful. Confirm email."
        });
    }

}
