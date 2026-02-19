using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Godot;
using LauncherGodot.Menus;
using SerbleGames.Client;
using Environment = System.Environment;
using FileAccess = System.IO.FileAccess;
using HttpClient = System.Net.Http.HttpClient;

namespace LauncherGodot.Scripts;

public record InstalledGameInfo(string Name, string Path, string Exe, string Args);
public record RunningGame(DateTime StartTime, Process Process);
public record ActiveDownload(string GameId, bool IsUpdate, DownloadProgress Progress);

public class DownloadProgress {
    public double Progress {
        get => _progress;
        set {
            _progress = value;
            ProgressChanged?.Invoke(_progress);
        }
    }
    public readonly SafeEvent<double> ProgressChanged = new();
    
    private double _progress;
}

public static class InstallManager {
    private const string DataFilePath = "user://install_data.cfg";
    private const string InstallPath = "user://installed_games/";

    private static readonly ConfigFile Data = new();
    private static readonly Dictionary<string, RunningGame> RunningProcesses = new();
    public static readonly List<ActiveDownload> ActiveDownloads = [];

    public static event Action<string> GameClosed = null!;
    
    public static void Init() {
        Data.Load(DataFilePath);
    }

    public static bool IsInstalled(string gameId) {
        return Data.GetValue(gameId, "installed", false).AsBool();
    }

    public static async Task Install(Game game, bool isUpdate = false) {
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

        DownloadProgress progress = new();
        ActiveDownload activeDownload = new(game.Id, isUpdate, progress);
        lock (ActiveDownloads) {
            ActiveDownloads.Add(activeDownload);
        }

        Node currentScene = Global.Instance.GetTree().CurrentScene;
        if (currentScene is Main main) {
            main.RefreshCurrentGame(game.Id);
        }
        
        try {
            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync(s3DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? -1;
            long downloadedBytes = 0;

            await using Stream contentStream = await response.Content.ReadAsStreamAsync(),
                fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            byte[] buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0) {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                downloadedBytes += bytesRead;
                if (totalBytes > 0) {
                    progress.Progress = (double)downloadedBytes / totalBytes;
                }
            }
        }
        catch (Exception e) {
            GD.PrintErr("Failed to download game " + game.Name + ": " + e);
            Global.MessageBox("Download Error", "Failed to download game files. Please check your internet connection and try again.");
            lock (ActiveDownloads) {
                ActiveDownloads.Remove(activeDownload);
            }
            progress.Progress = -1;  // make it call update event
            return;
        }
        
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
            lock (ActiveDownloads) {
                ActiveDownloads.Remove(activeDownload);
            }
            progress.Progress = -1;  // make it call update event
            return;
        }
        
        // Download icon
        string iconPath = null;
        try {
            byte[] iconData = await AuthManager.Client.GetIcon(game.Id);
            if (iconData != null) {
                iconPath = Path.Combine(destDir, "icon.png");
                await File.WriteAllBytesAsync(iconPath, iconData);
            }
        }
        catch (Exception e) {
            GD.PrintErr("Failed to download icon for game " + game.Name + ": " + e);
            // not a critical error, just log it and continue
        }
        
        // make a shortcut to it
        string shortcutPath = LauncherShortcuts.CreateShortcut(game.Name, Path.Combine(path, package.MainBinary), iconPath);
        if (shortcutPath == null) {
            GD.Print("Failed to create shortcut for game " + game.Name + ": Unsupported platform");
        }
        else {
            GD.Print("Created shortcut for game " + game.Name + " at " + shortcutPath);
        }
        
        Data.SetValue(game.Id, "installed", true);
        Data.SetValue(game.Id, "version", package.Id);
        Data.SetValue(game.Id, "name", game.Name);
        Data.SetValue(game.Id, "path", path);
        Data.SetValue(game.Id, "exe", package.MainBinary);
        Data.SetValue(game.Id, "args", package.LaunchArguments);
        Data.SetValue(game.Id, "shortcut", shortcutPath ?? "");
        Data.Save(DataFilePath);
        lock (ActiveDownloads) {
            ActiveDownloads.Remove(activeDownload);
        }
        progress.Progress = -1;  // make it call update event
    }
    
    public static DownloadProgress GetDownloadProgress(string gameId) {
        ActiveDownload download;
        lock (ActiveDownloads) {
            download = ActiveDownloads.Find(d => d.GameId == gameId);
        }
        return download?.Progress;
    }
    
    public static bool IsDownloading(string gameId) {
        lock (ActiveDownloads) {
            return ActiveDownloads.Exists(d => d.GameId == gameId);
        }
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
    
    public static bool IsUpdateAvailable(Game game) {
        if (!IsInstalled(game.Id)) {
            return false;
        }

        string installedVersion = Data.GetValue(game.Id, "version", "").ToString();
        string latestVersion = GetRelevantRelease(game);

        if (string.IsNullOrWhiteSpace(installedVersion)) {
            GD.PrintErr("Installed version is missing for game ID " + game.Id);
            return false;
        }

        if (string.IsNullOrWhiteSpace(latestVersion)) {
            GD.PrintErr("Latest version is missing for game ID " + game.Id);
            return false;
        }
        
        return Guid.Parse(installedVersion) != Guid.Parse(latestVersion);
    }
    
    public static async Task Update(Game game) {
        if (!IsInstalled(game.Id)) {
            throw new InvalidOperationException("Game is not installed");
        }

        if (IsRunning(game.Id)) {
            throw new InvalidOperationException("Game is currently running");
        }

        Uninstall(game.Id);
        await Install(game, true);
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
        
        string shortcut = Data.GetValue(gameId, "shortcut", "").ToString();
        if (!string.IsNullOrWhiteSpace(shortcut) && File.Exists(shortcut)) {
            try {
                File.Delete(shortcut);
            }
            catch (Exception e) {
                GD.PrintErr("Failed to delete shortcut for game ID " + gameId + ": " + e);
                // not a critical error, just log it and continue
            }
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
