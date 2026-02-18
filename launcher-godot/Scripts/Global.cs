using System.IO;
using System.Threading.Tasks;
using Godot;
using SerbleGames.Client;

namespace LauncherGodot.Scripts;

// We'll consider this script's _Ready as the entrypoint of the app.
public partial class Global : Node {
	private static Global _instance;
	
	public override void _Ready() {
		_instance = this;
		GetViewport().GetWindow().CloseRequested += () => {
			GetTree().Root.Mode = Window.ModeEnum.Minimized;
			GD.Print("Minimizing instead of closing");
		};
		
		// check login
		AuthManager.Init();
		if (!AuthManager.LoggedIn) {
			GetTree().CallDeferred("change_scene_to_file", "res://Menus/login.tscn");
		}
		else {
			GetTree().CallDeferred("change_scene_to_file", "res://Menus/main.tscn");
		}
		
		// local data
		InstallManager.Init();

		Task.Run(() => {
			GameManagementServer server = new();
			_ = server.Start();
		});
	}
	
	public static async Task GrantAchievement(string achievementId) {
		await AuthManager.Client.GrantAchievement(achievementId, (await AuthManager.GetAccountInfo()).Id);
		
		// TODO: display it or play sound or something. Maybe a notification in the corner?
		GD.Print("Granted achievement " + achievementId);
	}
	
	public static void MessageBox(string title, string message) {
		AcceptDialog dialog = new();
		dialog.Title = title;
		dialog.DialogText = message;
		_instance.AddChild(dialog);
		dialog.PopupCentered();
		dialog.CloseRequested += () => dialog.QueueFree();
	}
	
	public static async Task<ImageTexture> GetGameIcon(Game game) {
		string basePath = Path.Join(ProjectSettings.GlobalizePath("user://"), "icon_" + game.Id);
		bool hasIcon = game.Icon != null;
		
		// Check if we already have an icon with extension
		string existingIcon = FindIconWithExtension(basePath);
		
		if (existingIcon == null) {
			byte[] iconData = await AuthManager.Client.GetIcon(game.Id);
			if (iconData != null) {
				// Detect format and save with appropriate extension
				string extension = DetectImageFormat(iconData);
				if (extension != null) {
					string pathWithExtension = basePath + extension;
					await File.WriteAllBytesAsync(pathWithExtension, iconData);
					existingIcon = pathWithExtension;
				}
			}
			else {
				hasIcon = false;
			}
		}

		if (!hasIcon || existingIcon == null) return null;
		
		// Convert file path to Godot's user:// format
		string relativePath = Path.GetRelativePath(ProjectSettings.GlobalizePath("user://"), existingIcon);
		Image iconImage = Image.LoadFromFile("user://" + relativePath);
		return ImageTexture.CreateFromImage(iconImage);
	}
	
	public static async Task<ImageTexture> GetAchievementIcon(Achievement achievement) {
		string basePath = Path.Join(ProjectSettings.GlobalizePath("user://"), "achievement_icon_" + achievement.Id);
		
		// Check if we already have an icon with extension
		string existingIcon = FindIconWithExtension(basePath);
		
		if (existingIcon == null) {
			byte[] iconData = await AuthManager.Client.GetAchievementIcon(achievement.Id);
			if (iconData != null) {
				// Detect format and save with appropriate extension
				string extension = DetectImageFormat(iconData);
				if (extension != null) {
					string pathWithExtension = basePath + extension;
					await File.WriteAllBytesAsync(pathWithExtension, iconData);
					existingIcon = pathWithExtension;
				}
			}
			else {
				return null;
			}
		}

		// Convert file path to Godot's user:// format
		string relativePath = Path.GetRelativePath(ProjectSettings.GlobalizePath("user://"), existingIcon!);
		Image iconImage = Image.LoadFromFile("user://" + relativePath);
		return ImageTexture.CreateFromImage(iconImage);
	}

	private static string FindIconWithExtension(string basePath) {
		// Check for common image extensions
		string[] extensions = [".png", ".jpg", ".jpeg", ".bmp", ".webp", ".svg"];
		foreach (string ext in extensions) {
			string pathWithExt = basePath + ext;
			if (File.Exists(pathWithExt)) {
				return pathWithExt;
			}
		}
		return null;
	}

	private static string DetectImageFormat(byte[] data) {
		if (data == null || data.Length < 4) {
			return null;
		}

		// PNG: 89 50 4E 47
		if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47) {
			return ".png";
		}

		// JPEG: FF D8 FF
		if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF) {
			return ".jpg";
		}

		// BMP: 42 4D
		if (data[0] == 0x42 && data[1] == 0x4D) {
			return ".bmp";
		}

		// WebP: RIFF ... WEBP
		if (data.Length >= 12 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
			data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50) {
			return ".webp";
		}

		// GIF: 47 49 46 (GIF89a or GIF87a)
		if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46) {
			return ".gif";
		}

		// SVG: XML declaration or <svg tag (text-based)
		if (data.Length >= 4 && data[0] == 0x3C) { // '<'
			string headerStr = System.Text.Encoding.UTF8.GetString(data, 0, Mathf.Min(50, data.Length));
			if (headerStr.Contains("svg") || headerStr.Contains("<?xml")) {
				return ".svg";
			}
		}

		return null;
	}
}
