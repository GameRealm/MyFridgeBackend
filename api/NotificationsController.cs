using Microsoft.AspNetCore.Mvc;
using myFridge.Services.Interfaces;

namespace myFridge.Controllers; // Переконайся, що тут твій правильний namespace

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly string _expectedCronKey;

    public NotificationsController(INotificationService notificationService, IConfiguration config)
    {
        _notificationService = notificationService;

        _expectedCronKey = config["CRON_SECRET_KEY"]
            ?? throw new ArgumentNullException("CRON_SECRET_KEY відсутній у налаштуваннях");
    }

    [HttpPost("send-daily-reminders")]
    // 1. ОНОВЛЕННЯ: Тепер .NET сам шукає заголовок і каже Swagger-у створити для нього поле!
    public async Task<IActionResult> SendDailyReminders([FromHeader(Name = "X-Cron-Key")] string? providedKey)
    {
        // ТИМЧАСОВИЙ ДЕБАГ: виводимо в консоль те, що порівнюємо
        Console.WriteLine($"Очікуємо: '{_expectedCronKey}'");
        Console.WriteLine($"Прийшло: '{providedKey}'");

        // 2. ЗАХИСТ: Перевіряємо, чи збігаються ключі
        if (providedKey != _expectedCronKey)
        {
            return Unauthorized(new { message = "Invalid Cron Key" });
        }

        // Якщо ключі збіглися — йдемо в сервіс
        var sentCount = await _notificationService.SendDailyRemindersAsync();

        return Ok(new { message = $"Успішно відправлено сповіщень: {sentCount}" });
    }
}