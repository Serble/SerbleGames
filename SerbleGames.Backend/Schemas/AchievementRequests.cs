namespace SerbleGames.Backend.Schemas;

public record AchievementCreateRequest(string Title, string Description, bool Hidden = false);
public record AchievementUpdateRequest(string? Title, string? Description, bool? Hidden);
