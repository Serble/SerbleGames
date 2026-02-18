using SerbleGames.Backend.Schemas.Db;

namespace SerbleGames.Backend.Database;

public interface IPackageRepo {
    public Task<Package> CreatePackage(Package package);
    public Task<Package?> GetPackageById(string id);
    public Task<Package[]> GetPackagesByGameId(string gameId);
    public Task UpdatePackage(Package package);
    public Task DeletePackage(string id);
}
