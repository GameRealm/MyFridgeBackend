using myFridge.DTOs.Users;
using myFridge.Services.Interfaces;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace myFridge.Services
{
    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _supabaseUrl;
        private readonly string? _supabaseKey;

        public UserService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _supabaseUrl = config["SUPABASE_URL"];
            _supabaseKey = config["SUPABASE_API_KEY"];
        }

        public async Task<string> GetUserProfileAsync(string token, string userId)
        {
            var url = $"{_supabaseUrl}/rest/v1/users?id=eq.{userId}&select=id,email,created_at";
            return await SendRequestAsync(HttpMethod.Get, url, token);
        }

        public async Task<string> CreateProfileAsync(string token, string userId, UserDto dto)
        {
            var url = $"{_supabaseUrl}/rest/v1/users";

            var body = new
            {
                id = userId,
                email = dto.Email
                // Тут можна додати Name, Avatar тощо в майбутньому
            };

            return await SendRequestAsync(HttpMethod.Post, url, token, body);
        }

        public async Task<string> DeleteProfileAsync(string token, string userId)
        {
            var url = $"{_supabaseUrl}/rest/v1/users?id=eq.{userId}";
            return await SendRequestAsync(HttpMethod.Delete, url, token);
        }

        // Універсальний метод для відправки запитів
        private async Task<string> SendRequestAsync(HttpMethod method, string url, string token, object? body = null)
        {
            var request = new HttpRequestMessage(method, url);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("apikey", _supabaseKey);
            request.Headers.Add("Prefer", "return=representation"); // Щоб Supabase повертав змінені дані

            if (body != null)
            {
                var json = JsonSerializer.Serialize(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Supabase Error ({response.StatusCode}): {content}");
            }

            return content;
        }
    }
}