using Microsoft.EntityFrameworkCore;
using Minesweeper.Structures;

namespace Minesweeper.Pesristance;
/// <summary>
/// DbContext pro přístup k databázi.
/// </summary>
public class ApplicationContext : DbContext 
{
    /// <summary>
    /// Inicializuje novou instanci třídy ApplicationDbContext s danými možnostmi.
    /// </summary>
    /// <param name="options">Možnosti používané při konfiguraci kontextu.</param>
    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
    { }
    /// <summary>
    /// Tabulka her v databázi.
    /// </summary>
    public DbSet<Game> Games { get; set; }
    /// <summary>
    /// Tabulka herních polí v databázi.
    /// </summary>
    public DbSet<GameField> GameFields { get; set; }
    /// <summary>
    /// Získá/nastaví DbSet uživatelů, který reprezentuje tabulku Users v databázi.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;
    /// <summary>
    /// Konfigurace databázového modelu.
    /// </summary>
    /// <param name="modelBuilder">Objekt pro konfiguraci databázového modelu.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Konfigurace vztahu mezi Game a GameField
        modelBuilder.Entity<Game>()
            .HasMany(x => x.GameFields) // Game má mnoho GameFields
            .WithOne(xy => xy.Game) // Každý GameField má jednoho rodiče Game
            .HasForeignKey(xy => xy.GameId); // Cizí klíč v GameField odkazující na Game

        // Nastavení vlastnosti Name v Game na povinnou a s maximální délkou
        modelBuilder.Entity<Game>()
            .Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(25);

        // Vytvoření indexu pro rychlejší vyhledávání podle nějakého kritéria
        modelBuilder.Entity<Game>()
            .HasIndex(x => x.Name)
            .IsUnique(); // Každá hra má unikátní název

        // Pozice X, Y jsou unikátní v rámci jedné hry
        modelBuilder.Entity<GameField>()
            .HasIndex(x => new { x.GameId, x.X, x.Y })
            .IsUnique(); // Zajistí, že každé pole ve hře má unikátní pozici
    }
}
