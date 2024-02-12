using Minesweeper.Api;

namespace Minesweeper.Interfaces;

/// <summary>
/// Rozhraní pro službu manipulující s herními poli.
/// </summary>
public interface IGameFieldService
{
    /// <summary>
    /// Odhalí herní pole v zadané hře.
    /// </summary>
    /// <param name="gameId">Identifikátor hry.</param>
    /// <param name="fieldId">Identifikátor herního pole.</param>
    /// <returns>DTO reprezentující odhalené herní pole.</returns>
    Task<GameFieldDto> RevealField(int gameId, int fieldId);
    /// <summary>
    /// Získá herní pole pro zadanou hru.
    /// </summary>
    /// <param name="gameId">Identifikátor hry.</param>
    /// <returns>Kolekci DTO reprezentujících herní pole pro zadanou hru.</returns>
    Task<IEnumerable<GameFieldDto>> GetGameFields(int gameId);
}
