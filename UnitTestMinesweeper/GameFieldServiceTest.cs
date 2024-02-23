using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Minesweeper.Interfaces;
using Minesweeper.Services;
using Minesweeper.Structures;
using Moq;
using Xunit;
using NodaTime.Testing;
using NodaTime;
using Microsoft.EntityFrameworkCore;
using Minesweeper.Api;


namespace UnitTestMinesweeper;

public class GameFieldServiceTest
{
    [Fact]
    public async Task GetGameFields_WhenFieldsExistNotificationYes()
    {
        await using var context = ApplicationContextTestFactory.CreateContext();
        var game = new Game { Name = "Test Game", State = GameState.Active };
        var testDateTime = Instant.FromDateTimeUtc(new DateTime(2024, 2, 22, 13, 30, 30, DateTimeKind.Utc));
        context.GameFields.Add(new GameField { GameId = 1, X = 0, Y = 0, IsRevealed = false, HasMine = true, MinesCount = 1 });
        context.SaveChanges();

        var notifServiceMock = new Mock<INotificationService>();
        var notificationServices = new[] { notifServiceMock.Object };

        var service = new GameFieldService(context, new FakeClock(testDateTime), notificationServices);

        var result = await service.GetGameFields(1);

        Assert.Single(result);
        notifServiceMock.Verify(s => s.SendNotification("HERNIPOLE_ZÍSKÁNO"), Times.Once);
    }

    [Fact]
    public async Task GetGameFields_WhenFieldsNotExistWithoutNotification()
    {
        await using var context = ApplicationContextTestFactory.CreateContext();

        var game = new Game { Name = "Test Game Without Fields", State = GameState.Active };
        var testDateTime = Instant.FromDateTimeUtc(new DateTime(2024, 2, 22, 13, 30, 30, DateTimeKind.Utc));
        context.Games.Add(game);
        await context.SaveChangesAsync();

        var notifServiceMock = new Mock<INotificationService>();
        var notificationServices = new[] { notifServiceMock.Object };

        var service = new GameFieldService(context, new FakeClock(testDateTime), notificationServices);

        var result = await service.GetGameFields(game.Id); 

        Assert.Empty(result); 
        notifServiceMock.Verify(s => s.SendNotification("HERNIPOLE_ZÍSKÁNO"), Times.Never); 
    }

    [Fact]
    public async Task RevealField_UpdatesField_WhenFieldDoesNotHaveMineNotificationYes()
    {
        await using var context = ApplicationContextTestFactory.CreateContext();
        var testDateTime = Instant.FromDateTimeUtc(new DateTime(2024, 2, 22, 13, 30, 30, DateTimeKind.Utc));

        var game = new Game { Name = "Test Game", State = GameState.Active };
        context.Games.Add(game);
        await context.SaveChangesAsync();

        // Přidání herního pole bez miny, které je spojeno s přidanou hrou
        var gameField = new GameField { GameId = game.Id, X = 0, Y = 0, IsRevealed = false, HasMine = false, MinesCount = 0 };
        context.GameFields.Add(gameField);
        await context.SaveChangesAsync();

        var notifServiceMock = new Mock<INotificationService>();
        var notificationServices = new[] { notifServiceMock.Object };
        var service = new GameFieldService(context, new FakeClock(testDateTime), notificationServices);

        var result = await service.RevealField(game.Id, gameField.Id);

        Assert.True(result.IsRevealed);
        Assert.False(result.HasMine);
        notifServiceMock.Verify(s => s.SendNotification("HRA_ODHALENA"), Times.Once);
    }


    [Fact]
    public async Task RevealField_FinishesGame_WhenFieldHasMineNotificationYes()
    {
        await using var context = ApplicationContextTestFactory.CreateContext();
        var testDateTime = Instant.FromDateTimeUtc(new DateTime(2024, 2, 22, 13, 30, 30, DateTimeKind.Utc));

        var game = new Game { Name = "Test Game", State = GameState.Active };
        context.Games.Add(game);
        await context.SaveChangesAsync();

        // Přidání herního pole s minou
        var gameField = new GameField { GameId = game.Id, X = 0, Y = 0, IsRevealed = false, HasMine = true, MinesCount = 1 };
        context.GameFields.Add(gameField);
        await context.SaveChangesAsync();

        var notifServiceMock = new Mock<INotificationService>();
        var notificationServices = new[] { notifServiceMock.Object };
        var service = new GameFieldService(context, new FakeClock(testDateTime), notificationServices);

        var result = await service.RevealField(game.Id, gameField.Id);

        var updatedGame = await context.Games.FindAsync(game.Id);

        Assert.True(result.IsRevealed);
        Assert.True(result.HasMine);
        Assert.Equal(GameState.Finished, updatedGame.State); 
        notifServiceMock.Verify(s => s.SendNotification("HRA_ODHALENA"), Times.Once); 
    }


