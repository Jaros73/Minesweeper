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
    /// 
    [RegularExpression(@"^[a-zA-Z0-9\s]+$", ErrorMessage = "Název hry může obsahovat pouze alfanumerické znaky a mezery.")]
    [Required(ErrorMessage = "Název hry je povinný.")]
    [StringLength(25, ErrorMessage = "Název hry musí být mezi 1 a 25 znaky.", MinimumLength = 1)]
    public string Name { get; set; }

    /// <summary>
    /// Počet min v herním poli.
    /// </summary>
    [Required(ErrorMessage = "Počet min je povinný.")]
    [Range(1, 99, ErrorMessage = "Počet min musí být kladné číslo a menší než 100.")]
    public int MinesCount { get; set; }
}
