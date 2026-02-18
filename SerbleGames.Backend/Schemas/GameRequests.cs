namespace SerbleGames.Backend.Schemas;

public record GameCreateRequest(string Name, string Description, decimal Price, DateTime? PublishDate, string? TrailerVideo, bool Public = false, string? Icon = null);
public record GameUpdateRequest(string? Name, string? Description, decimal? Price, DateTime? PublishDate, string? TrailerVideo, bool? Public, string? Icon, string? WindowsRelease, string? LinuxRelease, string? MacRelease);
public record AddPlaytimeRequest(double Minutes);
public record PackageCreateRequest(string Name, string GameId, string Platform, string MainBinary, string LaunchArguments);
