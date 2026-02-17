using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SerbleGames.Backend.Schemas.Db;

public class Game {
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string OwnerId { get; set; } = null!; // The user who created/owns the game entry
    public decimal Price { get; set; }
    public DateTime PublishDate { get; set; }
    public string? TrailerVideo { get; set; } // YouTube link
    public bool Public { get; set; }
    
    // Release builds (IDs of files in R2 bucket)
    public string? LinuxBuild { get; set; }
    public string? WindowsBuild { get; set; }
    public string? MacBuild { get; set; }
    public string? Icon { get; set; }
    
    [NotMapped]
    public double Playtime { get; set; }
    [NotMapped]
    public DateTime? LastPlayed { get; set; }
}
