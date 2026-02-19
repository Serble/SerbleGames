using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace LauncherGodot.Scripts;

public static class LauncherShortcuts {
    
    /// <summary>
    /// Creates a launcher shortcut if possible.
    /// </summary>
    /// <param name="name">Name of app.</param>
    /// <param name="path">Path to executable.</param>
    /// <param name="iconPath">Path to icon.</param>
    /// <returns>The path to the shortcut file.</returns>
    public static string CreateShortcut(string name, string path, string iconPath) {
        string safeName = string.Concat(name.Split(Path.GetInvalidFileNameChars()));
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            // Windows: Create .lnk in Start Menu Programs
            string startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            string shortcutPath = Path.Combine(startMenu, "Programs", $"{safeName}.lnk");
            CreateWindowsShortcut(shortcutPath, path, iconPath);
            return shortcutPath;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            // Linux: Create .desktop file in local applications dir
            string localApps = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "applications");
            Directory.CreateDirectory(localApps);
            string desktopPath = Path.Combine(localApps, $"{safeName.ToLower()}.desktop");
            CreateLinuxDesktopFile(desktopPath, name, path, iconPath);
            return desktopPath;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            // macOS: how can we do this?
        }

        return null;
    }
    
    private static void CreateWindowsShortcut(string shortcutPath, string execPath, string iconPath) {
        // Late bind COM to avoid requiring Windows Scripting lib reference
#pragma warning disable CA1416  // Yes it's Windows specific, that's the point
        Type wsh = Type.GetTypeFromProgID("WScript.Shell");
#pragma warning restore CA1416
        dynamic shell = Activator.CreateInstance(wsh!);
        dynamic shortcut = shell!.CreateShortcut(shortcutPath);
        shortcut.TargetPath = execPath;
        shortcut.IconLocation = iconPath;
        shortcut.Save();
    }

    private static void CreateLinuxDesktopFile(string path, string appName, string execPath, string iconPath) {
        string desktopEntry =
            $"""
             [Desktop Entry]
             Type=Application
             Name={appName}
             Exec="{execPath}"
             Icon={iconPath}
             Terminal=false
             Categories=Utility;

             """;
        File.WriteAllText(path, desktopEntry);
        // Make the .desktop file executable
        try {
            Process.Start("chmod", $"+x \"{path}\"");
        }
        catch {
            // Ignore, we can do little about it
        }
    }
}
