using System.ComponentModel.DataAnnotations;

namespace SerbleGames.Backend.Schemas.Db;

public class ServerOptions {
    [Key] public int Id { get; set; } = 1;
    public bool RequireCreateWhitelist { get; set; }
    public bool RequirePaidCreateWhitelist { get; set; }
    public int MaxGamesPerUser { get; set; }
    public int MaxBuildsPerGame { get; set; }
}
