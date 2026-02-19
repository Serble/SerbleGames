using SerbleGames.Backend.Schemas.Db;

namespace SerbleGames.Backend.Database;

public interface IGameRepo {
    Task<Game> CreateGame(Game game);
    Task<Game?> GetGameById(string id);
    Task<IEnumerable<Game>> GetGamesByOwnerId(string ownerId);
    Task<int> CountGamesByOwnerId(string ownerId);
    Task<IEnumerable<Game>> GetOwnedGamesByUserId(string userId);
    Task UpdateGame(Game game);
    Task DeleteGame(string id);
    Task<GameOwnership?> GetOwnership(string userId, string gameId);
    Task CreateOwnership(GameOwnership ownership);
    Task UpdateOwnership(GameOwnership ownership);
    Task<IEnumerable<Game>> GetPublicGames(int offset, int limit);
    Task<IEnumerable<Game>> SearchPublicGames(string query, int offset, int limit);
    
    // Achievements
    Task<IEnumerable<Achievement>> GetAchievementsByGameId(string gameId);
    Task<Achievement?> GetAchievementById(string id);
    Task CreateAchievement(Achievement achievement);
    Task UpdateAchievement(Achievement achievement);
    Task DeleteAchievement(string id);
    Task GrantAchievement(UserAchievement userAchievement);
    Task<IEnumerable<Achievement>> GetEarnedAchievements(string userId, string gameId);
    Task<bool> HasAchievement(string userId, string achievementId);
}
