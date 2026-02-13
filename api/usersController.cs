using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myFridge.DTOs.Users;
using myFridge.Models;
using System.Text.Json;

[ApiController]
[Authorize]
[Route("api/users")]
public class usersController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;

    public usersController(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _config = config;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirst("sub")?.Value;
        if (userId == null)
            return Unauthorized();

        var supabaseUrl = _config["SUPABASE_URL"];
        var token = HttpContext.Request.Headers["Authorization"].ToString();

        var client = _httpFactory.CreateClient();

        var url = $"{supabaseUrl}/rest/v1/users?id=eq.{userId}&select=id,email,created_at";

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Authorization", token);

        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

        var json = await response.Content.ReadAsStringAsync();
        return Ok(JsonSerializer.Deserialize<object>(json));
    }

    [HttpPost("profile")]
    public async Task<IActionResult> CreateProfile(UserDto dto)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (userId == null)
            return Unauthorized();

        var supabaseUrl = _config["SUPABASE_URL"];
        var token = HttpContext.Request.Headers["Authorization"].ToString();

        var client = _httpFactory.CreateClient();

        var url = $"{supabaseUrl}/rest/v1/users";

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Authorization", token);
        client.DefaultRequestHeaders.Add("Prefer", "return=representation");

        var body = new
        {
            id = userId,
            email = dto.Email
        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadAsStringAsync();
        return Ok(JsonSerializer.Deserialize<object>(result));
    }

    [HttpDelete("profile")]
    public async Task<IActionResult> DeleteProfile()
    {
        var supabaseUrl = _config["SUPABASE_URL"];

        if (string.IsNullOrEmpty(supabaseUrl))
            return StatusCode(500, new { error = "Supabase URL not set" });

        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (userId == null)
                return Unauthorized();

            var client = _httpFactory.CreateClient();

            var url = $"{supabaseUrl}/rest/v1/users?id=eq.{userId}";

            // беремо токен користувача
            var token = HttpContext.Request.Headers["Authorization"].ToString();

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", token);
            client.DefaultRequestHeaders.Add("Prefer", "return=representation");

            var response = await client.DeleteAsync(url);

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
