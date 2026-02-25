using myFridge.Services.Interfaces;
using System.Net.Http.Headers;
namespace myFridge.Services;

public class StoragePlaceService : IStoragePlaceService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly string? _supabaseUrl;
    private readonly string? _supabaseKey;

    public StoragePlaceService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
        _supabaseUrl = _config["SUPABASE_URL"];
        _supabaseKey = _config["SUPABASE_API_KEY"];
    }

    public async Task<string> GetMyStoragePlacesAsync(string token)
    {
        var url = $"{_supabaseUrl}/rest/v1/storage_places?select=*";
        return await SendRequestAsync(url, token);
    }

    public async Task<string> GetProductsInStoragePlaceAsync(string token, Guid storagePlaceId)
    {

        var url = $"{_supabaseUrl}/rest/v1/products?storage_place_id=eq.{storagePlaceId}&select=*";
        return await SendRequestAsync(url, token);
    }

    private async Task<string> SendRequestAsync(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("apikey", _supabaseKey);

        var response = await _httpClient.SendAsync(request);

        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Supabase Error: {response.StatusCode} - {content}");
        }
        return content;
    }
}