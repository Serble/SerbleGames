namespace SerbleGames.Backend.Schemas.Db;

public class GamesUser {
    public string Id { get; set; }
    public string Username { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public bool IsAdmin { get; set; }
    public bool IsBanned { get; set; }
    public ICollection<UserPermission> Permissions { get; set; } = new List<UserPermission>();
}
