namespace Minesweeper.Api;

/// <summary>
/// Data Transfer Object pro přenos informací o kliknutí na herní pole.
/// </summary>
public class GameClickDto
{
    public int GameId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

