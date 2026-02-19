using System.ComponentModel.DataAnnotations;

namespace SerbleGames.Backend.Schemas.Db;

public class WhitelistEntry {
    [StringLength(64)] public string UserId { get; set; } = null!;
    [StringLength(64)] public string Type { get; set; } = null!;
}

public static class WhitelistTypes {
    public const string CreateGames = "CreateGames";
    public const string CreatePaidGames = "CreatePaidGames";
}
