using System.ComponentModel.DataAnnotations;

namespace SerbleGames.Backend.Schemas.Db;

public class Package {
    [Key] [StringLength(64)] public string Id { get; set; } = null!;
    [StringLength(64)] public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    [StringLength(64)] public string GameId { get; set; } = null!;
    [StringLength(16)] public string Platform { get; set; } = null!;
    [StringLength(128)] public string MainBinary { get; set; } = null!;
    [StringLength(256)] public string LaunchArguments { get; set; } = null!;
}
