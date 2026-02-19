using Microsoft.EntityFrameworkCore;
using SerbleGames.Backend.Schemas.Db;

namespace SerbleGames.Backend.Database;

public class GameRepo(GamesDatabaseContext context) : IGameRepo {
    
    public async Task<Game> CreateGame(Game game) {
        context.Games.Add(game);
        await context.SaveChangesAsync();
        return game;
    }

    public async Task<Game?> GetGameById(string id) {
        return await context.Games.FindAsync(id);
    }

    public async Task<IEnumerable<Game>> GetGamesByOwnerId(string ownerId) {
        return await context.Games.Where(g => g.OwnerId == ownerId).ToListAsync();
    }

    public async Task<int> CountGamesByOwnerId(string ownerId) {
        return await context.Games.CountAsync(g => g.OwnerId == ownerId);
    }

    public async Task<IEnumerable<Game>> GetOwnedGamesByUserId(string userId) {
        return await context.GameOwnerships
            .Where(o => o.UserId == userId)
            .Join(context.Games, 
                o => o.GameId, 
                g => g.Id, 
                (o, g) => new { Game = g, Ownership = o })
            .Select(x => new Game {
                Id = x.Game.Id,
                Name = x.Game.Name,
                Description = x.Game.Description,
                OwnerId = x.Game.OwnerId,
                Price = x.Game.Price,
                PublishDate = x.Game.PublishDate,
                TrailerVideo = x.Game.TrailerVideo,
                Public = x.Game.Public,
                Icon = x.Game.Icon,
                LinuxRelease = x.Game.LinuxRelease,
                MacRelease = x.Game.MacRelease,
                WindowsRelease = x.Game.WindowsRelease,
                Playtime = x.Ownership.Playtime,
                LastPlayed = x.Ownership.LastPlayed
            })
            .ToListAsync();
    }

    public async Task UpdateGame(Game game) {
        context.Entry(game).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    public async Task DeleteGame(string id) {
        Game? game = await context.Games.FindAsync(id);
        if (game != null) {
            context.Games.Remove(game);
            // Also delete ownerships if a game is deleted
            IQueryable<GameOwnership> ownerships = context.GameOwnerships.Where(o => o.GameId == id);
            context.GameOwnerships.RemoveRange(ownerships);
            await context.SaveChangesAsync();
        }
    }

    public async Task<GameOwnership?> GetOwnership(string userId, string gameId) {
        return await context.GameOwnerships.FirstOrDefaultAsync(o => o.UserId == userId && o.GameId == gameId);
    }

    public async Task CreateOwnership(GameOwnership ownership) {
        context.GameOwnerships.Add(ownership);
        await context.SaveChangesAsync();
    }

    public async Task UpdateOwnership(GameOwnership ownership) {
        context.Entry(ownership).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Game>> GetPublicGames(int offset, int limit) {
        return await context.Games
            .Where(g => g.Public)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Game>> SearchPublicGames(string query, int offset, int limit) {
        return await context.Games
            .Where(g => g.Public && g.Name.Contains(query))
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Achievement>> GetAchievementsByGameId(string gameId) {
        return await context.Achievements.Where(a => a.GameId == gameId).ToListAsync();
    }

    public async Task<Achievement?> GetAchievementById(string id) {
        return await context.Achievements.FindAsync(id);
    }

    public async Task CreateAchievement(Achievement achievement) {
        context.Achievements.Add(achievement);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAchievement(Achievement achievement) {
        context.Entry(achievement).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    public async Task DeleteAchievement(string id) {
        Achievement? achievement = await context.Achievements.FindAsync(id);
        if (achievement != null) {
            context.Achievements.Remove(achievement);
            // Also delete user achievements
            IQueryable<UserAchievement> userAchievements = context.UserAchievements.Where(ua => ua.AchievementId == id);
            context.UserAchievements.RemoveRange(userAchievements);
            await context.SaveChangesAsync();
        }
    }

    public async Task GrantAchievement(UserAchievement userAchievement) {
        context.UserAchievements.Add(userAchievement);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Achievement>> GetEarnedAchievements(string userId, string gameId) {
        return await context.UserAchievements
            .Where(ua => ua.UserId == userId)
            .Join(context.Achievements,
                ua => ua.AchievementId,
                a => a.Id,
                (ua, a) => new { UserAchievement = ua, Achievement = a })
            .Where(x => x.Achievement.GameId == gameId)
            .Select(x => x.Achievement)
            .ToListAsync();
    }

    public async Task<bool> HasAchievement(string userId, string achievementId) {
        return await context.UserAchievements.AnyAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId);
    }
}
