using System.IO;
using System.Threading.Tasks;
using Godot;
using SerbleGames.Client;

namespace LauncherGodot.Scripts;

// We'll consider this script's _Ready as the entrypoint of the app.
public partial class Global : Node {
	
	public override void _Ready() {
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
	}
	
	public static async Task<ImageTexture> GetGameIcon(Game game) {
		string iconPath = Path.Join(ProjectSettings.GlobalizePath("user://"), "icon_" + game.Id + ".png");
		bool hasIcon = game.Icon != null;
		if (!File.Exists(iconPath)) {
			byte[] iconData = await AuthManager.Client.GetIcon(game.Id);
			if (iconData != null) {
				await File.WriteAllBytesAsync(iconPath, iconData);
			}
			else {
				hasIcon = false;
			}
		}

		if (!hasIcon) return null;
		Image iconImage = Image.LoadFromFile("user://" + "icon_" + game.Id + ".png");
		return ImageTexture.CreateFromImage(iconImage);

	}
}
