using SerbleGames.Backend.Schemas;
using SerbleGames.Backend.Schemas.Db;

namespace SerbleGames.Backend.Serble;

public interface ISerbleApiClient {
    Task<TokenResponse?> Authenticate(string code);
    Task<TokenResponse?> GetAccessToken(string refreshToken);
    Task<SerbleUser?> GetUserInfo(string accessToken);
    
    Task<TokenResponse?> GetAccessToken(GamesUser user) => GetAccessToken(user.RefreshToken);
}
