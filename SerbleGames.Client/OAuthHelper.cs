using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace SerbleGames.Client;

public class OAuthHelper(string clientId, string redirectUri) {
    private const string OAuthUrl = "https://serble.net/oauth/authorize";
    private const string Scope = "user_info";

    public async Task<string> GetAuthorizationCode() {
        Uri uri = new(redirectUri);
        int port = uri.Port;
        if (port == -1) port = 80;

        string state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        
        Dictionary<string, string?> query = new() {
            ["response_type"] = "token",
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["scope"] = Scope,
            ["state"] = state
        };
        
        string authUrl = QueryHelpers.AddQueryString(OAuthUrl, query);
        
        TaskCompletionSource<string> tcs = new();

        using HttpListener listener = new();
        listener.Prefixes.Add($"http://*:{port}/");
        listener.Start();

        OpenBrowser(authUrl);

        while (true) {
            HttpListenerContext context = await listener.GetContextAsync();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            if (request.Url?.AbsolutePath != uri.AbsolutePath) {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Close();
                continue;
            }

            string? receivedState = request.QueryString["state"];
            string? code = request.QueryString["code"];
            string? authorized = request.QueryString["authorized"];

            if (receivedState != state) {
                await SendResponse(response, "Invalid state", (int)HttpStatusCode.BadRequest);
                tcs.SetException(new Exception("Invalid state received"));
                break;
            }

            if (authorized != "true") {
                await SendResponse(response, "Authorization denied", (int)HttpStatusCode.Forbidden);
                tcs.SetException(new Exception("Authorization denied by user"));
                break;
            }

            if (string.IsNullOrEmpty(code)) {
                await SendResponse(response, "No code received", (int)HttpStatusCode.BadRequest);
                tcs.SetException(new Exception("No authorization code received"));
                break;
            }

            await SendResponse(response, "Authentication successful! You can close this window.", (int)HttpStatusCode.OK);
            tcs.SetResult(code);
            break;
        }

        listener.Stop();
        return await tcs.Task;
    }

    private static async Task SendResponse(HttpListenerResponse response, string message, int statusCode) {
        response.StatusCode = statusCode;
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes($"<html><body><h3>{message}</h3></body></html>");
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.Close();
    }

    private static void OpenBrowser(string url) {
        try {
            Process.Start(url);
        } catch {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                Process.Start("xdg-open", url);
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                Process.Start("open", url);
            } else {
                throw;
            }
        }
    }
}
