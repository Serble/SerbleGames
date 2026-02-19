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
public class GameController(IGameRepo games, IPackageRepo packages, IAmazonS3 s3, IOptions<S3Settings> s3Settings, IUserRepo users, IAdminRepo adminRepo) : ControllerBase {
    private readonly S3Settings _s3Settings = s3Settings.Value;
    
    private static readonly string[] ValidPlatforms = ["windows", "linux", "mac"];
    
    [HttpPost]
    public async Task<ActionResult<Game>> Post(GameCreateRequest request) {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        GamesUser? user = await users.GetUserById(userId);
        if (user == null) return Unauthorized();

        ServerOptions options = await adminRepo.GetServerOptions();

        if (!user.IsAdmin) {
            if (options.RequireCreateWhitelist && !await adminRepo.IsUserWhitelisted(userId, WhitelistTypes.CreateGames)) {
                return StatusCode(StatusCodes.Status403Forbidden, "User is not whitelisted to create games");
            }

            if (request.Price > 0 && options.RequirePaidCreateWhitelist && !await adminRepo.IsUserWhitelisted(userId, WhitelistTypes.CreatePaidGames)) {
                return StatusCode(StatusCodes.Status403Forbidden, "User is not whitelisted to create paid games");
            }
        }

        if (options.MaxGamesPerUser > 0) {
            int currentCount = await adminRepo.CountGamesByUser(userId);
            if (currentCount >= options.MaxGamesPerUser) {
                return BadRequest("Game creation limit reached");
            }
        }

        if (!string.IsNullOrEmpty(request.TrailerVideo) && !request.TrailerVideo.Contains("youtube.com") && !request.TrailerVideo.Contains("youtu.be")) {
            return BadRequest("Trailer video must be a YouTube link");
        }

        Game game = new() {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            PublishDate = request.PublishDate ?? DateTime.UtcNow,
            TrailerVideo = request.TrailerVideo,
            OwnerId = userId,
            Public = request.Public,
            Icon = request.Icon
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

        if (!string.IsNullOrEmpty(request.TrailerVideo) && !request.TrailerVideo.Contains("youtube.com") && !request.TrailerVideo.Contains("youtu.be")) {
            return BadRequest("Trailer video must be a YouTube link");
        }

        if (request.Name != null) game.Name = request.Name;
        if (request.Description != null) game.Description = request.Description;
        if (request.Price != null) game.Price = request.Price.Value;
        if (request.PublishDate != null) game.PublishDate = request.PublishDate.Value;
        if (request.TrailerVideo != null) game.TrailerVideo = request.TrailerVideo;
        if (request.Public != null) game.Public = request.Public.Value;
        if (request.Icon != null) game.Icon = request.Icon;
        if (request.WindowsRelease != null) {
            Package? package = await packages.GetPackageById(request.WindowsRelease);
            if (package == null || package.GameId != id) {
                return BadRequest("Invalid Windows release package ID");
            }
            game.WindowsRelease = request.WindowsRelease;
        }

        if (request.LinuxRelease != null) {
            Package? package = await packages.GetPackageById(request.LinuxRelease);
            if (package == null || package.GameId != id) {
                return BadRequest("Invalid Linux release package ID");
            }
            game.LinuxRelease = request.LinuxRelease;
        }

        if (request.MacRelease != null) {
            Package? package = await packages.GetPackageById(request.MacRelease);
            if (package == null || package.GameId != id) {
                return BadRequest("Invalid Mac release package ID");
            }
            game.MacRelease = request.MacRelease;
        }

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
        
        // first delete all its packages from db and s3
        IEnumerable<Package> gamePackages = await packages.GetPackagesByGameId(id);
        foreach (Package package in gamePackages) {
            await DeletePackageData(package);
        }
        
        // then delete the achievements and their icons
        IEnumerable<Achievement> achievements = await games.GetAchievementsByGameId(id);
        foreach (Achievement achievement in achievements) {
            await DeleteAchievementData(achievement);
        }
        
        // then delete the game icon if it exists
        if (game.Icon != null) {
            string key = $"{id}/icon/{game.Icon}";
            try {
                await s3.DeleteObjectAsync(_s3Settings.BucketName, key);
            }
            catch (AmazonS3Exception e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound) {
                // Ignore if the file is not found
            }
        }

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
            PurchaseDate = DateTime.UtcNow,
            Playtime = 0,
            LastPlayed = null
        };

        await games.CreateOwnership(ownership);
        return Ok();
    }

