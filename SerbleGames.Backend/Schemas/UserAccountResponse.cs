namespace SerbleGames.Backend.Schemas;

public record UserAccountResponse(string Id, string Username, bool IsAdmin, bool IsBanned, string[] Permissions);
public record PublicUserResponse(string Id, string Username);
