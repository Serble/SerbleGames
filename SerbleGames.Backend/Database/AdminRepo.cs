using Microsoft.EntityFrameworkCore;
using SerbleGames.Backend.Schemas.Db;

namespace SerbleGames.Backend.Database;

public class AdminRepo(GamesDatabaseContext context) : IAdminRepo {
    public async Task<ServerOptions> GetServerOptions() {
        ServerOptions? options = await context.ServerOptions.FirstOrDefaultAsync(o => o.Id == 1);
        if (options != null) return options;

        options = new ServerOptions {
            Id = 1,
            RequireCreateWhitelist = false,
            RequirePaidCreateWhitelist = false,
            MaxGamesPerUser = 0,
            MaxBuildsPerGame = 0
        };
        context.ServerOptions.Add(options);
        await context.SaveChangesAsync();
        return options;
    }

    public async Task<ServerOptions> UpdateServerOptions(ServerOptions options) {
        context.ServerOptions.Update(options);
        await context.SaveChangesAsync();
        return options;
    }

    public async Task<string[]> GetUserPermissions(string userId) {
        return await context.UserPermissions
            .Where(p => p.UserId == userId)
            .Select(p => p.Permission)
            .ToArrayAsync();
    }

    public async Task SetUserPermissions(string userId, IEnumerable<string> permissions) {
        List<UserPermission> existing = await context.UserPermissions
            .Where(p => p.UserId == userId)
            .ToListAsync();

        context.UserPermissions.RemoveRange(existing);

        IEnumerable<string> normalized = permissions
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (string permission in normalized) {
            context.UserPermissions.Add(new UserPermission {
                UserId = userId,
                Permission = permission
            });
        }

        await context.SaveChangesAsync();
    }

    public async Task<bool> IsUserWhitelisted(string userId, string type) {
        return await context.WhitelistEntries.AnyAsync(w => w.UserId == userId && w.Type == type);
    }

    public async Task SetWhitelist(string userId, string type, bool isWhitelisted) {
        WhitelistEntry? existing = await context.WhitelistEntries
            .FirstOrDefaultAsync(w => w.UserId == userId && w.Type == type);

        if (isWhitelisted && existing == null) {
            context.WhitelistEntries.Add(new WhitelistEntry { UserId = userId, Type = type });
            await context.SaveChangesAsync();
            return;
        }

        if (!isWhitelisted && existing != null) {
            context.WhitelistEntries.Remove(existing);
            await context.SaveChangesAsync();
        }
    }

    public async Task<string[]> GetWhitelistUsers(string type) {
        return await context.WhitelistEntries
            .Where(w => w.Type == type)
            .Select(w => w.UserId)
            .ToArrayAsync();
    }

    public async Task<int> CountGamesByUser(string userId) {
        return await context.Games.CountAsync(g => g.OwnerId == userId);
    }

    public async Task<int> CountPackagesByGame(string gameId) {
        return await context.Packages.CountAsync(p => p.GameId == gameId);
    }
}
