using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SerbleGames.Backend.Database;
using SerbleGames.Backend.Schemas;
using SerbleGames.Backend.Schemas.Db;

namespace SerbleGames.Backend.Routes;

[Route("/options")]
[ApiController]
public class OptionsController(IAdminRepo adminRepo) : ControllerBase {

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ServerOptionsResponse>> Get() {
        ServerOptions options = await adminRepo.GetServerOptions();
        return Ok(new ServerOptionsResponse(options.RequireCreateWhitelist, options.RequirePaidCreateWhitelist, options.MaxGamesPerUser, options.MaxBuildsPerGame));
    }
}
