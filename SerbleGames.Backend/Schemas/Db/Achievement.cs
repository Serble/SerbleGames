using System.ComponentModel.DataAnnotations;

namespace SerbleGames.Backend.Schemas.Db;

public class Achievement {
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string GameId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Icon { get; set; }
    public bool Hidden { get; set; }
}
