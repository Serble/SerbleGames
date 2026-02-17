using System.ComponentModel.DataAnnotations;

namespace SerbleGames.Backend.Schemas.Db;

public class GameOwnership {
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = null!;
    public string GameId { get; set; } = null!;
    public DateTime PurchaseDate { get; set; }
    public double Playtime { get; set; } // In minutes
    public DateTime? LastPlayed { get; set; }
}
