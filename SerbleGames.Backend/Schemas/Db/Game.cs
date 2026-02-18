using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SerbleGames.Backend.Schemas.Db;

public class Game {
    [Key]
    [StringLength(64)] public string Id { get; set; } = Guid.NewGuid().ToString();
    [StringLength(64)] public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    [StringLength(64)] public string OwnerId { get; set; } = null!; // The user who created/owns the game entry
    public decimal Price { get; set; }
    public DateTime PublishDate { get; set; }
    public string? TrailerVideo { get; set; } // YouTube link
    public bool Public { get; set; }
    [StringLength(64)] public string? Icon { get; set; }
    
    // these are IDs of current packages
    [StringLength(64)] public string? LinuxRelease { get; set; }
    [StringLength(64)] public string? WindowsRelease { get; set; }
    [StringLength(64)] public string? MacRelease { get; set; }
    
    // Fields just for users (not in db)
    
    [NotMapped]
    public double Playtime { get; set; }
    [NotMapped]
    public DateTime? LastPlayed { get; set; }
}
