using Microsoft.AspNetCore.Mvc;
using SerbleGames.Backend.Schemas;

namespace SerbleGames.Backend.Routes;

[Route("/")]
[ApiController]
public class RootController : ControllerBase {

    [HttpGet]
    public ActionResult<ApiInformation> Get() {
        return Ok(new ApiInformation("Serble Games API", "0.0.1"));
    }
}
