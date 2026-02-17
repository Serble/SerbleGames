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

    public async Task<IEnumerable<Game>> GetOwnedGamesByUserId(string userId) {
        return await context.GameOwnerships
            .Where(o => o.UserId == userId)
            .Join(context.Games, 
                o => o.GameId, 
                g => g.Id, 
                (o, g) => g)
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
}
