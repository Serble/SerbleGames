using System;
using System.Threading.Tasks;
using Godot;
using SerbleGames.Client;

namespace LauncherGodot.Scripts;

public static class AuthManager {
    private const string ConfigPath = "user://auth.cfg";
    
    public static SerbleGamesClient Client { get; } = new();
    public static bool LoggedIn { get; private set; }

    private static readonly ConfigFile Creds = new();
    private static UserAccountResponse _accountInfo;

    public static async Task<UserAccountResponse> GetAccountInfo() {
        if (!LoggedIn) {
            throw new InvalidOperationException("Not logged in");
        }

        _accountInfo ??= await Client.GetAccountInfo();
        return _accountInfo;
    }
    
    public static void Init() {
        Creds.Load(ConfigPath);
        Variant result = Creds.GetValue("auth", "token", "");
        if (result.VariantType == Variant.Type.Nil || string.IsNullOrEmpty(result.AsString())) {
            GD.Print("No token found, not logging in");
            return;
        }
        
        GD.Print("Initializing with token from config");
        string token = result.AsString();
        Client.SetAccessToken(token);
        LoggedIn = true;
    }

    public static async Task<bool> Login() {
        try {
            string token = await Client.LoginWithOAuth("3a41c262-81df-4dfb-b129-6a61f86fcb6f");
            if (string.IsNullOrEmpty(token)) {
                throw new Exception("Received empty token from login");
            }
        
            GD.Print("Got token: " + token);
            Client.SetAccessToken(token);
            Creds.SetValue("auth", "token", token);
            Creds.Save(ConfigPath);
            LoggedIn = true;
            return true;
        }
        catch (Exception) {
            GD.PrintErr("Failed to get token");
            return false;
        }
    }

    public static void Logout() {
        Client.SetAccessToken("");
        Creds.EraseSectionKey("auth", "token");
        Creds.Save(ConfigPath);
        LoggedIn = false;
    }
}
