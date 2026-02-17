using Godot;
using LauncherGodot.Scripts;

namespace LauncherGodot.Menus;

public partial class Login : Control {
	private Button _loginButton;
	
	public override void _Ready() {
		_loginButton = GetNode<Button>("%LoginButton");
	}

	public async void OnLoginPressed() {
		_loginButton.Disabled = true;
		bool success = await AuthManager.Login();
		if (success) {
			GetTree().ChangeSceneToFile("res://Menus/main.tscn");
		}
		else {
			_loginButton.Disabled = false;
			GD.PrintErr("Login failed");
		}
	}
}
