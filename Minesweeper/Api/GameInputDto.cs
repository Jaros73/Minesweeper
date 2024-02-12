using System.ComponentModel.DataAnnotations;

namespace Minesweeper.Api;

/// <summary>
/// Data objektu (DTO) pro vstupní data pro vytvoření nové hry.
/// </summary>
public class GameInputDto
{
    /// <summary>
    /// Název hry.
    /// </summary>
    [Required(ErrorMessage = "Název hry je povinný.")]
    [StringLength(100, ErrorMessage = "Název hry musí být mezi 1 a 100 znaky.", MinimumLength = 1)]
    public string Name { get; set; }

    /// <summary>
    /// Počet min v herním poli.
    /// </summary>
    [Required(ErrorMessage = "Počet min je povinný.")]
    [Range(1, 99, ErrorMessage = "Počet min musí být kladné číslo a menší než 100.")]
    public int MinesCount { get; set; }
}
