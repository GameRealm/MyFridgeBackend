using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace myFridge.api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _config;

        public ProductsController(IHttpClientFactory httpFactory, IConfiguration config)
        {
            _httpFactory = httpFactory;
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var client = _httpFactory.CreateClient();

                var supabaseUrl = _config["SUPABASE_URL"];
                var supabaseKey = _config["SUPABASE_API_KEY"];

                if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
                    return StatusCode(500, new { error = "Supabase URL or API key not set in environment variables" });

                // REST API для Supabase
                var url = $"{supabaseUrl}/rest/v1/products?select=name,quantity,unit,expiration_date,users(email),storage_places(name)";

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("apikey", supabaseKey);
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", supabaseKey);

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<object>(json);

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
