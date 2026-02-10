using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Headers;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Додаємо Environment Variables у Configuration
builder.Configuration.AddEnvironmentVariables();

// Додаємо контролери
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Додаємо HttpClient для REST API
builder.Services.AddHttpClient();

var app = builder.Build();

// Swagger для тестів локально
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyFridge API v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Простий ендпоінт для перевірки
app.MapGet("/", () => "API is running");

// --- Лог підключення до Supabase ---
using (var scope = app.Services.CreateScope())
{
    var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
    var client = httpFactory.CreateClient();

    var supabaseUrl = builder.Configuration["SUPABASE_URL"];
    var supabaseKey = builder.Configuration["SUPABASE_API_KEY"];

    Console.WriteLine("SUPABASE_URL=" + Environment.GetEnvironmentVariable("SUPABASE_URL"));
    Console.WriteLine("SUPABASE_API_KEY=" + Environment.GetEnvironmentVariable("SUPABASE_API_KEY"));


    if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
    {
        Console.WriteLine("❌ SUPABASE_URL або SUPABASE_API_KEY не встановлені у Environment Variables.");
    }
    else
    {
        try
        {
            var url = $"{supabaseUrl}/rest/v1/products?select=id";
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apikey", supabaseKey);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);

            var response = client.GetAsync(url).Result;

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Підключення до Supabase REST API успішне!");
            }
            else
            {
                Console.WriteLine($"❌ Не вдалося підключитися до Supabase. StatusCode: {response.StatusCode}");
                var body = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine(body);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Не вдалося підключитися до Supabase: " + ex.Message);
        }
    }
}

app.Run();
