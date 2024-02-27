using Microsoft.AspNetCore.Http.HttpResults;
using Minesweeper.Api;
using Minesweeper.Interfaces;
using Minesweeper.Structures;
using Minesweeper.Pesristance;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using System.ComponentModel.DataAnnotations;
using Minesweeper.Extensions;


namespace Minesweeper.Services;
/// <summary>
/// Implementace rozhraní pro službu manipulující s hrami.
/// </summary>
public class GameService : IGameService
{ 
    private readonly ApplicationContext _context;
    private readonly IClock _clock;
    private readonly INotificationService[]? _notificationServices;

/// <summary>
/// Inicializuje novou instanci třídy GameService s daným kontextem databáze, časovým poskytovatelem a volitelnými službami notifikace.
/// </summary>
/// <param name="context">Kontext databáze, který se používá pro komunikaci s databází.</param>
/// <param name="clock">Poskytovatel času, který se používá pro řízení časově závislých operací.</param>
/// <param name="notificationServices">Kolekce služeb pro odesílání notifikací. Může být null, pokud nejsou poskytnuty žádné služby notifikace.</param>
    public GameService(ApplicationContext context, IClock clock, IEnumerable<INotificationService>? notificationServices = null)
    {
        _context = context;
        _clock = clock;
        _notificationServices = notificationServices?.ToArray();
    }
    /// <summary>
    /// Získá hru podle zadaného identifikátoru.
    /// </summary>
    /// <param name="id">Identifikátor hry.</param>
    /// <returns>DTO reprezentující získanou hru.</returns>
    public async Task<GameDto> Get(int id)
    {
        var game = await LoadGameAsync(id);
        if (game == null) return null;

        foreach (var service in _notificationServices)
        {
            await service.SendNotification("HRA_ZÍSKÁNA");
        };

        return new GameDto(game);
    }
    /// <summary>
    /// Získá všechny aktivní hry.
    /// </summary>
    /// <returns>Kolekci DTO reprezentujících všechny aktivní hry.</returns>
    public async Task<IEnumerable<GameDto>> GetAllActive()
    {
        var games = await _context.Games
            .Where(x => x.State == GameState.Active)
            .Include(x => x.GameFields) // Přidává načítání herních polí
            .ToListAsync();

        if (games.Any() && _notificationServices is not null)
        {
            foreach (var service in _notificationServices)
            {
                await service.SendNotification("AKTIVNI_HRY_ZÍSKÁNY");
            };
        }

        return games.Select(game => new GameDto(game)
        {
            Id = game.Id,
            Name = game.Name,
            State = game.State.ToString(), // Konverze enum na string
            EndDate = game.EndDate,
            GameFields = game.GameFields.Select(x => new GameFieldDto
            {
                Id = x.Id,
                X = x.X,
                Y = x.Y,
                IsRevealed = x.IsRevealed,
                HasMine = x.HasMine,
                MinesCount = x.MinesCount,
            }).ToList() // Převod herních polí na GameFieldDto
        });
    }
    /// <summary>
    /// Najde hru podle zadaného identifikátoru.
    /// </summary>
    /// <param name="id">Identifikátor hry.</param>
    /// <returns>DTO reprezentující nalezenou hru.</returns>
    public async Task<GameDto> Find(int id)
    {
        var game = await LoadGameAsync(id);
        if (game == null) return null;

        foreach (var service in _notificationServices)
        {
           await service.SendNotification("HRA_NALEZENA");
        };

        return new GameDto(game)
        {
            Id = game.Id,
            Name = game.Name,
            State = game.State.ToString(),
            EndDate = game.EndDate,
        };
    }
    /// <summary>
    /// Vytvoří novou hru na základě zadaných vstupních údajů.
    /// </summary>
    /// <param name="input">DTO obsahující vstupní údaje pro vytvoření nové hry.</param>
    /// <returns>DTO reprezentující nově vytvořenou hru.</returns>
    public async Task<GameDto> Create(GameInputDto input)
    {
        input.Name.ValidateLettersOrNumbers();
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(input);

        if (!Validator.TryValidateObject(input, validationContext, validationResults, true))
        {
            // Převádí výsledky validace na chybové zprávy
            var errorMessages = validationResults.Select(vr => vr.ErrorMessage).ToList();
            // Můžete vyvolat výjimku s detailními informacemi o chybách
            throw new ArgumentException("Neplatná data pro vytvoření hry:");
        }

        // Pevná velikost 10x10
        int width = 10;
        int height = 10;

        if (input.MinesCount < 0 || input.MinesCount > 100)
        {
            throw new ArgumentException("Počet min musí být větší než 0 a menší než 100.");
        }

        var newGame = new Game
        {
            Name = input.Name,
            State = GameState.Active,
            CreatedDate = DateTime.UtcNow,
            // EndDate zůstane null, protože hra teprve začíná
            MinesCount = input.MinesCount,
            GameFields = GenerateGameFields(width, height, input.MinesCount)
        };

        _context.Games.Add(newGame);
        await _context.SaveChangesAsync();
         if (_notificationServices is not null)
        {
            foreach (var service in _notificationServices) { 
            await service.SendNotification("HRA_VYTVOŘENA");};
        }
        return new GameDto(newGame)
        {
            Id = newGame.Id,
            Name = newGame.Name,
            State = newGame.State.ToString(),
            CreatedDate = newGame.CreatedDate,
            EndDate = newGame.EndDate
        };
    }
    /// <summary>
    /// Odstraní hru podle zadaného identifikátoru.
    /// </summary>
    /// <param name="id">Identifikátor hry k odstranění.</param>
    public async Task Delete(int id)
    {
        var game = await _context.Games.FindAsync(id);
        if (game == null) return;

        foreach (var service in _notificationServices)
        {
            await service.SendNotification("HRA_ODSTRANĚNA");
        };

        _context.Games.Remove(game);
        await _context.SaveChangesAsync();
    }
    /// <summary>
    /// Načte hru a související herní pole z databáze na základě zadaného identifikátoru hry.
    /// </summary>
    /// <param name="id">Identifikátor hry, která má být načtena.</param>
    /// <returns>
    /// Vrátí instanci hry s načtenými herními poli, pokud hra s daným identifikátorem existuje.
    /// V opačném případě vrátí null.
    /// </returns>
    internal async Task<Game> LoadGameAsync(int id)
    {
        var game = await _context.Games
            .Include(g => g.GameFields)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (game != null)
        {
            foreach (var service in _notificationServices)
            {
                await service.SendNotification("HRA_NAČTENA");
            }
        }
        return game;
    }


