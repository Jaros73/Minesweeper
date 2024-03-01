using Minesweeper.Api;
namespace Minesweeper.Interfaces;

public interface INotificationService
{
    Task<bool> SendNotification(string type);
    public Task<bool> SendNotification(GameInputDto type) => throw new NotImplementedException();
}
