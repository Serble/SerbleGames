using SerbleGames.Backend.Schemas.Db;

namespace SerbleGames.Backend.Database;

public interface IAdminRepo {
    Task<ServerOptions> GetServerOptions();
    Task<ServerOptions> UpdateServerOptions(ServerOptions options);
    Task<string[]> GetUserPermissions(string userId);
    Task SetUserPermissions(string userId, IEnumerable<string> permissions);
    Task<bool> IsUserWhitelisted(string userId, string type);
    Task SetWhitelist(string userId, string type, bool isWhitelisted);
    Task<string[]> GetWhitelistUsers(string type);
    Task<int> CountGamesByUser(string userId);
    Task<int> CountPackagesByGame(string gameId);
}
