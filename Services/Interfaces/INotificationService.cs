namespace myFridge.Services.Interfaces;

public interface INotificationService
{
    Task<int> SendDailyRemindersAsync();
}
