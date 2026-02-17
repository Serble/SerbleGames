namespace SerbleGames.Backend.Schemas.Db;

public class GamesUser {
    public string Id { get; set; }
    public string Username { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
}
