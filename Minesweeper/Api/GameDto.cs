using Minesweeper.Structures;

namespace Minesweeper.Api;

/// <summary>
/// Reprezentuje objekt pro přenos dat (DTO) pro entitu hry.
/// </summary>
public class GameDto
{
    /// <summary>
    /// Získá nebo nastaví identifikátor hry.
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Získá nebo nastaví název hry.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Získá nebo nastaví stav hry.
    /// </summary>
    public string State { get; set; }
    /// <summary>
    /// Získá nebo nastaví datum a čas vytvoření hry.
    /// </summary>
    public DateTime CreatedDate { get; set; }
    /// <summary>
    /// Získá nebo nastaví datum a čas ukončení hry (pokud je k dispozici).
    /// </summary>
    public DateTime? EndDate { get; set; }
    /// <summary>
    /// Získá nebo nastaví seznam herních polí spojených s hrou.
    /// </summary>
    public List<GameFieldDto> GameFields { get; set; }

    /// <summary>
    /// Inicializuje novou instanci třídy <see cref="GameDto"/> na základě poskytnuté entity hry.
    /// </summary>
    /// <param name="game">Entita hry k převodu na DTO.</param>
    public GameDto(Game game)
    {
        Id = game.Id;
        Name = game.Name;
        State = game.State.ToString();
        CreatedDate = game.CreatedDate;
        EndDate = game.EndDate;

        GameFields = game.GameFields != null ? game.GameFields.Select(x => new GameFieldDto
        {
            Id = x.Id,
            X = x.X,
            Y = x.Y,
            IsRevealed = x.IsRevealed,
            HasMine = x.HasMine,
            MinesCount = x.MinesCount
        }).ToList() : new List<GameFieldDto>();
    }
}
