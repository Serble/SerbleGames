namespace SerbleGames.Backend.Schemas;

public record UpdatePermissionsRequest(string[] Permissions);
public record SetBanRequest(bool IsBanned);
public record SetAdminRequest(bool IsAdmin);
public record SetWhitelistRequest(bool IsWhitelisted);
public record UpdateServerOptionsRequest(bool RequireCreateWhitelist, bool RequirePaidCreateWhitelist, int MaxGamesPerUser, int MaxBuildsPerGame);
