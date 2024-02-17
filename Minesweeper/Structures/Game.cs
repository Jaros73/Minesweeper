using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Minesweeper.Structures;
/// <summary>
/// Reprezentuje entitu hry.
/// </summary>
public class Game
{
    public Game()
    {
        GameFields = new List<GameField>();
    }
    /// <summary>
    /// Identifikátor hry.
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Název hry.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Stav hry.
    /// </summary>
    public GameState State { get; set; }
    /// <summary>
    /// Počet min v hře.
    /// </summary>
    public int MinesCount { get; set; }
    /// <summary>
    /// Datum a čas vytvoření hry.
    /// </summary>
    public DateTime CreatedDate{ get; set; }
    /// <summary>
    /// Datum a čas ukončení hry.
    /// </summary>
    public DateTime? EndDate { get; set; }
    /// <summary>
    /// Herní pole spojená s touto hrou.
    /// </summary>
    public List<GameField> GameFields { get; set; }
}


