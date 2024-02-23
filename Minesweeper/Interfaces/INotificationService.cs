using Minesweeper.Api;

namespace Minesweeper.Interfaces;

public interface INotificationService
{
    Task<bool> SendNotification(string zprava);
    public Task<bool> SendNotification(GameInputDto type) => throw new NotImplementedException();
}
