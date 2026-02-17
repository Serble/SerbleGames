namespace SerbleGames.Backend.Schemas;

public record GameCreateRequest(string Name, string Description, decimal Price, DateTime? PublishDate, string? TrailerVideo, bool Public = false);
public record GameUpdateRequest(string? Name, string? Description, decimal? Price, DateTime? PublishDate, string? TrailerVideo, bool? Public);
