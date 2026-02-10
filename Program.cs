using myFridge.Db;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddHttpClient();

var app = builder.Build();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://*:{port}");

// Перевірка підключення до бази (тільки повідомлення в консоль)
using (var scope = app.Services.CreateScope())
{
    var conn = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
    try
    {
        conn.Open();
        Console.WriteLine("✅ Підключення до бази успішне!");
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Не вдалося підключитися до бази: " + ex.Message);
    }
    finally
    {
        conn.Close();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => "API is running");
app.Run();
