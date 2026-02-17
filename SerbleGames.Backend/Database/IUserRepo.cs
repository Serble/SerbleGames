using SerbleGames.Backend.Schemas.Db;

namespace SerbleGames.Backend.Database;

public interface IUserRepo {
    Task<GamesUser> CreateUser(GamesUser user);
    Task<GamesUser?> GetUserById(string id);
    Task<GamesUser?> GetUserByUsername(string username);
    Task UpdateRefreshToken(string id, string refreshToken);
    Task UpdateUser(GamesUser user);
}
