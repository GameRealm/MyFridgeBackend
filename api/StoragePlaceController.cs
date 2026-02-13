using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace myFridge.api;

[ApiController]
[Route("api/[controller]")]
public class StoragePlaceController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;

    public StoragePlaceController(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _config = config;
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetMyStoragePlaces()
    {
        try
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader))
                return Unauthorized(new { error = "Missing Authorization header" });

            var token = authHeader.Replace("Bearer ", "");

            var client = _httpFactory.CreateClient();
            var supabaseUrl = _config["SUPABASE_URL"];

            var url = $"{supabaseUrl}/rest/v1/storage_places";

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
        [HttpGet ("products")]
        public async Task<IActionResult> GetProducts([FromQuery] Guid storagePlaceId)
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authHeader))
                    return Unauthorized(new { error = "Missing Authorization header" });

                var token = authHeader.Replace("Bearer ", "");

                var client = _httpFactory.CreateClient();
                var supabaseUrl = _config["SUPABASE_URL"];

                var url = $"{supabaseUrl}/rest/v1/products?storage_place_id=eq.{storagePlaceId}";

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
