using SerbleGames.Backend.Schemas.Db;

namespace SerbleGames.Backend.Schemas;

public class PackageCreateResponse {
    public Package Package { get; set; } = null!;
    public string UploadUrl { get; set; } = null!;
}
