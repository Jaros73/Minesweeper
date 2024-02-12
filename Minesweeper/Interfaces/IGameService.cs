using Minesweeper.Api;

namespace Minesweeper.Interfaces;

/// <summary>
/// Rozhraní pro službu manipulující s hrami.
/// </summary>
public interface IGameService
{
    /// <summary>
    /// Získá hru podle zadaného identifikátoru nebo vrátí null, pokud hra neexistuje.
    /// </summary>
    /// <param name="id">Identifikátor hry.</param>
    /// <returns>DTO reprezentující nalezenou hru nebo null, pokud hra nebyla nalezena.</returns>
    Task<GameDto> Get(int id);
    /// <summary>
    /// Získá všechny aktivní hry.
    /// </summary>
    /// <returns>Kolekci DTO reprezentujících všechny aktivní hry.</returns>
    Task<IEnumerable<GameDto>> GetAllActive();
    /// <summary>
    /// Vytvoří novou hru na základě zadaných vstupních údajů.
    /// </summary>
    /// <param name="input">DTO obsahující vstupní údaje pro vytvoření nové hry.</param>
    /// <returns>DTO reprezentující nově vytvořenou hru.</returns>
    Task<GameDto> Create(GameInputDto input);
    /// <summary>
    /// Odstraní hru podle zadaného identifikátoru.
    /// </summary>
    /// <param name="id">Identifikátor hry k odstranění.</param>
    Task Delete(int id);
}
