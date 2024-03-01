using Minesweeper.Api;
using Minesweeper.Interfaces;

namespace ComponentTestMinesweeper.Mocks;
public class MockNotificationService : INotificationService
{
    public Task<bool> SendNotification(string type)
    { return Task.FromResult(type == "HRA_VYTVOŘENA"); }
    public Task<bool> SendNotification(GameInputDto type) => throw new NotImplementedException();
}
