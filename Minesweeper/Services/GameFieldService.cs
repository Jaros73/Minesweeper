using Minesweeper.Api;
using Minesweeper.Interfaces;
using Minesweeper.Structures;
using Minesweeper.Pesristance;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Minesweeper.Services;
/// <summary>
/// Implementace rozhraní pro službu manipulující s herními poli.
/// </summary>
public class GameFieldService : IGameFieldService
{
    private readonly ApplicationContext _context;
    private readonly IClock _clock;
    private readonly INotificationService[]? _notificationServices;
    /// <summary>
    /// Inicializuje novou instanci třídy GameFieldService s daným kontextem databáze.
    /// </summary>
    /// <param name="context">Kontext databáze, který se používá pro komunikaci s databází.</param>
    public GameFieldService(ApplicationContext context, IClock clock, IEnumerable<INotificationService>? notificationServices = null)
    {
        _context = context;
        _clock = clock;
        _notificationServices = notificationServices?.ToArray();
    }
    /// <summary>
    /// Získá herní pole pro danou hru.
    /// </summary>
    /// <param name="gameId">Identifikátor hry, pro kterou se hledají herní pole.</param>
    /// <returns>Kolekci herních polí pro danou hru.</returns>
    public async Task<IEnumerable<GameFieldDto>> GetGameFields(int gameId)
    {
        var gameFields = await _context.GameFields
            .Where(x => x.GameId == gameId)
            .ToListAsync();

        if (gameFields.Any())
        {
            foreach (var service in _notificationServices)
            {
                await service.SendNotification("HERNIPOLE_ZÍSKÁNO");
            }
        }

        return gameFields.Select(xy => new GameFieldDto
        {
            Id = xy.Id,
            X = xy.X,
            Y = xy.Y,
            IsRevealed = xy.IsRevealed,
            HasMine = xy.HasMine,
            MinesCount = xy.MinesCount,           
        });
    }
    /// <summary>
    /// Asynchronně odhalí specifikované herní pole a pokud pole neobsahuje minu rekurzivně odhalí i sousední pole.
    /// Pokud odhalené pole obsahuje minu hra se okamžitě ukončí, nastaví se její stav na ukončený a uloží se datum a čas ukončení.
    /// </summary>
    /// <param name="gameId">Identifikátor hry, ve které se má pole odhalit.</param>
    /// <param name="fieldId">Identifikátor herního pole, které se má odhalit.</param>
    /// <returns>DTO odhaleného herního pole s aktualizovanými informacemi zda pole obsahuje minu + počet min v okolí + souřadnice.</returns>
    /// <exception cref="InvalidOperationException">Vyvolá výjimku, pokud hra nebo herní pole nebylo nalezeno.</exception>
    public async Task<GameFieldDto> RevealField(int gameId, int fieldId)
    {
        // Načtení hry a všech jejích herních polí 
        var game = await _context.Games
                                 .Include(x => x.GameFields)
                                 .FirstOrDefaultAsync(x => x.Id == gameId) ?? throw new InvalidOperationException("Game not found.");

        var fieldToReveal = game.GameFields.FirstOrDefault(x => x.Id == fieldId) ?? throw new InvalidOperationException("Field not found.");

        if (fieldToReveal.HasMine)
        {
            game.State = GameState.Finished;
            game.EndDate = DateTime.UtcNow;
        }
        else
        {
            // Rekurzivní odkrytí 
            RevealSurroundingFields(game.GameFields.ToList(), fieldToReveal.X, fieldToReveal.Y);
        }

        fieldToReveal.IsRevealed = true;
        await _context.SaveChangesAsync();

        if (fieldToReveal.IsRevealed)
        {
            foreach (var service in _notificationServices)
            {
                await service.SendNotification("HRA_ODHALENA");
            }
        }

        return new GameFieldDto
        {
            Id = fieldToReveal.Id,
            X = fieldToReveal.X,
            Y = fieldToReveal.Y,
            IsRevealed = fieldToReveal.IsRevealed,
            HasMine = fieldToReveal.HasMine,
            MinesCount = fieldToReveal.MinesCount,
        };
    }
    /// <summary>
    /// Odhalí sousední herní pole v okolí specifikovaného pole, pokud toto pole neobsahuje minu.
    /// Tato metoda pracuje rekurzivně a odhaluje všechna sousední pole, která nebyla předtím odhalena.
    /// Rekurze pokračuje pouze pro pole, která nemají sousední miny + odhalení volných oblastí bez min.
    /// </summary>
    /// <param name="allFields">Seznam všech polí ve hře, se kterými se má pracovat.</param>
    /// <param name="x">X souřadnice pole, od kterého se má začít odhalování.</param>
    /// <param name="y">Y souřadnice pole, od kterého se má začít odhalování.</param>

