using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using myFridge.DTOs.Users;
using myFridge.Services.Interfaces;

namespace myFridge.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _client;
    private readonly string _supabaseUrl;

    // Використовуємо ТІЛЬКИ секретний ключ для всіх операцій
    private readonly string _serviceRoleKey;

    private readonly JsonSerializerOptions _jsonOptions;

    public AuthService(HttpClient client, IConfiguration config)
    {
        _client = client;

        // Перевіряємо наявність URL
        _supabaseUrl = config["SUPABASE_URL"]
                       ?? throw new ArgumentNullException("SUPABASE_URL is missing");

        // 🔥 Беремо саме SERVICE_ROLE_KEY. 
        // Якщо ви не додали нову змінну, можете тимчасово читати зі старої, 
        // АЛЕ там має лежати саме секретний ключ (service_role), а не anon.
        _serviceRoleKey = config["SUPABASE_SERVICE_ROLE_KEY"]
                          ?? config["SUPABASE_API_KEY"]
                          ?? throw new ArgumentNullException("SUPABASE_SERVICE_ROLE_KEY is missing");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var url = $"{_supabaseUrl}/auth/v1/token?grant_type=password";

        var body = new
        {
            email = dto.Email,
            password = dto.Password
        };

        // Використовуємо секретний ключ для запиту
        var request = CreateRequest(HttpMethod.Post, url, body);

        var response = await _client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Login failed: {GetErrorMessage(json)}");
        }

        return JsonSerializer.Deserialize<AuthResponseDto>(json, _jsonOptions)!;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        // 1. Реєстрація в Auth (Supabase Auth)
        var authUrl = $"{_supabaseUrl}/auth/v1/signup";

        var body = new
        {
            email = dto.Email,
            password = dto.Password,
            // Можна додати автоматичне підтвердження пошти, якщо ви адмін
            // data = new { ... } 
        };

        var request = CreateRequest(HttpMethod.Post, authUrl, body);
        var response = await _client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Registration failed: {GetErrorMessage(json)}");
        }

        var authResponse = JsonSerializer.Deserialize<AuthResponseDto>(json, _jsonOptions);
        var userId = authResponse?.User?.Id.ToString();

        // Іноді Supabase повертає User, але не повертає Token (якщо треба конфірм пошти)
        if (string.IsNullOrEmpty(userId))
        {
            // Спробуємо дістати ID з IdToken або іншого поля, якщо структура відрізняється
            // Але зазвичай User.Id є завжди при успіху 200 OK
            throw new Exception("User created but ID not returned from Supabase.");
        }

        // 2. Створюємо запис у вашій таблиці 'users'
        // Оскільки ми використовуємо _serviceRoleKey, RLS ігнорується -> помилки доступу зникнуть
        await SyncUserTableAsync(userId, dto.Email!);

        // 3. Створюємо дефолтні холодильники
        await CreateDefaultStoragePlacesAsync(userId);

        // Формуємо відповідь
        if (string.IsNullOrEmpty(authResponse?.AccessToken))
        {
            authResponse ??= new AuthResponseDto();
            authResponse.Message = "Registration successful. Please check your email to confirm account.";
        }

        return authResponse!;
    }

    private async Task SyncUserTableAsync(string userId, string email)
    {
        // 1. Перевіряємо чи є юзер
        var checkRequest = CreateRequest(HttpMethod.Get, $"{_supabaseUrl}/rest/v1/users?id=eq.{userId}");
        var checkResp = await _client.SendAsync(checkRequest);
        var checkJson = await checkResp.Content.ReadAsStringAsync();

        if (checkJson != "[]" && !string.IsNullOrWhiteSpace(checkJson))
        {
            return;
        }

        // 2. Створюємо об'єкт
        var userBody = new
        {
            id = userId,
            email = email,
            created_at = DateTime.UtcNow
        };

        var postRequest = CreateRequest(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/users", userBody);
        var response = await _client.SendAsync(postRequest);

        // 🔥 ЧИТАЄМО ВІДПОВІДЬ
        var errorBody = await response.Content.ReadAsStringAsync();

        // 4. ЯКЩО ПОМИЛКА - КИДАЄМО EXCEPTION НАГОРУ
        if (!response.IsSuccessStatusCode)
        {

            throw new Exception($"DB SYNC ERROR ({response.StatusCode}): {errorBody}");
        }
    }

    private async Task CreateDefaultStoragePlacesAsync(string userId)
    {
        var storagePlaces = new[]
        {
            new { name = "Холодильник", user_id = userId },
            new { name = "Морозилка", user_id = userId },
            new { name = "Комора", user_id = userId }
        };

        var url = $"{_supabaseUrl}/rest/v1/storage_places";
        var request = CreateRequest(HttpMethod.Post, url, storagePlaces);

        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error creating default storages: {err}");
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);

        request.Headers.Add("apikey", _serviceRoleKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);

        if (body != null)
        {
            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            if (method == HttpMethod.Post)
            {
                request.Headers.Add("Prefer", "return=minimal");
            }
        }

        return request;
    }

    private string GetErrorMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error_description", out var desc))
                return desc.GetString() ?? "Unknown error";
            if (doc.RootElement.TryGetProperty("msg", out var msg))
                return msg.GetString() ?? "Unknown error";
            if (doc.RootElement.TryGetProperty("message", out var message))
                return message.GetString() ?? "Unknown error";

            return json;
        }
        catch
        {
            return "Unknown server error";
        }
    }
}