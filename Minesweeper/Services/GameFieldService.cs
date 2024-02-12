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
            .Where(gf => gf.GameId == gameId)
            .ToListAsync();

        return gameFields.Select(gf => new GameFieldDto
        {
            Id = gf.Id,
            X = gf.X,
            Y = gf.Y,
            IsRevealed = gf.IsRevealed,
            HasMine = gf.HasMine,
            MinesCount = gf.MinesCount,
            
        });
    }
    /// /// <summary>
    /// Asynchronně odhalí specifikované herní pole a, pokud pole neobsahuje minu, rekurzivně odhalí i sousední pole.
    /// Pokud odhalené pole obsahuje minu, hra se ukončí.
    /// </summary>
    /// <param name="gameId">Identifikátor hry, ve které se má pole odhalit.</param>
    /// <param name="fieldId">Identifikátor herního pole, které se má odhalit.</param>
    /// <returns>DTO odhaleného herního pole s aktualizovanými informacemi.</returns>
    /// <exception cref="InvalidOperationException">Výjimka - pokud není pole nalezeno.</exception>
    public async Task<GameFieldDto> RevealField(int gameId, int fieldId)
    {
        var field = await _context.GameFields
                .Include(x => x.Game)
                .FirstOrDefaultAsync(x => x.Id == fieldId && x.GameId == gameId) ?? throw new InvalidOperationException("Field not found.");

        if (field.HasMine)
        {
            // Když pole obsahuje minu
            field.Game.State = GameState.Finished;
            field.Game.EndDate = DateTime.UtcNow;
        }
        else
        {
            // Rekurzivní odkrytí, pokud pole neobsahuje minu
            await RevealSurroundingFields(gameId, field.X, field.Y);
        }

        field.IsRevealed = true;
        await _context.SaveChangesAsync();

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
    /// <summary>
    /// Asynchronně odhalí sousední pole okolo daného pole, pokud toto pole neobsahuje minu.
    /// Metoda je rekurzivní a bude pokračovat v odhalování polí, která nejsou bezprostředně vedle miny.
    /// </summary>
    /// <param name="gameId">Identifikátor hry, ve které se mají pole odhalit.</param>
    /// <param name="x">X souřadnice středového pole, okolo kterého se odhalují sousední pole.</param>
    /// <param name="y">Y souřadnice středového pole, okolo kterého se odhalují sousední pole.</param>
    /// <returns>Task pro asynchronní operaci bez navrácené hodnoty.</returns>
    private async Task RevealSurroundingFields(int gameId, int x, int y)
    {
        var fieldsToCheck = await _context.GameFields
            .Where(xy => xy.GameId == gameId &&
                        Math.Abs(xy.X - x) <= 1 &&
                        Math.Abs(xy.Y - y) <= 1 &&
                        !xy.IsRevealed)
            .ToListAsync();

        foreach (var field in fieldsToCheck)
        {
            field.IsRevealed = true;

            // Pokud pole nemá sousední miny, pokračuj v odkrývání.
            if (field.MinesCount == 0)
            {
                await RevealSurroundingFields(gameId, field.X, field.Y);
            }
        }
    }
}
