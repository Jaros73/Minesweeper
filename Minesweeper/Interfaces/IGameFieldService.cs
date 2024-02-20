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
    /// <summary>
    /// Asynchronně zpracuje kliknutí uživatele na herní pole.
    /// </summary>
    /// <param name="gameId">Identifikátor hry, ve které se má herní pole odhalit.</param>
    /// <param name="input">DTO obsahující informace potřebné k identifikaci a zpracování kliknutého herního pole.</param>
    Task<GameFieldDto> Click(int gameId, GameClickDto input);
}
