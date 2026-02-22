namespace myFridge.Services.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Шукає продукти, що прострочаться завтра, і розсилає Push-сповіщення.
    /// Повертає кількість успішно відправлених сповіщень.
    /// </summary>
    Task<int> SendDailyRemindersAsync();
}
