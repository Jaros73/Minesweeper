using Minesweeper.Api;
using Minesweeper.Interfaces;
using Minesweeper.Pesristance;
using Minesweeper.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NodaTime.Testing;
using NodaTime;
using Xunit;
using Minesweeper.Structures;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace UnitTestMinesweeper;

public class GameServiceTest
{
    [Theory]
    [InlineData("//DROP TABLE Users;", "obsahuje nepovolené znaky")]
    [InlineData("'OR '1'='1", "obsahuje nepovolené znaky")]
    [InlineData("", "řetězec je prázdný")]
    [InlineData("$", "obsahuje nepovolené znaky")]
    public async Task Create_IncorretGameName_WithoutNotification(string gameName, string expectedErrorMessage)
    {
        var testDateTime = Instant.FromDateTimeUtc(new DateTime(2024, 2, 22, 13, 30, 30, DateTimeKind.Utc));
        await using var context = ApplicationContextTestFactory.CreateContext();

        var notifService1 = new Mock<INotificationService>();
        notifService1.Setup(mock => mock.SendNotification(It.Is<GameInputDto>(x => x.Name == gameName))).ReturnsAsync(true);
        var notifService2 = new Mock<INotificationService>();
        notifService2.Setup(mock => mock.SendNotification(It.IsAny<GameInputDto>())).ReturnsAsync(true);

        var sut = new GameService(context, new FakeClock(testDateTime), new[] { notifService1.Object, notifService2.Object });

        // Vytvoření instance GameInputDto s názvem hry
        var gameInputDto = new GameInputDto { Name = gameName };

        // Pokus o vytvoření hry s neplatným názvem by měl vyvolat ArgumentException
        var result = await Assert.ThrowsAsync<ArgumentException>(() => sut.Create(gameInputDto));

        Assert.NotNull(result);
        Assert.Empty(context.Games);
        Assert.Contains(expectedErrorMessage, result.Message); // Ověření, že zpráva výjimky obsahuje očekávanou chybovou zprávu
        notifService1.Verify(mock => mock.SendNotification(It.IsAny<GameInputDto>()), Times.Never);
        notifService2.Verify(mock => mock.SendNotification(It.IsAny<GameInputDto>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public async Task Create_IncorretMinesCount_WithoutNotification(int mines)
    {
        var testDateTime = Instant.FromDateTimeUtc(new DateTime(2024, 2, 22, 13, 30, 30, DateTimeKind.Utc));
        await using var context = ApplicationContextTestFactory.CreateContext();

        var notifService1 = new Mock<INotificationService>();
        notifService1.Setup(mock => mock.SendNotification(It.Is<GameInputDto>(x => x.MinesCount == mines))).ReturnsAsync(true);
        var notifService2 = new Mock<INotificationService>();
        notifService2.Setup(mock => mock.SendNotification(It.IsAny<GameInputDto>())).ReturnsAsync(true);
        var sut = new GameService
                    (
                      context,
                      new FakeClock(testDateTime),
                      new[] { notifService1.Object, notifService2.Object }
                    );
        // Vytvoření instance GameInputDto s počtem min
        var gameInputDto = new GameInputDto { MinesCount = mines };
        var result = await Assert.ThrowsAsync<ArgumentException>(() => sut.Create(gameInputDto));

        Assert.NotNull(result);
        Assert.Empty(context.Games);
        // Upravená kontrola chybové zprávy pro případ nesprávného počtu min
        Assert.Contains("řetězec je prázdný!", result.Message);
        notifService1.Verify(mock => mock.SendNotification(It.IsAny<GameInputDto>()), Times.Never);
        notifService2.Verify(mock => mock.SendNotification(It.IsAny<GameInputDto>()), Times.Never);
    }

    [Fact]
    public async Task Create_CorretGameName_NotificationYes()
    {
        var testDateTime = Instant.FromDateTimeUtc(new DateTime(2024, 2, 22, 13, 30, 30, DateTimeKind.Utc));
        await using var context = ApplicationContextTestFactory.CreateContext();

        var notifService1 = new Mock<INotificationService>();
        var message = "HRA_VYTVOŘENA";
        notifService1.Setup(mock => mock.SendNotification(It.Is<GameInputDto>(x => x.Name == message))).ReturnsAsync(true);


        var sut = new GameService
                    (
                      context,
                      new FakeClock(testDateTime),
                      new[] { notifService1.Object }
                    );

        var gameInputDto = new GameInputDto { Name = "Minolovka", MinesCount = 10 };
        await sut.Create(gameInputDto);

        Assert.NotEmpty(context.Games);

        notifService1.Verify(mock => mock.SendNotification("HRA_VYTVOŘENA"), Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(86)]
    [InlineData(99)]
    public async Task Create_CorretMinesCount_NotificationYes(int mines)
    {
        var testDateTime = Instant.FromDateTimeUtc(new DateTime(2024, 2, 22, 13, 30, 30, DateTimeKind.Utc));
        await using var context = ApplicationContextTestFactory.CreateContext();

        var notifService1 = new Mock<INotificationService>();
        var notifService2 = new Mock<INotificationService>();
        var sut = new GameService
                    (
                      context,
                      new FakeClock(testDateTime),
                      new[] { notifService1.Object, notifService2.Object }
                    );
        // Vytvoření instance GameInputDto s názvem hry a platným počtem min
        var gameInputDto = new GameInputDto { Name = "ValidGameName", MinesCount = mines };
        await sut.Create(gameInputDto);

        var gameExists = context.Games.Any(g => g.Name == "ValidGameName" && g.MinesCount == mines);
        Assert.True(gameExists, "Hra nebyla úspěšně vytvořena v databázi.");

        // Ověření, že byly odeslány notifikace
        notifService1.Verify(mock => mock.SendNotification(It.IsAny<string>()), Times.Once);
        notifService2.Verify(mock => mock.SendNotification(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Get_ReturnsGameDto_WhenGameExistsNotificationYes()
    {
        await using var context = ApplicationContextTestFactory.CreateContext();

        var testGame = new Game { Id = 1, Name = "Test Game", MinesCount = 10 };
        context.Games.Add(testGame);
        await context.SaveChangesAsync();

        var clockMock = new Mock<IClock>();
        var notifService = new Mock<INotificationService>();

        var service = new GameService(context, clockMock.Object, new[] { notifService.Object });

        var result = await service.Get(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Game", result.Name);

        notifService.Verify(s => s.SendNotification("HRA_ZÍSKÁNA"), Times.Once);
    }

    [Fact]
    public async Task Get_ReturnsGameDtoNull_WhenGameDoesNotExistWithoutNotification()
    {
        await using var context = ApplicationContextTestFactory.CreateContext();
        var clockMock = new Mock<IClock>();
        var notifService = new Mock<INotificationService>();

        var service = new GameService(context, clockMock.Object, new[] { notifService.Object });

        var nonExistentGameId = 999; 
        var result = await service.Get(nonExistentGameId);

        Assert.Null(result); 

        notifService.Verify(s => s.SendNotification(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetAllActive__WhenActiveGamesAreQueriedNotificationYes()
    {
        await using var context = ApplicationContextTestFactory.CreateContext();

        context.Games.Add(new Game { Id = 1, Name = "Active Game", MinesCount = 10, State = GameState.Active });
        context.Games.Add(new Game { Id = 2, Name = "Finished Game", MinesCount = 5, State = GameState.Finished });
        await context.SaveChangesAsync();

        var clockMock = new Mock<IClock>();

        var notifService1 = new Mock<INotificationService>();
        var notifService2 = new Mock<INotificationService>();
        var notificationServices = new[] { notifService1.Object, notifService2.Object };

        var service = new GameService(context, clockMock.Object, notificationServices);

        var results = await service.GetAllActive();

        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Contains(results, g => g.Name == "Active Game");

        notifService1.Verify(s => s.SendNotification("AKTIVNI_HRY_ZÍSKÁNY"), Times.Once);
        notifService2.Verify(s => s.SendNotification("AKTIVNI_HRY_ZÍSKÁNY"), Times.Once);
    }

    [Fact]
    public async Task GetAllActiveNull_WhenNoActiveGamesExistWithoutNotification()
    {
        await using var context = ApplicationContextTestFactory.CreateContext();

        context.Games.Add(new Game { Id = 1, Name = "Finished Game 1", MinesCount = 10, State = GameState.Finished });
        context.Games.Add(new Game { Id = 2, Name = "Finished Game 2", MinesCount = 5, State = GameState.Finished });
        await context.SaveChangesAsync();

        var clockMock = new Mock<IClock>();
        var notifService = new Mock<INotificationService>();
        var notificationServices = new[] { notifService.Object };

        var service = new GameService(context, clockMock.Object, notificationServices);

        var results = await service.GetAllActive();

        Assert.NotNull(results);
        Assert.Empty(results); 

        notifService.Verify(s => s.SendNotification(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task FindGameDto_WhenGameExistsNotificationYes()
    {
        await using var context = ApplicationContextTestFactory.CreateContext();
        var testDateTime = Instant.FromDateTimeUtc(new DateTime(2024, 2, 22, 13, 30, 30, DateTimeKind.Utc));
        var testGame = new Game { Id = 1, Name = "Existing Game", State = GameState.Active, EndDate = testDateTime.ToDateTimeUtc() };
        context.Games.Add(testGame);
        await context.SaveChangesAsync();

        var clockMock = new FakeClock(testDateTime);

        var notifService = new Mock<INotificationService>();
        var notificationServices = new[] { notifService.Object };

        var service = new GameService(context, clockMock, notificationServices);

        var result = await service.Find(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Existing Game", result.Name);

        notifService.Verify(s => s.SendNotification("HRA_NALEZENA"), Times.Once);
    }


    [Fact]
    public async Task FindGameDtoNull_WhenGameDoesNotExistWithoutNotification()
    {
        await using var context = ApplicationContextTestFactory.CreateContext();
        var clockMock = new Mock<IClock>();

        var notifService = new Mock<INotificationService>();
        var notificationServices = new[] { notifService.Object };

        var service = new GameService(context, clockMock.Object, notificationServices);
        var result = await service.Find(999); 

        Assert.Null(result);

        notifService.Verify(s => s.SendNotification(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Delete_AndRemovesGame_WhenGameExistsNotificationYes()
    {
        await using var context = ApplicationContextTestFactory.CreateContext();
        var testGame = new Game { Id = 1, Name = "Game To Delete", State = GameState.Active };
        context.Games.Add(testGame);
        await context.SaveChangesAsync();

        var clockMock = new Mock<IClock>();
        var notifService = new Mock<INotificationService>();
        var notificationServices = new[] { notifService.Object };

        var service = new GameService(context, clockMock.Object, notificationServices);

        await service.Delete(1);

        var gameAfterDelete = await context.Games.FindAsync(1);
        Assert.Null(gameAfterDelete);

        notifService.Verify(s => s.SendNotification("HRA_ODSTRANĚNA"), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenGameDoesNotExistWithoutNotification()
    {
        await using var context = ApplicationContextTestFactory.CreateContext();

        var clockMock = new Mock<IClock>();
        var notifService = new Mock<INotificationService>();
        var notificationServices = new[] { notifService.Object };

        var service = new GameService(context, clockMock.Object, notificationServices);

        await service.Delete(999); 

        notifService.Verify(s => s.SendNotification(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoadGameAsync__WhenGameIsFoundNotificationYes()
    {
        await using var context = ApplicationContextTestFactory.CreateContext();
        var testGame = new Game { Id = 1, Name = "Test Game", State = GameState.Active };
        context.Games.Add(testGame);
        await context.SaveChangesAsync();

        var notifService = new Mock<INotificationService>();
        var service = new GameService(context, new Mock<IClock>().Object, new[] { notifService.Object });

        var result = await service.LoadGameAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Test Game", result.Name);
        notifService.Verify(s => s.SendNotification("HRA_NAČTENA"), Times.Once);
    }

    [Fact]
    public async Task LoadGameAsync_WhenGameIsNotFoundWithoutNotification()
    {
        await using var context = ApplicationContextTestFactory.CreateContext();

        var notifService = new Mock<INotificationService>();
        var service = new GameService(context, new Mock<IClock>().Object, new[] { notifService.Object });

        var result = await service.LoadGameAsync(999); // ID, které neexistuje

        Assert.Null(result);
        notifService.Verify(s => s.SendNotification(It.IsAny<string>()), Times.Never);
    }
}



