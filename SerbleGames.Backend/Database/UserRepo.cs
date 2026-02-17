using Microsoft.EntityFrameworkCore;
using SerbleGames.Backend.Schemas.Db;

namespace SerbleGames.Backend.Database;

public class UserRepo(GamesDatabaseContext context) : IUserRepo {
    
    public async Task<GamesUser> CreateUser(GamesUser user) {
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public async Task<GamesUser?> GetUserById(string id) { 
        return await context.Users.FindAsync(id);
    }
    
    public async Task<GamesUser?> GetUserByUsername(string username) {
        return await context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }
    
    public async Task UpdateRefreshToken(string id, string refreshToken) {
        GamesUser? user = await context.Users.FindAsync(id);
        if (user != null) {
            user.RefreshToken = refreshToken;
            await context.SaveChangesAsync();
        }
    }

    public async Task UpdateUser(GamesUser user) {
        context.Entry(user).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }
}
