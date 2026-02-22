using Microsoft.AspNetCore.Mvc;
using myFridge.Services.Interfaces;

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
    public async Task<IActionResult> SendDailyReminders()
    {
        var providedKey = Request.Headers["X-Cron-Key"].FirstOrDefault();

        // ТИМЧАСОВИЙ ДЕБАГ: виводимо в консоль те, що порівнюємо
        Console.WriteLine($"Очікуємо: '{_expectedCronKey}'");
        Console.WriteLine($"Прийшло: '{providedKey}'");

        if (providedKey != _expectedCronKey)
        {
            return Unauthorized(new { message = "Invalid Cron Key" });
        }

        var sentCount = await _notificationService.SendDailyRemindersAsync();
        return Ok(new { message = $"Успішно відправлено сповіщень: {sentCount}" });
    }
}