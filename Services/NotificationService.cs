using Microsoft.Extensions.Configuration;
using myFridge.Services.Interfaces;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using myFridge.DTOs.Notification;
namespace myFridge.Services;

public class NotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;

    // Інжектимо IConfiguration, щоб дістати URL та Key для Supabase з appsettings.json
    public NotificationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        _supabaseUrl = configuration["SUPABASE_URL"] ?? throw new ArgumentNullException("SUPABASE_URL відсутній у налаштуваннях");
        _supabaseKey = configuration["SUPABASE_KEY"] ?? throw new ArgumentNullException("SUPABASE_KEY відсутній у налаштуваннях");
    }

    public async Task<int> SendDailyRemindersAsync()
    {
        // 1. Визначаємо завтрашню дату у форматі YYYY-MM-DD
        var tomorrowString = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

        // 2. Формуємо URL для Supabase.
        // eq.{tomorrowString} - фільтр по даті
        // select=name,users(push_token) - беремо назву продукту та підтягуємо токен користувача
        var url = $"{_supabaseUrl}/rest/v1/products?expiration_date=eq.{tomorrowString}&select=name,users(push_token)";

        // Налаштовуємо заголовки для Supabase
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Add("apikey", _supabaseKey);
        requestMessage.Headers.Add("Authorization", $"Bearer {_supabaseKey}");

        // 3. Робимо запит до БД
        var response = await _httpClient.SendAsync(requestMessage);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Помилка отримання продуктів: {response.StatusCode}");
            return 0;
        }

        var json = await response.Content.ReadAsStringAsync();

        // Десеріалізуємо відповідь
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var expiringProducts = JsonSerializer.Deserialize<List<NotificationProductDto>>(json, jsonOptions);

        if (expiringProducts == null || !expiringProducts.Any()) return 0;

        // 4. Відправка повідомлень через Expo
        var expoApiUrl = "https://exp.host/--/api/v2/push/send";
        int sentMessagesCount = 0;

        foreach (var product in expiringProducts)
        {
            // Перевіряємо, чи є у користувача токен взагалі
            var pushToken = product.User?.PushToken;
            if (string.IsNullOrEmpty(pushToken)) continue;

            var payload = new
            {
                to = pushToken,
                title = "⏳ Термін придатності спливає!",
                body = $"Завтра прострочиться: {product.Name}. Використайте його швидше!"
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var expoResponse = await _httpClient.PostAsync(expoApiUrl, content);

            if (expoResponse.IsSuccessStatusCode)
            {
                sentMessagesCount++;
            }
            else
            {
                Console.WriteLine($"Помилка відправки для {pushToken}: {expoResponse.StatusCode}");
            }
        }

        return sentMessagesCount;
    }
}