    /// <summary>
    /// Generuje a inicializuje seznam herních polí s definovanou šířkou, výškou a počtem min.
    /// Každé pole je inicializováno jako neodhalené a bez miny. Poté jsou miny náhodně rozmístěny
    /// po celém herním poli, přičemž každé pole dostane informaci o počtu sousedních min.
    /// Pro pole s minou je počet sousedních min nastaven na -1.
    /// </summary>
    /// <param name="width">Šířka herního pole.</param>
    /// <param name="height">Výška herního pole.</param>
    /// <param name="minesCount">Celkový počet min, které mají být rozmístěny na herním poli.</param>
    /// <returns>Seznam herních polí s příslušnou inicializací a rozložením min.</returns>
    private List<GameField> GenerateGameFields(int width, int height, int minesCount)
    {
        var fields = new List<GameField>();
        var random = new Random();

        // Inicializace herního pole s pevnou velikostí 10x10
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                fields.Add(new GameField { GameId = 0, X = x, Y = y, IsRevealed = false, HasMine = false });
            }
        }

        // Náhodné rozmístění min
        int placedMines = 0;
        while (placedMines < minesCount)
        {
            int position = random.Next(fields.Count);
            if (!fields[position].HasMine)
            {
                fields[position].HasMine = true;
                placedMines++;
            }
        }

        // Výpočet počtu min v okolí pro každé pole
        foreach (var field in fields)
        {
            field.MinesCount = fields.Count(f =>
                !f.HasMine &&
                Math.Abs(f.X - field.X) <= 1 &&
                Math.Abs(f.Y - field.Y) <= 1 &&
                fields.Any(ff => ff.X == f.X && ff.Y == f.Y && ff.HasMine));

            // Pokud má pole minu, MinesCount nastavíme na -1 pro snadnou identifikaci
            if (field.HasMine)
            {
                field.MinesCount = -1;
            }
        }

        return fields;
    }
}
