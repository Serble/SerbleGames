using SerbleGames.Backend.Schemas.Db;

namespace SerbleGames.Backend.Database;

public interface IGameRepo {
    Task<Game> CreateGame(Game game);
    Task<Game?> GetGameById(string id);
    Task<IEnumerable<Game>> GetGamesByOwnerId(string ownerId);
    Task<IEnumerable<Game>> GetOwnedGamesByUserId(string userId);
    Task UpdateGame(Game game);
    Task DeleteGame(string id);
    Task<GameOwnership?> GetOwnership(string userId, string gameId);
    Task CreateOwnership(GameOwnership ownership);
    Task<IEnumerable<Game>> GetPublicGames(int offset, int limit);
    Task<IEnumerable<Game>> SearchPublicGames(string query, int offset, int limit);
}
