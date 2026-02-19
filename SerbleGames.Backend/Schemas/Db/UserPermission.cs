using System.ComponentModel.DataAnnotations;

namespace SerbleGames.Backend.Schemas.Db;

public class UserPermission {
    [StringLength(64)] public string UserId { get; set; } = null!;
    [StringLength(64)] public string Permission { get; set; } = null!;
}
