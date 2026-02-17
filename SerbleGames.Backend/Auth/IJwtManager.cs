using SerbleGames.Backend.Schemas.Db;

namespace SerbleGames.Backend.Auth;

public interface IJwtManager {
    string GenerateToken(GamesUser user);
}
