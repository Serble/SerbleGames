using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SerbleGames.Backend.Auth;
using SerbleGames.Backend.Database;
using SerbleGames.Backend.Schemas;
using SerbleGames.Backend.Schemas.Db;
using SerbleGames.Backend.Serble;

namespace SerbleGames.Backend.Routes;

[Route("/auth")]
[ApiController]
public class AuthController(IUserRepo users, ISerbleApiClient serbleApi, IJwtManager jwt) : ControllerBase {
    
    [HttpPost]
    public async Task<ActionResult<AuthResponse>> Post(AuthenticateRequest request) {
        TokenResponse? tokenResponse = await serbleApi.Authenticate(request.Code);
        if (tokenResponse == null) {
            return BadRequest("Invalid code");
        }
        
        SerbleUser info = await serbleApi.GetUserInfo(tokenResponse.AccessToken) ?? throw new Exception("Failed to get user info");
        
        GamesUser? user = await users.GetUserById(info.Id);
        if (user == null) {
            user = new GamesUser {
                Id = info.Id,
                Username = info.Username,
                RefreshToken = tokenResponse.RefreshToken
            };
            await users.CreateUser(user);
        }
        else {
            user.Username = info.Username;
            user.RefreshToken = tokenResponse.RefreshToken;
            await users.UpdateUser(user);
        }

        string backendToken = jwt.GenerateToken(user);
        
        return Ok(new AuthResponse(true, backendToken));
    }

    [Authorize]
    [HttpGet]
    public ActionResult Get() {
        return Ok();
    }
}
