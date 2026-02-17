using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace SerbleGames.Client;

public class SerbleGamesClient(string baseUrl = "http://localhost:5240") {
    private readonly HttpClient _httpClient = new() { BaseAddress = new Uri(baseUrl) };
    private string? _accessToken;

    public void SetAccessToken(string token) {
        _accessToken = token;
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<string> LoginWithOAuth(string clientId, string redirectUri = "http://localhost:3000/callback") {
        OAuthHelper oauth = new(clientId, redirectUri);
        string code = await oauth.GetAuthorizationCode();
        
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/auth", new { code });
        response.EnsureSuccessStatusCode();
        
        AuthResponse? authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (authResponse?.AccessToken == null) {
            throw new Exception("Failed to get access token from backend");
        }
        
        SetAccessToken(authResponse.AccessToken);
        return authResponse.AccessToken;
    }

    public async Task<UserAccountResponse?> GetAccountInfo() {
        return await _httpClient.GetFromJsonAsync<UserAccountResponse>("/account");
    }

    public async Task<PublicUserResponse?> GetPublicUserInfo(string userId) {
        return await _httpClient.GetFromJsonAsync<PublicUserResponse>($"/account/{userId}");
    }

    public async Task<IEnumerable<Game>?> GetPublicGames(int offset = 0, int limit = 10) {
        string url = QueryHelpers.AddQueryString("/game/public", new Dictionary<string, string?> {
            ["offset"] = offset.ToString(),
            ["limit"] = limit.ToString()
        });
        return await _httpClient.GetFromJsonAsync<IEnumerable<Game>>(url);
    }

    public async Task<IEnumerable<Game>?> SearchPublicGames(string query, int offset = 0, int limit = 10) {
        string url = QueryHelpers.AddQueryString("/game/search", new Dictionary<string, string?> {
            ["query"] = query,
            ["offset"] = offset.ToString(),
            ["limit"] = limit.ToString()
        });
        return await _httpClient.GetFromJsonAsync<IEnumerable<Game>>(url);
    }

    public async Task<IEnumerable<Game>?> GetGamesByUser(string userId) {
        return await _httpClient.GetFromJsonAsync<IEnumerable<Game>>($"/game/user/{userId}");
    }

    public async Task<Game?> CreateGame(GameCreateRequest request) {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/game", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Game>();
    }

    public async Task<Game?> UpdateGame(string id, GameUpdateRequest request) {
        HttpResponseMessage response = await _httpClient.PatchAsJsonAsync($"/game/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Game>();
    }

    public async Task DeleteGame(string id) {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"/game/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<Game>?> GetCreatedGames() {
        return await _httpClient.GetFromJsonAsync<IEnumerable<Game>>("/game/created");
    }

    public async Task<IEnumerable<Game>?> GetOwnedGames() {
        return await _httpClient.GetFromJsonAsync<IEnumerable<Game>>("/game/owned");
    }

    public async Task PurchaseGame(string id) {
        HttpResponseMessage response = await _httpClient.PostAsync($"/game/{id}/purchase", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> GetUploadUrl(string gameId, string platform) {
        HttpResponseMessage response = await _httpClient.PostAsync($"/game/{gameId}/release/{platform}", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadAsStringAsync()).Trim('"');
    }

    public async Task<string> GetDownloadUrl(string gameId, string platform) {
        HttpResponseMessage response = await _httpClient.GetAsync($"/game/{gameId}/download/{platform}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadAsStringAsync()).Trim('"');
    }

    public async Task UploadRelease(string gameId, string platform, Stream fileStream) {
        string uploadUrl = await GetUploadUrl(gameId, platform);
        using HttpClient uploadClient = new();
        using StreamContent content = new(fileStream);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        HttpResponseMessage response = await uploadClient.PutAsync(uploadUrl, content);
        response.EnsureSuccessStatusCode();
    }
}

public record AuthResponse(bool Success, string? AccessToken);
public record UserAccountResponse(string Id, string Username);
public record PublicUserResponse(string Id, string Username);
public record Game(string Id, string Name, string Description, decimal Price, DateTime PublishDate, string? TrailerVideo, bool Public, string? LinuxBuild, string? WindowsBuild, string? MacBuild);
public record GameCreateRequest(string Name, string Description, decimal Price, DateTime? PublishDate, string? TrailerVideo, bool Public = false);
public record GameUpdateRequest(string? Name, string? Description, decimal? Price, DateTime? PublishDate, string? TrailerVideo, bool? Public);
