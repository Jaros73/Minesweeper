using Microsoft.AspNetCore.Http.HttpResults;
using Minesweeper.Api;
using Minesweeper.Interfaces;
using Minesweeper.Structures;
using Minesweeper.Pesristance;
using Microsoft.EntityFrameworkCore;


namespace Minesweeper.Services;
/// <summary>
/// Implementace rozhraní pro službu manipulující s hrami.
/// </summary>
public class GameService : IGameService
{ 
    private readonly ApplicationContext _context;
    /// <summary>
    /// Inicializuje novou instanci třídy GameService s daným kontextem databáze.
    /// </summary>
    /// <param name="context">Kontext databáze, který se používá pro komunikaci s databází.</param>
    public GameService(ApplicationContext context)
    {
        _context = context;
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
        var newGame = new Game
        {
            Name = input.Name,
            State = GameState.Active,
            CreatedDate = DateTime.UtcNow,
            // EndDate zůstane null, protože hra teprve začíná
            MinesCount = input.MinesCount
        };

        _context.Games.Add(newGame);
        await _context.SaveChangesAsync();

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
    private async Task<Game> LoadGameAsync(int id)
    {
        return await _context.Games
            .Include(g => g.GameFields)
            .FirstOrDefaultAsync(g => g.Id == id);
    }
}
