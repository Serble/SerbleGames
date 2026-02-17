using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LauncherGodot.Menus.Elements;
using LauncherGodot.Scripts;
using SerbleGames.Client;

namespace LauncherGodot.Menus;

public partial class Main : Control {
	private const string Clock = "⏲";
	
	private Label _initialLabel;
	private Control _gameList;
	private TabContainer _tabs;
	private PackedScene _gameEntryScene = GD.Load<PackedScene>("res://Menus/Elements/game_store_entry.tscn");

	private Control _currentGameContent;
	private Control _currentGameNoContent;
	private TextureRect _currentGameIcon;
	private Label _currentGameName;
	private Label _currentGameSubtext;
	private Label _currentGamePrice;
	private Label _currentGamePlaytime;
	private Label _currentGameInLibrary;
	private Control _currentGameAddToLibrary;
	private Control _currentGamePlay;
	private Control _currentGameInstall;
	
	public override async void _Ready() {
		try {
			_tabs = GetNode<TabContainer>("TabContainer");
			_initialLabel = GetNode<Label>("%Initial");
			_initialLabel.Text = (await AuthManager.GetAccountInfo()).Username[0].ToString();
			GetNode<Label>("%Username").Text = (await AuthManager.GetAccountInfo()).Username;
			GetNode<Label>("%Id").Text = "User ID: " + (await AuthManager.GetAccountInfo()).Id;
			
			_gameList = GetNode<Control>("%GameEntries");
			DisplayStoreEntries(await AuthManager.Client.GetPublicGames());
			
			_currentGameContent = GetNode<Control>("%CurrentGameContent");
			_currentGameNoContent = GetNode<Control>("%CurrentGameNoContent");
			_currentGameIcon = GetNode<TextureRect>("%CurrentGameIcon");
			_currentGameName = GetNode<Label>("%CurrentGameName");
			_currentGameSubtext = GetNode<Label>("%CurrentGameSubtext");
			_currentGamePrice = GetNode<Label>("%CurrentGamePrice");
			_currentGamePlaytime = GetNode<Label>("%CurrentGamePlaytime");
			_currentGameInLibrary = GetNode<Label>("%CurrentGameInLibrary");
			_currentGameAddToLibrary = GetNode<Control>("%CurrentGameAddToLibrary");
			_currentGamePlay = GetNode<Control>("%CurrentGamePlay");
			_currentGameInstall = GetNode<Control>("%CurrentGameInstall");
		}
		catch (Exception e) {
			GD.PrintErr("Failed to load account info: " + e);
			_initialLabel.Text = "Failed to load account info";
		}
	}

	public async void DisplayStoreEntries(IEnumerable<Game> games) {
		foreach (Node child in _gameList.GetChildren()) {
			child.QueueFree();
		}

		foreach (Game game in games) {
			GameStoreEntry entry = _gameEntryScene.Instantiate<GameStoreEntry>();
			await entry.SetGame(game);
			_gameList.AddChild(entry);
		}
	}
	
	public async void OnStoreSearchTextChanged(string newText) {
		// clear current entries
		foreach (Node child in _gameList.GetChildren()) {
			child.QueueFree();
		}

		if (string.IsNullOrWhiteSpace(newText)) {
			DisplayStoreEntries(await AuthManager.Client.GetPublicGames());
			return;
		}
		
		DisplayStoreEntries(await AuthManager.Client.SearchPublicGames(newText));
	}

	public async void SelectGame(Game game) {
		try {
			_currentGameNoContent.Visible = false;
			_currentGameContent.Visible = true;

			IEnumerable<Game> ownedGames = await AuthManager.Client.GetOwnedGames();
			bool owned = ownedGames!.Any(g => g.Id == game.Id);

			// general info (not dependent on ownership)
			_currentGameName.Text = game.Name;
			_currentGameSubtext.Text = game.Description;
			ImageTexture iconTexture = await Global.GetGameIcon(game);
			if (iconTexture != null) {
				_currentGameIcon.Texture = iconTexture;
			}
		
			if (owned) {
				_currentGamePrice.Visible = false;
				_currentGameAddToLibrary.Visible = false;
				_currentGameInLibrary.Visible = true;
				// _currentGamePlay.Visible = true;
				_currentGamePlaytime.Text = Clock + " " + TimeSpan.FromSeconds(game.Playtime).ToString(@"hh\:mm\:ss");
				_currentGamePlaytime.Visible = true;
				_currentGameInstall.Visible = true;
				_currentGameSubtext.Text = "Last played: " + (game.LastPlayed.HasValue ? game.LastPlayed.Value.ToString("g") : "Never");
				_currentGameSubtext.Visible = true;
			}
			else {
				_currentGamePrice.Text = game.Price == 0 ? "FREE" : "$" + game.Price.ToString("0.00");
				_currentGamePrice.Visible = true;
				_currentGameAddToLibrary.Visible = true;
				_currentGameInLibrary.Visible = false;
				_currentGamePlay.Visible = false;
				_currentGameInstall.Visible = false;
				_currentGamePlaytime.Visible = false;
				_currentGameSubtext.Visible = false;
			}

			_tabs.CurrentTab = 3;
		}
		catch (Exception e) {
			GD.PrintErr("Failed to load game info: " + e);
			_currentGameNoContent.Visible = true;
			_currentGameContent.Visible = false;
		}
	}
	
	public void OnLogoutPressed() {
		AuthManager.Logout();
		GetTree().ChangeSceneToFile("res://Menus/login.tscn");
	}
}