    [HttpPost("{id}/playtime")]
    public async Task<ActionResult> AddPlaytime(string id, AddPlaytimeRequest request) {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        GameOwnership? ownership = await games.GetOwnership(userId, id);
        if (ownership == null) return Forbid("You do not own this game");

        ownership.Playtime += request.Minutes;
        ownership.LastPlayed = DateTime.UtcNow;

        await games.UpdateOwnership(ownership);
        return Ok();
    }

    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Game>>> GetPublic([FromQuery] int offset = 0, [FromQuery] int limit = 10) {
        return Ok(await games.GetPublicGames(offset, limit));
    }

    [HttpGet("public/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Game>> GetPublicGame(string id) {
        Game? game = await games.GetGameById(id);
        if (game == null || !game.Public) {
            return NotFound("Game not found");
        }
        return Ok(game);
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

    [HttpPost("{id}/package")]
    public async Task<ActionResult<PackageCreateResponse>> CreatePackage(string id, PackageCreateRequest request) {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        Game? game = await games.GetGameById(id);
        if (game == null) return NotFound("Game not found");
        if (game.OwnerId != userId) return Forbid();

        GamesUser? user = await users.GetUserById(userId);
        if (user == null) return Unauthorized();

        ServerOptions options = await adminRepo.GetServerOptions();
        if (!user.IsAdmin && options.MaxBuildsPerGame > 0) {
            int buildCount = await adminRepo.CountPackagesByGame(id);
            if (buildCount >= options.MaxBuildsPerGame) {
                return BadRequest("Build limit reached for this game");
            }
        }

        string buildId = Guid.NewGuid().ToString();
        string key = $"game/{id}/package/{buildId}";

        if (!ValidPlatforms.Contains(request.Platform)) {
            return BadRequest("Invalid platform specified, must be one of: windows, linux, mac");
        }

        Package package = new() {
            CreatedAt = DateTime.UtcNow,
            GameId = id,
            Platform = request.Platform,
            MainBinary = request.MainBinary,
            LaunchArguments = request.LaunchArguments,
            Id = buildId,
            Name = request.Name
        };
        await packages.CreatePackage(package);

        GetPreSignedUrlRequest signReq = new() {
            BucketName = _s3Settings.BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(_s3Settings.PresignExpiryMinutes)
        };

        return Ok(new PackageCreateResponse {
            Package = package,
            UploadUrl = await s3.GetPreSignedURLAsync(signReq)
        });
    }
    
    [HttpGet("{id}/package")]
    public async Task<ActionResult<Package[]>> GetPackages(string id) {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        Game? game = await games.GetGameById(id);
        if (game == null) return NotFound("Game not found");
        if (game.OwnerId != userId) return Forbid();

        return Ok(await packages.GetPackagesByGameId(id));
    }
    
    [HttpGet("{id}/package/{packageId}")]
    public async Task<ActionResult<Package>> GetPackage(string id, string packageId) {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        Game? game = await games.GetGameById(id);
        if (game == null) return NotFound("Game not found");
        if (game.OwnerId != userId) {
            GameOwnership? ownership = await games.GetOwnership(userId, id);
            if (ownership == null) return Forbid();

            bool isLive = packageId == game.WindowsRelease
                || packageId == game.LinuxRelease
                || packageId == game.MacRelease;
            if (!isLive) return Forbid();
        }

        Package? package = await packages.GetPackageById(packageId);
        if (package == null || package.GameId != id) {
            return NotFound("Package not found");
        }

        return Ok(package);
    }
    
    [HttpDelete("{id}/package/{packageId}")]
    public async Task<ActionResult<Package[]>> DeletePackage(string id, string packageId) {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        Game? game = await games.GetGameById(id);
        if (game == null) return NotFound("Game not found");
        if (game.OwnerId != userId) return Forbid();
        
        Package? package = await packages.GetPackageById(packageId);
        if (package == null || package.GameId != id) {
            return NotFound("Package not found");
        }
        
        await DeletePackageData(package);
        return NoContent();
    }
    
    private async Task DeletePackageData(Package package) {
        string key = $"game/{package.GameId}/package/{package.Id}";
        try {
            await s3.DeleteObjectAsync(_s3Settings.BucketName, key);
        }
        catch (AmazonS3Exception e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound) {
            // Ignore if the file is not found
        }
        await packages.DeletePackage(package.Id);
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
            "linux" => game.LinuxRelease,
            "windows" => game.WindowsRelease,
            "mac" => game.MacRelease,
            _ => null
        };

        if (buildId == null) return NotFound("No build found for this platform");
        string key = $"game/{id}/package/{buildId}";

        GetPreSignedUrlRequest request = new() {
            BucketName = _s3Settings.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(_s3Settings.PresignExpiryMinutes)
        };
        
        // check if the file exists in S3 before returning the URL, otherwise return 404
        try {
            await s3.GetObjectMetadataAsync(_s3Settings.BucketName, key);
        }
        catch (AmazonS3Exception e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound) {
            return NotFound("Build file not found");
        }

        return Ok(await s3.GetPreSignedURLAsync(request));
    }
    
    [HttpGet("{id}/icon")]
    [AllowAnonymous]
    public async Task<ActionResult> GetIcon(string id) {
        Game? game = await games.GetGameById(id);
        if (game?.Icon == null) {
            return NotFound();
        }

        string key = $"{id}/icon/{game.Icon}";
        try {
            GetObjectResponse response = await s3.GetObjectAsync(_s3Settings.BucketName, key);
            return File(response.ResponseStream, response.Headers.ContentType);
        }
        catch (AmazonS3Exception e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound) {
            return NotFound();
        }
    }

    [HttpPost("{id}/icon")]
    public async Task<ActionResult<string>> UpdateIcon(string id) {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        Game? game = await games.GetGameById(id);
        if (game == null) return NotFound("Game not found");
        if (game.OwnerId != userId) return Forbid();

        // Delete old icon if it exists
        if (game.Icon != null) {
            string oldKey = $"{id}/icon/{game.Icon}";
            try {
                await s3.DeleteObjectAsync(_s3Settings.BucketName, oldKey);
            }
            catch (Exception) {
                // Ignore errors during deletion of old icon
            }
        }

        string iconId = Guid.NewGuid().ToString();
        game.Icon = iconId;
        await games.UpdateGame(game);

        string key = $"{id}/icon/{iconId}";

        GetPreSignedUrlRequest request = new() {
            BucketName = _s3Settings.BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(_s3Settings.PresignExpiryMinutes)
        };

        return Ok(await s3.GetPreSignedURLAsync(request));
    }

    [HttpGet("{id}/achievements")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Achievement>>> GetAchievements(string id) {
        Game? game = await games.GetGameById(id);
        if (game == null) return NotFound("Game not found");

        return Ok(await games.GetAchievementsByGameId(id));
    }

    [HttpPost("{id}/achievements")]
    public async Task<ActionResult<Achievement>> CreateAchievement(string id, AchievementCreateRequest request) {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        Game? game = await games.GetGameById(id);
        if (game == null) return NotFound("Game not found");
        if (game.OwnerId != userId) return Forbid();

        Achievement achievement = new() {
            GameId = id,
            Title = request.Title,
            Description = request.Description,
            Hidden = request.Hidden
        };

        await games.CreateAchievement(achievement);
        return Ok(achievement);
    }

    [HttpPatch("achievement/{achievementId}")]
    public async Task<ActionResult<Achievement>> UpdateAchievement(string achievementId, AchievementUpdateRequest request) {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        Achievement? achievement = await games.GetAchievementById(achievementId);
        if (achievement == null) return NotFound("Achievement not found");

        Game? game = await games.GetGameById(achievement.GameId);
        if (game == null) return NotFound("Game not found");
        if (game.OwnerId != userId) return Forbid();

        if (request.Title != null) achievement.Title = request.Title;
        if (request.Description != null) achievement.Description = request.Description;
        if (request.Hidden != null) achievement.Hidden = request.Hidden.Value;

        await games.UpdateAchievement(achievement);
        return Ok(achievement);
    }

    [HttpDelete("achievement/{achievementId}")]
    public async Task<ActionResult> DeleteAchievement(string achievementId) {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        Achievement? achievement = await games.GetAchievementById(achievementId);
        if (achievement == null) return NotFound("Achievement not found");

        Game? game = await games.GetGameById(achievement.GameId);
        if (game == null) return NotFound("Game not found");
        if (game.OwnerId != userId) return Forbid();

        await DeleteAchievementData(achievement);
        return NoContent();
    }
    
    private async Task DeleteAchievementData(Achievement achievement) {
        if (achievement.Icon != null) {
            string key = $"achievement/{achievement.Id}/icon/{achievement.Icon}";
            try {
                await s3.DeleteObjectAsync(_s3Settings.BucketName, key);
            }
            catch (AmazonS3Exception e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound) {
                // Ignore if the file is not found
            }
        }
        await games.DeleteAchievement(achievement.Id);
    }

    [HttpPost("achievement/{achievementId}/grant/{targetUserId}")]
    public async Task<ActionResult> GrantAchievement(string achievementId, string targetUserId) {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        Achievement? achievement = await games.GetAchievementById(achievementId);
        if (achievement == null) return NotFound("Achievement not found");

        Game? game = await games.GetGameById(achievement.GameId);
        if (game == null) return NotFound("Game not found");
        if (game.OwnerId != userId) return Forbid();

        if (await games.HasAchievement(targetUserId, achievementId)) {
            return BadRequest("User already has this achievement");
        }

        UserAchievement userAchievement = new() {
            UserId = targetUserId,
            AchievementId = achievementId,
            DateEarned = DateTime.UtcNow
        };

        await games.GrantAchievement(userAchievement);
        return Ok();
    }

    [HttpGet("{id}/achievements/earned")]
    public async Task<ActionResult<IEnumerable<Achievement>>> GetEarnedAchievements(string id) {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        return Ok(await games.GetEarnedAchievements(userId, id));
    }

    [HttpGet("achievement/{achievementId}/icon")]
    [AllowAnonymous]
    public async Task<ActionResult> GetAchievementIcon(string achievementId) {
        Achievement? achievement = await games.GetAchievementById(achievementId);
        if (achievement == null || achievement.Icon == null) {
            return Redirect("/serble_logo.png"); // Default icon
        }

        string key = $"achievement/{achievementId}/icon/{achievement.Icon}";
        try {
            GetObjectResponse response = await s3.GetObjectAsync(_s3Settings.BucketName, key);
            return File(response.ResponseStream, response.Headers.ContentType);
        }
        catch (AmazonS3Exception e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound) {
            return Redirect("/serble_logo.png");
        }
    }

    [HttpPost("achievement/{achievementId}/icon")]
    public async Task<ActionResult<string>> UpdateAchievementIcon(string achievementId) {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        Achievement? achievement = await games.GetAchievementById(achievementId);
        if (achievement == null) return NotFound("Achievement not found");

        Game? game = await games.GetGameById(achievement.GameId);
        if (game == null) return NotFound("Game not found");
        if (game.OwnerId != userId) return Forbid();

        // Delete old icon if it exists
        if (achievement.Icon != null) {
            string oldKey = $"achievement/{achievementId}/icon/{achievement.Icon}";
            try {
                await s3.DeleteObjectAsync(_s3Settings.BucketName, oldKey);
            }
            catch (Exception) {
                // Ignore
            }
        }

        string iconId = Guid.NewGuid().ToString();
        achievement.Icon = iconId;
        await games.UpdateAchievement(achievement);

        string key = $"achievement/{achievementId}/icon/{iconId}";

        GetPreSignedUrlRequest request = new() {
            BucketName = _s3Settings.BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(_s3Settings.PresignExpiryMinutes)
        };

        return Ok(await s3.GetPreSignedURLAsync(request));
    }
}
