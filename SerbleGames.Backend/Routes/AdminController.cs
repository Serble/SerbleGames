using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SerbleGames.Backend.Auth;
using SerbleGames.Backend.Database;
using SerbleGames.Backend.Schemas;
using SerbleGames.Backend.Schemas.Db;

namespace SerbleGames.Backend.Routes;

[Route("/admin")]
[ApiController]
[Authorize]
public class AdminController(IUserRepo users, IAdminRepo adminRepo, IJwtManager jwt) : ControllerBase {
    private static bool IsValidWhitelistType(string type) =>
        type == WhitelistTypes.CreateGames || type == WhitelistTypes.CreatePaidGames;

    private async Task<GamesUser?> GetAdminUser() {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return null;
        GamesUser? user = await users.GetUserById(userId);
        return user != null && user.IsAdmin ? user : null;
    }

    [HttpGet("users/{id}")]
    public async Task<ActionResult<AdminUserResponse>> GetUser(string id) {
        if (await GetAdminUser() == null) return Forbid();

        GamesUser? user = await users.GetUserById(id);
        if (user == null) return NotFound("User not found");

        string[] permissions = await adminRepo.GetUserPermissions(id);
        bool createWhitelisted = await adminRepo.IsUserWhitelisted(id, WhitelistTypes.CreateGames);
        bool paidWhitelisted = await adminRepo.IsUserWhitelisted(id, WhitelistTypes.CreatePaidGames);

        return Ok(new AdminUserResponse(user.Id, user.Username, user.IsAdmin, user.IsBanned, permissions, createWhitelisted, paidWhitelisted));
    }

    [HttpPut("users/{id}/permissions")]
    public async Task<ActionResult> SetPermissions(string id, UpdatePermissionsRequest request) {
        if (await GetAdminUser() == null) return Forbid();

        GamesUser? user = await users.GetUserById(id);
        if (user == null) return NotFound("User not found");

        await adminRepo.SetUserPermissions(id, request.Permissions ?? Array.Empty<string>());
        return NoContent();
    }

    [HttpPost("users/{id}/ban")]
    public async Task<ActionResult> SetBan(string id, SetBanRequest request) {
        if (await GetAdminUser() == null) return Forbid();

        GamesUser? user = await users.GetUserById(id);
        if (user == null) return NotFound("User not found");

        user.IsBanned = request.IsBanned;
        await users.UpdateUser(user);
        return NoContent();
    }

    [HttpPost("users/{id}/admin")]
    public async Task<ActionResult> SetAdmin(string id, SetAdminRequest request) {
        if (await GetAdminUser() == null) return Forbid();

        GamesUser? user = await users.GetUserById(id);
        if (user == null) return NotFound("User not found");

        user.IsAdmin = request.IsAdmin;
        await users.UpdateUser(user);
        return NoContent();
    }

    [HttpPost("users/{id}/impersonate")]
    public async Task<ActionResult<ImpersonateResponse>> Impersonate(string id) {
        if (await GetAdminUser() == null) return Forbid();

        GamesUser? user = await users.GetUserById(id);
        if (user == null) return NotFound("User not found");
        if (user.IsBanned) return StatusCode(StatusCodes.Status403Forbidden, "User is banned");

        string token = jwt.GenerateToken(user);
        return Ok(new ImpersonateResponse(token));
    }

    [HttpGet("options")]
    public async Task<ActionResult<ServerOptionsResponse>> GetOptions() {
        if (await GetAdminUser() == null) return Forbid();

        ServerOptions options = await adminRepo.GetServerOptions();
        return Ok(new ServerOptionsResponse(options.RequireCreateWhitelist, options.RequirePaidCreateWhitelist, options.MaxGamesPerUser, options.MaxBuildsPerGame));
    }

    [HttpPut("options")]
    public async Task<ActionResult<ServerOptionsResponse>> UpdateOptions(UpdateServerOptionsRequest request) {
        if (await GetAdminUser() == null) return Forbid();
        if (request.MaxGamesPerUser < 0 || request.MaxBuildsPerGame < 0) return BadRequest("Limits cannot be negative");

        ServerOptions options = await adminRepo.GetServerOptions();
        options.RequireCreateWhitelist = request.RequireCreateWhitelist;
        options.RequirePaidCreateWhitelist = request.RequirePaidCreateWhitelist;
        options.MaxGamesPerUser = request.MaxGamesPerUser;
        options.MaxBuildsPerGame = request.MaxBuildsPerGame;

        await adminRepo.UpdateServerOptions(options);
        return Ok(new ServerOptionsResponse(options.RequireCreateWhitelist, options.RequirePaidCreateWhitelist, options.MaxGamesPerUser, options.MaxBuildsPerGame));
    }

    [HttpPut("whitelist/{userId}")]
    public async Task<ActionResult> SetWhitelist(string userId, [FromQuery] string type, SetWhitelistRequest request) {
        if (await GetAdminUser() == null) return Forbid();
        if (!IsValidWhitelistType(type)) return BadRequest("Invalid whitelist type");

        await adminRepo.SetWhitelist(userId, type, request.IsWhitelisted);
        return NoContent();
    }

    [HttpGet("whitelist")]
    public async Task<ActionResult<string[]>> GetWhitelist([FromQuery] string type) {
        if (await GetAdminUser() == null) return Forbid();
        if (!IsValidWhitelistType(type)) return BadRequest("Invalid whitelist type");

        return Ok(await adminRepo.GetWhitelistUsers(type));
    }
}
