using System.Security.Claims;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SerbleGames.Backend.Database;
using SerbleGames.Backend.Schemas;
using SerbleGames.Backend.Schemas.Config;
using SerbleGames.Backend.Schemas.Db;

namespace SerbleGames.Backend.Routes;

[Route("/game")]
[ApiController]
[Authorize]
public class GameController(IGameRepo games, IAmazonS3 s3, IOptions<S3Settings> s3Settings) : ControllerBase {
    private readonly S3Settings _s3Settings = s3Settings.Value;
    
    [HttpPost]
    public async Task<ActionResult<Game>> Post(GameCreateRequest request) {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (request.TrailerVideo != null && !request.TrailerVideo.Contains("youtube.com") && !request.TrailerVideo.Contains("youtu.be")) {
            return BadRequest("Trailer video must be a YouTube link");
        }

        Game game = new() {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            PublishDate = request.PublishDate ?? DateTime.UtcNow,
            TrailerVideo = request.TrailerVideo,
            OwnerId = userId,
            Public = request.Public
        };

        await games.CreateGame(game);
        return Ok(game);
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<Game>> Patch(string id, GameUpdateRequest request) {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        Game? game = await games.GetGameById(id);
        if (game == null) return NotFound("Game not found");

        if (game.OwnerId != userId) return Forbid();

        if (request.TrailerVideo != null && !request.TrailerVideo.Contains("youtube.com") && !request.TrailerVideo.Contains("youtu.be")) {
            return BadRequest("Trailer video must be a YouTube link");
        }

        if (request.Name != null) game.Name = request.Name;
        if (request.Description != null) game.Description = request.Description;
        if (request.Price != null) game.Price = request.Price.Value;
        if (request.PublishDate != null) game.PublishDate = request.PublishDate.Value;
        if (request.TrailerVideo != null) game.TrailerVideo = request.TrailerVideo;
        if (request.Public != null) game.Public = request.Public.Value;

        await games.UpdateGame(game);
        return Ok(game);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id) {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        Game? game = await games.GetGameById(id);
        if (game == null) return NotFound("Game not found");

        if (game.OwnerId != userId) return Forbid();

        await games.DeleteGame(id);
        return NoContent();
    }

    [HttpGet("created")]
    public async Task<ActionResult<IEnumerable<Game>>> GetCreated() {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        return Ok(await games.GetGamesByOwnerId(userId));
    }

    [HttpGet("owned")]
    public async Task<ActionResult<IEnumerable<Game>>> GetOwned() {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        return Ok(await games.GetOwnedGamesByUserId(userId));
    }

    [HttpPost("{id}/purchase")]
    public async Task<ActionResult> Purchase(string id) {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        Game? game = await games.GetGameById(id);
        if (game == null) return NotFound("Game not found");

        if (game.Price > 0) {
            // TODO: Implement payment system
            return StatusCode(501, "Purchasing games with a price is not yet implemented");
        }

        GameOwnership? existing = await games.GetOwnership(userId, id);
        if (existing != null) return BadRequest("You already own this game");

        GameOwnership ownership = new() {
            UserId = userId,
            GameId = id,
            PurchaseDate = DateTime.UtcNow
        };

        await games.CreateOwnership(ownership);
        return Ok();
    }

    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Game>>> GetPublic([FromQuery] int offset = 0, [FromQuery] int limit = 10) {
        return Ok(await games.GetPublicGames(offset, limit));
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Game>>> Search([FromQuery] string query, [FromQuery] int offset = 0, [FromQuery] int limit = 10) {
        return Ok(await games.SearchPublicGames(query, offset, limit));
    }

    [HttpGet("user/{userId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Game>>> GetByUser(string userId) {
        IEnumerable<Game> userGames = await games.GetGamesByOwnerId(userId);
        return Ok(userGames.Where(g => g.Public));
    }

    [HttpPost("{id}/release/{platform}")]
    public async Task<ActionResult<string>> UpdateRelease(string id, string platform) {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        Game? game = await games.GetGameById(id);
        if (game == null) return NotFound("Game not found");
        if (game.OwnerId != userId) return Forbid();

        string buildId = Guid.NewGuid().ToString();
        string key = $"{id}/{platform}/{buildId}";

        switch (platform.ToLower()) {
            case "linux": game.LinuxBuild = buildId; break;
            case "windows": game.WindowsBuild = buildId; break;
            case "mac": game.MacBuild = buildId; break;
            default: return BadRequest("Invalid platform. Must be linux, windows, or mac.");
        }

        await games.UpdateGame(game);

        GetPreSignedUrlRequest request = new() {
            BucketName = _s3Settings.BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(_s3Settings.PresignExpiryMinutes)
        };

        return Ok(await s3.GetPreSignedURLAsync(request));
    }

    [HttpGet("{id}/download/{platform}")]
    public async Task<ActionResult<string>> GetDownloadUrl(string id, string platform) {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        Game? game = await games.GetGameById(id);
        if (game == null) return NotFound("Game not found");

        // Allow access if the user is the creator OR if they have an ownership record
        if (game.OwnerId != userId) {
            GameOwnership? ownership = await games.GetOwnership(userId, id);
            if (ownership == null) return Forbid();
        }

        string? buildId = platform.ToLower() switch {
            "linux" => game.LinuxBuild,
            "windows" => game.WindowsBuild,
            "mac" => game.MacBuild,
            _ => null
        };

        if (buildId == null) return NotFound("No build found for this platform");

        string key = $"{id}/{platform.ToLower()}/{buildId}";

        GetPreSignedUrlRequest request = new() {
            BucketName = _s3Settings.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(_s3Settings.PresignExpiryMinutes)
        };

        return Ok(await s3.GetPreSignedURLAsync(request));
    }
}
