using System.ComponentModel.DataAnnotations;

namespace Minesweeper.Structures;

public class User
{
    public int Id { get; set; }
    [MaxLength(30)]
    public required string UserName { get; set; }
    [MaxLength(30)]
    public required string Password { get; set; }
}
