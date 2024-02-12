namespace Minesweeper.Api;

/// <summary>
/// Data objektu (DTO) pro reprezentaci herního pole.
/// </summary>
public class GameFieldDto
{
    /// <summary>
    /// Identifikátor herního pole.
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Pozice X herního pole.
    /// </summary>
    public int X { get; set; }
    /// <summary>
    /// Pozice Y herního pole.
    /// </summary>
    public int Y { get; set; }
    /// <summary>
    /// Určuje, zda je herní pole odkryté.
    /// </summary>
    public bool IsRevealed { get; set; }
    /// <summary>
    /// Určuje, zda má herní pole minu.
    /// </summary>
    public bool HasMine { get; set; }
    /// <summary>
    /// Počet min v okolí herního pole.
    /// </summary>
    public int MinesCount { get; set; }
}
