namespace SerbleGames.Backend.Schemas;

public record GameCreateRequest(string Name, string Description, decimal Price, DateTime? PublishDate, string? TrailerVideo, bool Public = false, string? Icon = null);
public record GameUpdateRequest(string? Name, string? Description, decimal? Price, DateTime? PublishDate, string? TrailerVideo, bool? Public, string? Icon);
public record AddPlaytimeRequest(double Minutes);
