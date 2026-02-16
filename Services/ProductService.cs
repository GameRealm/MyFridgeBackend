using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using myFridge.DTOs.Products;
using myFridge.Services.Interfaces;

namespace myFridge.Services;

public class ProductService : IProductService
{
    private readonly HttpClient _httpClient;
    private readonly string? _supabaseUrl;
    private readonly string? _supabaseKey;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProductService(HttpClient httpClient, IConfiguration config, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _supabaseUrl = config["SUPABASE_URL"];
        _supabaseKey = config["SUPABASE_API_KEY"];

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    // 🛠️ HELPER: Налаштування клієнта для кожного запиту
    private void PrepareClientHeaders(bool useServiceKey = false)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (useServiceKey)
        {
            // Використовуємо Service Key (обхід RLS)
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _supabaseKey);
        }
        else
        {
            // Використовуємо токен користувача з поточного запиту
            var context = _httpContextAccessor.HttpContext;
            if (context != null && context.Request.Headers.ContainsKey("Authorization"))
            {
                var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }
    }

    // 🔹 GET ALL
    // Repositories/ProductRepository.cs

    public async Task<List<ProductDto>> GetProductsAsync(string userId, ProductFilterDto filter)
    {
        PrepareClientHeaders();

        // 1. Базовий URL з обов'язковими фільтрами
        var urlBuilder = new StringBuilder();
        urlBuilder.Append($"{_supabaseUrl}/rest/v1/products?");
        urlBuilder.Append("select=id,name,quantity,unit,expiration_date,is_favorite,users(id,email),storage_places(id,name)");
        urlBuilder.Append($"&user_id=eq.{userId}");
        urlBuilder.Append("&is_deleted=eq.false");

        // 2. Динамічне додавання фільтрів

        // Пошук
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            urlBuilder.Append($"&name=ilike.*{filter.SearchTerm}*");
        }

        // Улюблене
        if (filter.IsFavorite.HasValue)
        {
            urlBuilder.Append($"&is_favorite=eq.{filter.IsFavorite.Value.ToString().ToLower()}");
        }

        // Місце зберігання
        if (filter.StorageId.HasValue)
        {
            urlBuilder.Append($"&storage_place_id=eq.{filter.StorageId}");
        }

        // Логіка дат (Expiring Days або Category)
        var today = DateTime.UtcNow.Date;

        if (filter.ExpiringInDays.HasValue)
        {
            var targetDate = today.AddDays(filter.ExpiringInDays.Value).ToString("yyyy-MM-dd");
            urlBuilder.Append($"&expiration_date=lte.{targetDate}");
        }
        else if (!string.IsNullOrEmpty(filter.ExpirationCategory))
        {
            switch (filter.ExpirationCategory.ToLower())
            {
                case "soon": // 0-3 дні
                    var soonEnd = today.AddDays(3).ToString("yyyy-MM-dd");
                    urlBuilder.Append($"&expiration_date=lte.{soonEnd}");
                    break;
                case "medium": // 4-10 днів
                    var mStart = today.AddDays(4).ToString("yyyy-MM-dd");
                    var mEnd = today.AddDays(10).ToString("yyyy-MM-dd");
                    urlBuilder.Append($"&expiration_date=gte.{mStart}&expiration_date=lte.{mEnd}");
                    break;
                case "later": // >10 днів
                    var lStart = today.AddDays(10).ToString("yyyy-MM-dd");
                    urlBuilder.Append($"&expiration_date=gt.{lStart}");
                    break;
            }
        }

        // 3. Сортування
        var sortOrder = filter.SortDescending ? "desc" : "asc";
        // Перевірка на валідні поля сортування, щоб уникнути SQL Injection (хоча Supabase захищений)
        var allowedSorts = new[] { "name", "created_at", "expiration_date", "quantity" };
        var sortBy = allowedSorts.Contains(filter.SortBy) ? filter.SortBy : "created_at";

        urlBuilder.Append($"&order={sortBy}.{sortOrder}");

