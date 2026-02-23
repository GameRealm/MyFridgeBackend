using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myFridge.Services.Interfaces;
namespace myFridge.Controllers; 

[AllowAnonymous]
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
    public async Task<IActionResult> SendDailyReminders([FromHeader(Name = "X-Cron-Key")] string? providedKey)
    {
        if (providedKey != _expectedCronKey)
        {
            return Unauthorized();
        }

        var sentCount = await _notificationService.SendDailyRemindersAsync();
        return NoContent();
    }
}
