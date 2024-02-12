namespace Minesweeper.Structures;
/// <summary>
/// Reprezentuje jedno herní pole v rámci hry.
/// </summary>
public class GameField
{
    /// <summary>
    /// Identifikátor herního pole.
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Identifikátor hry, ke které herní pole patří.
    /// </summary>
    public int GameId { get; set; }
    /// <summary>
    /// Reference na hru, ke které herní pole patří.
    /// </summary>
    public Game Game { get; set; }
    /// <summary>
    /// Pozice X herního pole.
    /// </summary>
    public int X { get; set; }
    /// <summary>
    /// Pozice Y herního pole.
    /// </summary>
    public int Y { get; set; }
    /// <summary>
    /// Určuje, zda je herní pole odhalené.
    /// </summary>
    public bool IsRevealed { get; set; }
    /// <summary>
    /// Určuje, zda má herní pole minu.
    /// </summary>
    public bool HasMine {  get; set; }
    /// <summary>
    /// Počet min okolo herního pole.
    /// </summary>
    public int MinesCount { get; set; }
}