    private void RevealSurroundingFields(List<GameField> allFields, int x, int y)
    {
        var fieldsToCheck = allFields
            .Where(xy => Math.Abs(xy.X - x) <= 1 && Math.Abs(xy.Y - y) <= 1 && !xy.IsRevealed)
            .ToList();

        foreach (var field in fieldsToCheck)
        {
            field.IsRevealed = true;

            if (field.MinesCount == 0)
            {
                RevealSurroundingFields(allFields, field.X, field.Y);
            }
        }
    }

    /// <summary>
    /// Asynchronně zpracuje kliknutí uživatele na herní pole v rámci dané hry. Pokud pole obsahuje minu,
    /// aktualizuje stav hry na ukončený a nastaví datum a čas ukončení. Pokud pole neobsahuje minu, metoda odhalí toto pole
    /// a rekurzivně odhalí sousední herní pole, pokud pole nemá žádné sousední miny. Tato operace může vést k odhalení větší
    /// části herního pole na základě jediného kliknutí, pokud jsou splněny podmínky pro rekurzivní odhalení.
    /// </summary>
    /// <param name="gameId">Identifikátor hry, ve které se má pole odhalit.</param>
    /// <param name="input">DTO obsahující souřadnice X a Y potřebné k identifikaci a zpracování kliknutého herního pole. </param>

    public async Task<GameFieldDto> Click(int gameId, GameClickDto input)
    {
        // Načtení hry a jejích herních polí z databáze
        var game = await _context.Games
                                 .Include(g => g.GameFields)
                                 .FirstOrDefaultAsync(g => g.Id == gameId);

        // Kontrola, zda hra existuje a je aktivní
        if (game == null || game.State != GameState.Active)
        {
            throw new InvalidOperationException("Game not found or is not active.");
        }

        // Nalezení konkrétního herního pole na základě X a Y souřadnic
        var field = game.GameFields.FirstOrDefault(f => f.X == input.X && f.Y == input.Y);
        if (field == null)
        {
            throw new InvalidOperationException("Field not found.");
        }

        // Kontrola, zda bylo na pole již kliknuto nebo obsahuje minu
        if (field.IsRevealed)
        {
            return new GameFieldDto
            {
                Id = field.Id,
                X = field.X,
                Y = field.Y,
                IsRevealed = field.IsRevealed,
                HasMine = field.HasMine,
                MinesCount = field.MinesCount,
            };
        }

        bool wasClicked = !field.IsRevealed;

        // Odhalení pole
        field.IsRevealed = true;

        // Pokud pole obsahuje minu, aktualizace stavu hry na dokončenou
        if (field.HasMine)
        {
            game.State = GameState.Finished;
            game.EndDate = DateTime.UtcNow;
        }
        else
        {
            // Pokud pole neobsahuje minu a má 0 sousedních min, rekurzivní odhalení sousedních polí
            if (field.MinesCount == 0)
            {
                RevealSurroundingFields(game.GameFields.ToList(), field.X, field.Y);
            }
        }

        // Uložení změn do databáze
        await _context.SaveChangesAsync();

        if (wasClicked)
        {
            foreach (var service in _notificationServices)
            {
                await service.SendNotification("BYLO_KLIKNUTO");
            }
        }


        // Vrácení informací o aktuálně kliknutém poli
        return new GameFieldDto
        {
            Id = field.Id,
            X = field.X,
            Y = field.Y,
            IsRevealed = field.IsRevealed,
            HasMine = field.HasMine,
            MinesCount = field.MinesCount,
        };
    }

}
