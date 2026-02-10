using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace myFridge.api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IHttpClientFactory _factory;
        private readonly IConfiguration _config;

        public ProductsController(IHttpClientFactory factory, IConfiguration config)
        {
            _factory = factory;
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var client = _factory.CreateClient();

                var baseUrl = _config["Supabase:Url"];
                var apiKey = _config["Supabase:ApiKey"];

                var url = $"{baseUrl}/rest/v1/products?select=name,quantity,unit,expiration_date,users(email),storage_places(name)";

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("apikey", apiKey);
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode,
                        await response.Content.ReadAsStringAsync());

                var json = await response.Content.ReadAsStringAsync();

                // Можеш потім замінити на DTO
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
