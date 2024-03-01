using Minesweeper.Pesristance;
using Minesweeper.Controllers;
using Minesweeper.Services;
using Minesweeper.Structures;
using NodaTime;
using System;


namespace ComponentTestMinesweeper.Utils;


public class TestDataSeeder
{
    public const int Game_1_ID = 1;
    public const int Game_2_ID = 2;
    public const int Game_3_ID = 3;
    public const string Game_1_Name = "Test_hra_první";
    public const string Game_2_Name = "Test_hra_druhá";
    public const string Game_3_Name = "Test_hra_třetí";
    public const int Game_1_CountMine = 10;
    public const int Game_2_CountMine = 20;
    public const int Game_3_CountMine = 30;



    public static void SeedData(IServiceProvider provider)
    {
        var testDateTime = Instant.FromDateTimeUtc(new DateTime(2024, 2, 22, 13, 30, 30, DateTimeKind.Utc));
        Random random = new Random();

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        // Konverze Instant na DateTime
        var createdDate = testDateTime.ToDateTimeUtc();

        db.Games.AddRange(new[]
        {
                new Game() { Id = Game_1_ID, Name = Game_1_Name, CreatedDate = createdDate, MinesCount = Game_1_CountMine, State = GameState.Active },
                new Game() { Id = Game_2_ID, Name = Game_2_Name, CreatedDate = createdDate, MinesCount = Game_2_CountMine, State = GameState.Active },
                new Game() { Id = Game_3_ID, Name = Game_3_Name, CreatedDate = createdDate, MinesCount = Game_3_CountMine, State = GameState.Active },

        });

        db.Users.Add(new User() { Id = 1, UserName = "games-app", Password = "Karel*" });

        GenerateAndAddGameFields(db, Game_1_ID, Game_1_CountMine, random);
        GenerateAndAddGameFields(db, Game_2_ID, Game_2_CountMine, random);
        GenerateAndAddGameFields(db, Game_3_ID, Game_3_CountMine, random);

        db.SaveChanges();
    }
    private static void GenerateAndAddGameFields(ApplicationContext db, int gameId, int minesCount, Random random)
    {
        var minePositions = new HashSet<(int, int)>();
        while (minePositions.Count < minesCount)
        {
            minePositions.Add((random.Next(0, 10), random.Next(0, 10)));
        }

        var gameFields = new List<GameField>();
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                var hasMine = minePositions.Contains((x, y));
                gameFields.Add(new GameField()
                {
                    GameId = gameId,
                    X = x,
                    Y = y,
                    IsRevealed = false,
                    HasMine = hasMine,
                    MinesCount = hasMine ? -1 : 0 // Nastaví MinesCount na -1 pro pole s minou, jinak 0
                });
            }
        }

        db.GameFields.AddRange(gameFields);
    }

}
