using System.Threading.Tasks;
using Godot;
using LauncherGodot.Scripts;
using SerbleGames.Client;

namespace LauncherGodot.Menus.Elements;

public partial class GameStoreEntry : Panel {
	private const float TweenDuration = 0.1f;
	
	private Control _hover;
	private Game _game;
	
	public override void _Ready() {
		_hover = GetNode<Control>("%Hover");
		_hover.Visible = false;
	}
	
	public async Task SetGame(Game game) {
		_game = game;
		ImageTexture iconTexture = await Global.GetGameIcon(game);
		if (iconTexture != null) {
			GetNode<TextureRect>("%Icon").Texture = iconTexture;
		}
		
		GetNode<Label>("%Name").Text = game.Name;
	}

	public void TileMouseEntered() {
		_hover.Visible = true;
		_hover.Modulate = new Color(_hover.Modulate, 0f);
		Tween tween = CreateTween();
		tween.TweenProperty(_hover, "modulate:a", 1f, TweenDuration).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
	}
	
	public void TileMouseExited() {
		Tween tween = CreateTween();
		tween.TweenProperty(_hover, "modulate:a", 0f, TweenDuration).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
	}

	public override void _GuiInput(InputEvent @event) {
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left) {
			GetTree().CurrentScene.GetNode<Main>(".").SelectGame(_game);
		}
	}
}