        // 4. Виконуємо запит
        var response = await _httpClient.GetAsync(urlBuilder.ToString());
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ProductDto>>(json, _jsonOptions) ?? new List<ProductDto>();
    }

    // 🔹 GET BY ID
    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        PrepareClientHeaders();

        var url = $"{_supabaseUrl}/rest/v1/products?id=eq.{id}&select=*,users(email),storage_places(name)";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var products = JsonSerializer.Deserialize<List<ProductDto>>(json, _jsonOptions);

        return products?.FirstOrDefault();
    }

    // 🔹 CREATE
    public async Task<ProductDto?> CreateAsync(CreateProductDto dto, string userId)
    {
        // 1. Валідація Storage Place (як у вашому коді)
        // Тут можна використати User Token, якщо RLS налаштовано правильно, але залишу Service Key як у прикладі
        // або краще переробити на звичайний клієнт (безпечніше). 
        // Використаю тут звичайний клієнт, бо RLS має дозволити бачити свої storage_places.
        PrepareClientHeaders();

        var checkUrl = $"{_supabaseUrl}/rest/v1/storage_places?id=eq.{dto.Storage_Place_Id}";
        var checkResp = await _httpClient.GetAsync(checkUrl);
        var checkJson = await checkResp.Content.ReadAsStringAsync();

        if (!checkResp.IsSuccessStatusCode || checkJson == "[]")
            throw new Exception("Invalid storage_place_id or access denied");

        // 2. Створення
        var body = new
        {
            name = dto.Name,
            quantity = dto.Quantity,
            unit = dto.Unit,
            expiration_date = dto.Expiration_Date,
            storage_place_id = dto.Storage_Place_Id,
            user_id = userId
        };

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

        var response = await _httpClient.PostAsync($"{_supabaseUrl}/rest/v1/products", content);
        response.EnsureSuccessStatusCode();

        var respJson = await response.Content.ReadAsStringAsync();
        var createdList = JsonSerializer.Deserialize<List<ProductDto>>(respJson, _jsonOptions);
        return createdList?.FirstOrDefault();
    }

    // 🔹 UPDATE
    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto dto)
    {
        PrepareClientHeaders();

        var url = $"{_supabaseUrl}/rest/v1/products?id=eq.{id}";
        var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

        var response = await _httpClient.PatchAsync(url, content);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var updatedList = JsonSerializer.Deserialize<List<ProductDto>>(json, _jsonOptions);
        return updatedList?.FirstOrDefault();
    }

    // 🔹 DELETE (SMART LOGIC)
    public async Task<bool> DeleteSmartAsync(Guid id)
    {
        PrepareClientHeaders();

        // 1. Перевірка чи улюблений
        var checkUrl = $"{_supabaseUrl}/rest/v1/products?id=eq.{id}&select=is_favorite";
        var checkResp = await _httpClient.GetAsync(checkUrl);

        if (!checkResp.IsSuccessStatusCode) return false;

        var checkJson = await checkResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(checkJson);
        if (doc.RootElement.GetArrayLength() == 0) return false; // Не знайдено

        bool isFavorite = doc.RootElement[0].GetProperty("is_favorite").GetBoolean();

        // 2. Логіка видалення
        if (isFavorite)
        {
            // Soft Delete
            var updateUrl = $"{_supabaseUrl}/rest/v1/products?id=eq.{id}";
            var content = new StringContent(JsonSerializer.Serialize(new { is_deleted = true }), Encoding.UTF8, "application/json");
            var patchResp = await _httpClient.PatchAsync(updateUrl, content);
            return patchResp.IsSuccessStatusCode;
        }
        else
        {
            // Hard Delete
            var deleteUrl = $"{_supabaseUrl}/rest/v1/products?id=eq.{id}";
            var deleteResp = await _httpClient.DeleteAsync(deleteUrl);
            return deleteResp.IsSuccessStatusCode;
        }
    }

    // 🔹 SEARCH
    public async Task<List<ProductDto>> SearchAsync(string searchTerm)
    {
        PrepareClientHeaders();
        var url = $"{_supabaseUrl}/rest/v1/products?select=*";

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            url += $"&name=ilike.*{searchTerm}*";
        }

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ProductDto>>(json, _jsonOptions) ?? new List<ProductDto>();
    }

    // 🔹 GET EXPIRING
    public async Task<List<ProductDto>> GetExpiringAsync(int days)
    {
        PrepareClientHeaders();
        var targetDate = DateTime.UtcNow.AddDays(days).ToString("yyyy-MM-dd");
        var url = $"{_supabaseUrl}/rest/v1/products?expiration_date=lte.{targetDate}";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ProductDto>>(json, _jsonOptions) ?? new List<ProductDto>();
    }

    // 🔹 UPDATE FAVORITE
    public async Task<bool> UpdateFavoriteAsync(Guid id, bool isFavorite)
    {
        PrepareClientHeaders();
        var url = $"{_supabaseUrl}/rest/v1/products?id=eq.{id}";
        var content = new StringContent(JsonSerializer.Serialize(new { is_favorite = isFavorite }), Encoding.UTF8, "application/json");

        var response = await _httpClient.PatchAsync(url, content);
        return response.IsSuccessStatusCode;
    }

    // 🔹 GET FAVORITES
    public async Task<List<ProductDto>> GetFavoritesAsync(Guid? storageId)
    {
        PrepareClientHeaders();
        var url = $"{_supabaseUrl}/rest/v1/products?is_favorite=eq.true";
        if (storageId.HasValue) url += $"&storage_place_id=eq.{storageId}";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) return new List<ProductDto>();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ProductDto>>(json, _jsonOptions) ?? new List<ProductDto>();
    }

    // 🔹 COMPLEX FILTER (By Expiration Category)
    public async Task<List<ProductDto>> GetByExpirationCategoryAsync(string userId, string category, Guid? storageId, bool? favorite)
    {
        PrepareClientHeaders();

        var url = $"{_supabaseUrl}/rest/v1/products?user_id=eq.{userId}&is_deleted=eq.false";

        if (favorite.HasValue)
            url += $"&is_favorite=eq.{favorite.Value.ToString().ToLower()}";

        if (storageId.HasValue)
            url += $"&storage_place_id=eq.{storageId.Value}";

        var today = DateTime.UtcNow.Date;

        switch (category?.ToLower())
        {
            case "soon":
                var soonEnd = today.AddDays(3);
                url += $"&expiration_date=lte.{soonEnd:yyyy-MM-dd}";
                break;
            case "medium":
                var mediumStart = today.AddDays(4);
                var mediumEnd = today.AddDays(10);
                url += $"&expiration_date=gte.{mediumStart:yyyy-MM-dd}&expiration_date=lte.{mediumEnd:yyyy-MM-dd}";
                break;
            case "later":
                var laterStart = today.AddDays(10);
                url += $"&expiration_date=gt.{laterStart:yyyy-MM-dd}";
                break;
        }

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) return new List<ProductDto>();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ProductDto>>(json, _jsonOptions) ?? new List<ProductDto>();
    }
}