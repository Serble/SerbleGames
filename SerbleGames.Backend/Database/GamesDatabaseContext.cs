using Microsoft.EntityFrameworkCore;
using SerbleGames.Backend.Schemas.Db;

namespace SerbleGames.Backend.Database;

public class GamesDatabaseContext : DbContext {
    public DbSet<GamesUser> Users { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<GameOwnership> GameOwnerships { get; set; }
    
    public GamesDatabaseContext(DbContextOptions<GamesDatabaseContext> options) : base(options) { }
}
