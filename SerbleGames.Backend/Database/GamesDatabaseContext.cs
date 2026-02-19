using Microsoft.EntityFrameworkCore;
using SerbleGames.Backend.Schemas.Db;

namespace SerbleGames.Backend.Database;

public class GamesDatabaseContext : DbContext {
    public DbSet<GamesUser> Users { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }
    public DbSet<WhitelistEntry> WhitelistEntries { get; set; }
    public DbSet<ServerOptions> ServerOptions { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<GameOwnership> GameOwnerships { get; set; }
    public DbSet<Achievement> Achievements { get; set; }
    public DbSet<UserAchievement> UserAchievements { get; set; }
    public DbSet<Package> Packages { get; set; }
    
    public GamesDatabaseContext(DbContextOptions<GamesDatabaseContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<UserPermission>().HasKey(up => new { up.UserId, up.Permission });
        modelBuilder.Entity<UserPermission>()
            .HasOne<GamesUser>()
            .WithMany(u => u.Permissions)
            .HasForeignKey(up => up.UserId);
        modelBuilder.Entity<WhitelistEntry>().HasKey(w => new { w.UserId, w.Type });
        modelBuilder.Entity<ServerOptions>().HasKey(o => o.Id);
    }
}