    [Fact]
    public async Task RevealField_WhenFieldNotFoundWithoutNotification()
    {
        await using var context = ApplicationContextTestFactory.CreateContext();
        var testDateTime = Instant.FromDateTimeUtc(new DateTime(2024, 2, 22, 13, 30, 30, DateTimeKind.Utc));
        context.GameFields.Add(new GameField { GameId = 1, X = 0, Y = 0, IsRevealed = false, HasMine = true, MinesCount = 1 });
        context.SaveChanges();
        var game = new Game { Name = "Test Game", State = GameState.Active };
        context.Games.Add(game);
        await context.SaveChangesAsync();

        var notifServiceMock = new Mock<INotificationService>();
        var notificationServices = new[] { notifServiceMock.Object };
        var service = new GameFieldService(context, new FakeClock(testDateTime), notificationServices);

        var gameId = game.Id;
        var nonExistentFieldId = 999; 

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.RevealField(gameId, nonExistentFieldId));

        notifServiceMock.Verify(s => s.SendNotification(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Click_WhenFieldIsNotRevealedNotificationYes()
    {
        var context = ApplicationContextTestFactory.CreateContext();
        var testDateTime = Instant.FromDateTimeUtc(new DateTime(2024, 2, 22, 13, 30, 30, DateTimeKind.Utc));
        var game = new Game { Name = "Test Game", State = GameState.Active };
        context.Games.Add(game);
        await context.SaveChangesAsync();

        var field = new GameField { GameId = game.Id, X = 0, Y = 0, IsRevealed = false, HasMine = false };
        context.GameFields.Add(field);
        await context.SaveChangesAsync();

        var notifServiceMock = new Mock<INotificationService>();
        var notificationServices = new[] { notifServiceMock.Object };
        var service = new GameFieldService(context, new FakeClock(testDateTime), notificationServices);

        var result = await service.Click(game.Id, new GameClickDto { X = 0, Y = 0 });

        Assert.True(result.IsRevealed);
        notifServiceMock.Verify(s => s.SendNotification("BYLO_KLIKNUTO"), Times.Once);
    }

    [Fact]
    public async Task Click__WhenFieldIsAlreadyRevealedWithoutNotification()
    {
        var context = ApplicationContextTestFactory.CreateContext();
        var testDateTime = Instant.FromDateTimeUtc(new DateTime(2024, 2, 22, 13, 30, 30, DateTimeKind.Utc));
        var game = new Game { Name = "Test Game", State = GameState.Active };
        context.Games.Add(game);
        await context.SaveChangesAsync();

        var field = new GameField { GameId = game.Id, X = 1, Y = 1, IsRevealed = true, HasMine = false };
        context.GameFields.Add(field);
        await context.SaveChangesAsync();

        var notifServiceMock = new Mock<INotificationService>();
        var notificationServices = new[] { notifServiceMock.Object };
        var service = new GameFieldService(context, new FakeClock(testDateTime), notificationServices);

        var result = await service.Click(game.Id, new GameClickDto { X = 1, Y = 1 });

        Assert.True(result.IsRevealed); 
        notifServiceMock.Verify(s => s.SendNotification("BYLO_KLIKNUTO"), Times.Never); 
    }

    [Fact]
    public async Task Click_ThrowsException_WhenClickedOutsideFieldRangeWithoutNotification()
    {
        await using var context = ApplicationContextTestFactory.CreateContext();
        var testDateTime = Instant.FromDateTimeUtc(new DateTime(2024, 2, 22, 13, 30, 30, DateTimeKind.Utc));
        var game = new Game { Name = "Test Game", State = GameState.Active };
        context.Games.Add(game);
        await context.SaveChangesAsync();

        var field = new GameField { GameId = game.Id, X = 0, Y = 0, IsRevealed = false, HasMine = false };
        context.GameFields.Add(field);
        await context.SaveChangesAsync();

        var notifServiceMock = new Mock<INotificationService>();
        var notificationServices = new[] { notifServiceMock.Object };
        var service = new GameFieldService(context, new FakeClock(testDateTime), notificationServices);

        var invalidClickDto = new GameClickDto { X = 10, Y = 10 };

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.Click(game.Id, invalidClickDto)
        );

        notifServiceMock.Verify(s => s.SendNotification(It.IsAny<string>()), Times.Never);
    }
}
