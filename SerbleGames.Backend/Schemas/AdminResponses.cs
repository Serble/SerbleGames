namespace SerbleGames.Backend.Schemas;

public record AdminUserResponse(string Id, string Username, bool IsAdmin, bool IsBanned, string[] Permissions, bool WhitelistedCreateGames, bool WhitelistedCreatePaidGames);
public record ServerOptionsResponse(bool RequireCreateWhitelist, bool RequirePaidCreateWhitelist, int MaxGamesPerUser, int MaxBuildsPerGame);
public record ImpersonateResponse(string Token);
