using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SerbleGames.Backend.Database;
using SerbleGames.Backend.Schemas;
using SerbleGames.Backend.Schemas.Db;

namespace SerbleGames.Backend.Routes;

[Route("/account")]
[ApiController]
[Authorize]
public class AccountController(IUserRepo users) : ControllerBase {
    
    [HttpGet]
    public async Task<ActionResult<UserAccountResponse>> Get() {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) {
            return Unauthorized();
        }
        
        GamesUser? user = await users.GetUserById(userId);
        if (user == null) {
            return NotFound("User not found in local database");
        }
        
        return Ok(new UserAccountResponse(user.Id, user.Username));
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicUserResponse>> GetPublic(string id) {
        GamesUser? user = await users.GetUserById(id);
        if (user == null) {
            return NotFound("User not found");
        }
        
        return Ok(new PublicUserResponse(user.Id, user.Username));
    }
}
