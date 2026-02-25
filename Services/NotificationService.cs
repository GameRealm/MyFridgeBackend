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

    public NotificationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        _supabaseUrl = configuration["SUPABASE_URL"] ?? throw new ArgumentNullException("SUPABASE_URL відсутній у налаштуваннях");
        _supabaseKey = configuration["SUPABASE_API_KEY"] ?? throw new ArgumentNullException("SUPABASE_KEY відсутній у налаштуваннях");
    }

    public async Task<int> SendDailyRemindersAsync()
    {
        var tomorrowString = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");
        var url = $"{_supabaseUrl}/rest/v1/products?expiration_date=eq.{tomorrowString}&select=name,users(push_token)";

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Add("apikey", _supabaseKey);
        requestMessage.Headers.Add("Authorization", $"Bearer {_supabaseKey}");

        var response = await _httpClient.SendAsync(requestMessage);
        if (!response.IsSuccessStatusCode) return 0;

        var json = await response.Content.ReadAsStringAsync();
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var expiringProducts = JsonSerializer.Deserialize<List<NotificationProductDto>>(json, jsonOptions);

        if (expiringProducts == null || !expiringProducts.Any()) return 0;

        var groupedNotifications = expiringProducts
            .Where(p => !string.IsNullOrEmpty(p.User?.PushToken))
            .GroupBy(p => p.User!.PushToken);

        var expoApiUrl = "https://exp.host/--/api/v2/push/send";
        int sentMessagesCount = 0;

        foreach (var group in groupedNotifications)
        {
            var token = group.Key;
            var productNames = string.Join(", ", group.Select(p => p.Name));

            var payload = new
            {
                to = token,
                title = "⏳ Термін придатності спливає!",
                body = $"Завтра прострочиться: {productNames}. Використайте їх швидше!"
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var expoResponse = await _httpClient.PostAsync(expoApiUrl, content);

            if (expoResponse.IsSuccessStatusCode)
            {
                sentMessagesCount++;
            }
            else
            {
                var error = await expoResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Помилка відправки в Expo для {token}: {error}");
            }
        }
        return sentMessagesCount;
    }
}