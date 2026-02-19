using Microsoft.EntityFrameworkCore;
using SerbleGames.Backend.Schemas.Db;

namespace SerbleGames.Backend.Database;

public class PackageRepo(GamesDatabaseContext context) : IPackageRepo {
    public async Task<Package> CreatePackage(Package package) {
        context.Packages.Add(package);
        await context.SaveChangesAsync();
        return package;
    }

    public async Task<Package?> GetPackageById(string id) {
        return await context.Packages.FindAsync(id);
    }

    public async Task<Package[]> GetPackagesByGameId(string gameId) {
        return await context.Packages.Where(p => p.GameId == gameId).ToArrayAsync();
    }

    public async Task<int> CountPackagesByGameId(string gameId) {
        return await context.Packages.CountAsync(p => p.GameId == gameId);
    }

    public async Task UpdatePackage(Package package) {
        context.Entry(package).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    public async Task DeletePackage(string id) {
        Package? package = await context.Packages.FindAsync(id);
        if (package != null) {
            context.Packages.Remove(package);
            await context.SaveChangesAsync();
        }
    }
}
