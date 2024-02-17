using Minesweeper.Api;
using Minesweeper.Interfaces;
using Minesweeper.Structures;
using Minesweeper.Pesristance;
using Microsoft.EntityFrameworkCore;

namespace Minesweeper.Services;
/// <summary>
/// Implementace rozhraní pro službu manipulující s herními poli.
/// </summary>
public class GameFieldService : IGameFieldService
{
    private readonly ApplicationContext _context;
    /// <summary>
    /// Inicializuje novou instanci třídy GameFieldService s daným kontextem databáze.
    /// </summary>
    /// <param name="context">Kontext databáze, který se používá pro komunikaci s databází.</param>
    public GameFieldService(ApplicationContext context)
    {
        _context = context;
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
}
