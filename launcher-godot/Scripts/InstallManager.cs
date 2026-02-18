using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Godot;
using SerbleGames.Client;
using Environment = System.Environment;
using HttpClient = System.Net.Http.HttpClient;

namespace LauncherGodot.Scripts;

public record InstalledGameInfo(string Name, string Path, string Exe, string Args);
public record RunningGame(DateTime StartTime, Process Process);

public static class InstallManager {
    private const string DataFilePath = "user://install_data.cfg";
    private const string InstallPath = "user://installed_games/";

    private static readonly ConfigFile Data = new();
    private static readonly Dictionary<string, RunningGame> RunningProcesses = new();

    public static event Action<string> GameClosed = null!;
    
    public static void Init() {
        Data.Load(DataFilePath);
    }

    public static bool IsInstalled(string gameId) {
        return Data.GetValue(gameId, "installed", false).AsBool();
    }

    public static async Task Install(Game game) {
        if (!CanInstall(game)) {
            throw new InvalidOperationException("Game is not compatible with this platform");
        }

        if (IsInstalled(game.Id)) {
            throw new InvalidOperationException("Game is already installed");
        }

        Package package = await AuthManager.Client.GetPackage(game.Id, GetRelevantRelease(game));
        if (package == null) {
            throw new InvalidOperationException("Failed to get package for game " + game.Name);
        }
        
        string path = ProjectSettings.GlobalizePath(InstallPath + game.Id + "/compressed");
        string s3DownloadUrl = await AuthManager.Client.GetDownloadUrl(game.Id, GetOsName());
        
        // make sure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        
        FileStream file = File.Open(path, FileMode.Create);
        using (HttpClient client = new()) {
            using HttpResponseMessage response = await client.GetAsync(s3DownloadUrl);
            response.EnsureSuccessStatusCode();
            await response.Content.CopyToAsync(file);
        }
        file.Close();
        
        // if it's a zip, extract it. However, we can't check the extension
        // because they're uploaded without it, so we just have to assume it's a zip and try to extract it,
        // and if it fails, we can just leave it as is
        string destDir = ProjectSettings.GlobalizePath(InstallPath + game.Id);
        try {
            ZipFile.ExtractToDirectory(path, destDir);
            File.Delete(path);
            path = destDir;
        }
        catch (Exception e) {
            GD.PrintErr("Failed to extract game " + game.Name + ": " + e);
            Global.MessageBox("Installation Error", "Failed to extract game files. The game may be corrupted or not properly packaged. Please try reinstalling or contact support.");
            return;
        }
        
        Data.SetValue(game.Id, "installed", true);
        Data.SetValue(game.Id, "name", game.Name);
        Data.SetValue(game.Id, "path", path);
        Data.SetValue(game.Id, "exe", package.MainBinary);
        Data.SetValue(game.Id, "args", package.LaunchArguments);
        Data.Save(DataFilePath);
    }
    
    public static InstalledGameInfo[] GetInstalledGames() {
        string[] gameIds = Data.GetSectionKeys("installed");
        InstalledGameInfo[] games = new InstalledGameInfo[gameIds.Length];
        for (int i = 0; i < gameIds.Length; i++) {
            string gameId = gameIds[i];
            if (Data.GetValue(gameId, "installed", false).AsBool()) {
                games[i] = new InstalledGameInfo(
                    Data.GetValue(gameId, "name", "").ToString(),
                    Data.GetValue(gameId, "path", "").ToString(),
                    Data.GetValue(gameId, "exe", "").ToString(),
                    Data.GetValue(gameId, "args", "").ToString()
                );
            }
        }

        return games;
    }
    
    public static bool IsRunning(string gameId) {
        return RunningProcesses.ContainsKey(gameId);
    }

    public static void Launch(string gameId) {
        if (!IsInstalled(gameId)) {
            throw new InvalidOperationException("Game is not installed");
        }

        string path = Data.GetValue(gameId, "path", "").ToString();
        string exe = Data.GetValue(gameId, "exe", "").ToString();
        string args = Data.GetValue(gameId, "args", "").ToString();

        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(exe)) {
            throw new InvalidOperationException("Invalid game data for game ID " + gameId);
        }

        string fullPath = Path.Combine(path, exe);
        if (!File.Exists(fullPath)) {
            throw new FileNotFoundException("Executable not found for game ID " + gameId);
        }

        Process proc = Process.Start(new ProcessStartInfo {
            FileName = fullPath,
            Arguments = args,
            WorkingDirectory = path
        });

        if (proc == null || proc.HasExited) {
            GD.PrintErr("Failed to launch game with ID " + gameId);
            return;  // we tried
        }

        proc.WaitForExitAsync().ContinueWith(__ => {
            GD.Print("Game with ID " + gameId + " has exited");
            RunningGame val = RunningProcesses[gameId];
            RunningProcesses.Remove(gameId);
            
            TimeSpan delta = DateTime.UtcNow - val.StartTime;
            Task _ = AuthManager.Client.AddPlaytime(gameId, (int)delta.TotalMinutes);
            GD.Print("Added " + (int)delta.TotalMinutes + " minutes of playtime for game ID " + gameId);
            GameClosed?.Invoke(gameId);
        });
        RunningProcesses[gameId] = new RunningGame(DateTime.UtcNow, proc);
        GD.Print("Launched game with ID " + gameId);
    }

    public static void Uninstall(string gameId) {
        if (!IsInstalled(gameId)) {
            throw new InvalidOperationException("Game is not installed");
        }
        
        if (IsRunning(gameId)) {
            throw new InvalidOperationException("Game is currently running");
        }
        
        string path = Data.GetValue(gameId, "path", "").ToString();
        if (string.IsNullOrWhiteSpace(path)) {
            throw new InvalidOperationException("Invalid game data for game ID " + gameId);
        }
        
        try {
            Directory.Delete(path, true);
        }
        catch (Exception e) {
            GD.PrintErr("Failed to delete game files for game ID " + gameId + ": " + e);
            throw new InvalidOperationException("Failed to delete game files");
        }
        
        Data.EraseSection(gameId);
        Data.Save(DataFilePath);
    }
    
    public static Task Kill(string gameId) {
        RunningProcesses[gameId].Process.Kill();
        return RunningProcesses[gameId].Process.WaitForExitAsync();
    }
    
    public static string GetRelevantRelease(Game game) {
        return Environment.OSVersion.Platform switch {
            PlatformID.Win32NT => game.WindowsRelease,
            PlatformID.Unix => game.LinuxRelease,
            PlatformID.MacOSX => game.MacRelease,
            _ => null
        };
    }
    
    public static string GetOsName() {
        return Environment.OSVersion.Platform switch {
            PlatformID.Win32NT => "windows",
            PlatformID.Unix => "linux",
            PlatformID.MacOSX => "mac",
            _ => ""
        };
    }

    public static bool CanInstall(Game game) {
        string release = GetRelevantRelease(game);

        if (string.IsNullOrWhiteSpace(release)) {
            GD.Print($"Linnux release ({game.Name}): " + game.LinuxRelease);
            return false;
        }
        
        return true;
    }
}
