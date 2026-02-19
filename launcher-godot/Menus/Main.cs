using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using LauncherGodot.Menus.Elements;
using LauncherGodot.Scripts;
using SerbleGames.Client;

namespace LauncherGodot.Menus;

public partial class Main : Control {
	private const string Clock = "⏲";
	private const string CompatibleColour = "00853e";
	private const string IncompatibleColour = "e8282b";
	
	private Label _initialLabel;
	private Control _storeGameList;
	private Control _libraryGameList;
	private TabContainer _tabs;
	private PackedScene _gameEntryScene = GD.Load<PackedScene>("res://Menus/Elements/game_store_entry.tscn");
	private PackedScene _achievementEntryScene = GD.Load<PackedScene>("res://Menus/Elements/achievement.tscn");

	private Game _currentGame;
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
	private Control _currentGameNotRunningContainer;
	private Control _currentGameKillButton;
	private Control _currentGameDesc;
	private Control _currentGameAchievementsContainer;
	private Control _currentGameAchievementsPanel;
	private Label _currentGameAchievementCount;
	private Label _currentGameCompatibility;
	private Button _currentGamePlayButton;
	private Button _currentGameUpdateButton;
	
	public override async void _Ready() {
		try {
			_tabs = GetNode<TabContainer>("TabContainer");
			_initialLabel = GetNode<Label>("%Initial");
			_initialLabel.Text = (await AuthManager.GetAccountInfo()).Username[0].ToString();
			GetNode<Label>("%Username").Text = (await AuthManager.GetAccountInfo()).Username;
			GetNode<Label>("%Id").Text = "User ID: " + (await AuthManager.GetAccountInfo()).Id;
			
			_storeGameList = GetNode<Control>("%StoreGameEntries");
			_libraryGameList = GetNode<Control>("%LibraryGameEntries");
			
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
			_currentGameNotRunningContainer = GetNode<Control>("%CurrentGameNotRunningContainer");
			_currentGameKillButton = GetNode<Control>("%CurrentGameKillButton");
			_currentGameDesc = GetNode<Control>("%CurrentGameDesc");
			_currentGameAchievementsContainer = GetNode<Control>("%CurrentGameAchievementsContainer");
			_currentGameAchievementsPanel = GetNode<Control>("%CurrentGameAchievementsPanel");
			_currentGameAchievementCount = GetNode<Label>("%CurrentGameAchievementCount");
			_currentGameCompatibility = GetNode<Label>("%CurrentGameCompatibility");
			_currentGamePlayButton = GetNode<Button>("%CurrentGamePlayButton");
			_currentGameUpdateButton = GetNode<Button>("%CurrentGameUpdateButton");
			
			_currentGameContent.Visible = false;
			_currentGameNoContent.Visible = true;
			_tabs.CurrentTab = 0;  // library
			
			// hooks
			InstallManager.GameClosed += OnGameClosed;
			
			// now do slow stuff
			DisplayStoreEntries(await AuthManager.Client.GetPublicGames(0, 18));
			DisplayLibraryEntries(await AuthManager.Client.GetOwnedGames());
		}
		catch (Exception e) {
			GD.PrintErr("Failed to load account info: " + e);
			_initialLabel.Text = "Failed to load account info";
		}
	}

	public override void _ExitTree() {
		base._ExitTree();
		InstallManager.GameClosed -= OnGameClosed;
	}
	
	private void OnGameClosed(string gameId) {
		if (_currentGame != null && _currentGame.Id == gameId) {
			RefreshCurrentGame();
		}
	}

	public void DisplayStoreEntries(IEnumerable<Game> games) => DisplayEntries(_storeGameList, games);
	public void DisplayLibraryEntries(IEnumerable<Game> games) => DisplayEntries(_libraryGameList, games);
	
	public async void DisplayEntries(Control parent, IEnumerable<Game> games) {
		foreach (Node child in parent.GetChildren()) {
			child.QueueFree();
		}

		foreach (Game game in games) {
			GameStoreEntry entry = _gameEntryScene.Instantiate<GameStoreEntry>();
			await entry.SetGame(game);
			parent.AddChild(entry);
		}
	}
	
	public async void OnStoreSearchTextChanged(string newText) {
		try {
			// clear current entries
			foreach (Node child in _storeGameList.GetChildren()) {
				child.QueueFree();
			}

			if (string.IsNullOrWhiteSpace(newText)) {
				DisplayStoreEntries(await AuthManager.Client.GetPublicGames(0, 18));
				return;
			}
		
			DisplayStoreEntries(await AuthManager.Client.SearchPublicGames(newText));
		}
		catch (Exception e) {
			GD.PrintErr("Failed to search store games: " + e);
		}
	}
	
	public async void OnLibrarySearchTextChanged(string newText) {
		try {
			// clear current entries
			foreach (Node child in _storeGameList.GetChildren()) {
				child.QueueFree();
			}

			IEnumerable<Game> games = await AuthManager.Client.GetOwnedGames();
		
			// we have to search client-side since there's no API for searching owned games
			if (string.IsNullOrWhiteSpace(newText)) {
				DisplayLibraryEntries(games);
				return;
			}
		
			DisplayLibraryEntries(games!.Where(g => g.Name.Contains(newText, StringComparison.OrdinalIgnoreCase)));
		}
		catch (Exception e) {
			GD.PrintErr("Failed to search library games: " + e);
		}
	}

	public async void SelectGame(Game game) {
		try {
			_currentGameNoContent.Visible = false;
			_currentGameContent.Visible = true;

			IEnumerable<Game> ownedGames = await AuthManager.Client.GetOwnedGames();
			Game ownedGame = ownedGames!.FirstOrDefault(g => g.Id == game.Id);
			bool owned = ownedGame != null;
			if (ownedGame != null) {
				game = ownedGame;
			}
			_currentGame = game;

			// general info (not dependent on ownership)
			_currentGameName.Text = game.Name;
			
			// set Markdown desc (it's in gdscript so we have to use Set)
			_currentGameDesc.Set("markdown_text", "## About this game\n" + game.Description);
			
			ImageTexture iconTexture = await Global.GetGameIcon(game);
			if (iconTexture != null) {
				_currentGameIcon.Texture = iconTexture;
			}
		
			if (owned) {
				_currentGamePrice.Visible = false;
				_currentGameAddToLibrary.Visible = false;
				_currentGameInLibrary.Visible = true;
				_currentGamePlaytime.Text = Clock + " " + TimeSpan.FromMinutes(game.Playtime).ToString(@"h\h\ m\m");
				_currentGamePlaytime.Visible = true;
				_currentGameSubtext.Text = "Last played: " + (game.LastPlayed.HasValue ? game.LastPlayed.Value.ToString("g") : "Never");
				_currentGameSubtext.Visible = true;
				_currentGameCompatibility.Visible = false;

				if (InstallManager.IsInstalled(game.Id)) {
					_currentGamePlay.Visible = true;
					_currentGamePlayButton.Text = "Play";
					_currentGameInstall.Visible = false;
					if (InstallManager.IsRunning(game.Id)) {
						_currentGameNotRunningContainer.Visible = false;
						_currentGameKillButton.Visible = true;
					}
					else {
						_currentGameNotRunningContainer.Visible = true;
						_currentGameKillButton.Visible = false;

						if (InstallManager.IsUpdateAvailable(game)) {
							_currentGameUpdateButton.Visible = true;
							_currentGamePlayButton.Visible = false;
						}
						else {
							_currentGameUpdateButton.Visible = false;
							_currentGamePlayButton.Visible = true;
						}
					}
				}
				else {
					Button installButton = _currentGameInstall.GetChild<Button>(0);
					
					_currentGamePlay.Visible = false;
					_currentGameInstall.Visible = true;
					installButton.Disabled = !InstallManager.CanInstall(game);
					installButton.Text = "Install Game";
					
					if (InstallManager.IsDownloading(game.Id)) {
						installButton.Disabled = true;
						DownloadProgress progress = InstallManager.GetDownloadProgress(game.Id);
						progress.ProgressChanged.Subscribe(this, p => {
							if (p < 0) {
								return;
							}
							if (game.Id != _currentGame.Id) {
								// different game, ignore
								return;
							}
							installButton.Text = $"Downloading {p * 100:N0}%...";
						});
						
						installButton.Text = $"Downloading {progress.Progress * 100:N0}%...";
						installButton.Disabled = true;
					}
				}
				
				// achievements
				foreach (Node child in _currentGameAchievementsContainer.GetChildren()) {
					if (child is not AchievementEntry) {
						continue;
					}
					child.QueueFree();
				}

				Achievement[] achievements = (await AuthManager.Client.GetAchievements(game.Id))!.ToArray();
				_currentGameAchievementsPanel.Visible = false;  // changed if count > 0
				if (achievements.Length > 0) {
					HashSet<string> granted = 
						(await AuthManager.Client.GetEarnedAchievements(game.Id))!
						.Select(a => a.Id)
						.ToHashSet();
				
					foreach (Achievement achievement in achievements!) {
						AchievementEntry entry = _achievementEntryScene.Instantiate<AchievementEntry>();
						entry.LoadData(achievement, granted.Contains(achievement.Id));
						_currentGameAchievementsContainer.AddChild(entry);
					}
					_currentGameAchievementsPanel.Visible = true;
					_currentGameAchievementCount.Text = $"({granted.Count}/{achievements.Length})";
				}
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
				_currentGameAchievementsPanel.Visible = false;
				_currentGameCompatibility.Visible = true;

				string platformName = InstallManager.GetOsName().Capitalize();
				if (InstallManager.CanInstall(game)) {
					// compatible
					_currentGameCompatibility.Text = $"Supports {platformName}";
					_currentGameCompatibility.LabelSettings.FontColor = Color.FromHtml(CompatibleColour);
				}
				else {
					// incompatible
					_currentGameCompatibility.Text = $"Doesn't support {platformName}";
					_currentGameCompatibility.LabelSettings.FontColor = Color.FromHtml(IncompatibleColour);
				}
			}

			_tabs.CurrentTab = 3;
		}
		catch (Exception e) {
			GD.PrintErr("Failed to load game info: " + e);
			_currentGameNoContent.Visible = true;
			_currentGameContent.Visible = false;
		}
	}

	public async void OnInstallPressed() {
		try {
			await InstallManager.Install(_currentGame);
			RefreshCurrentGame();
		}
		catch (Exception e) {
			GD.PrintErr("Failed to install game: " + e);
		}
	}
	
	public async void OnUpdatePressed() {
		try {
			await InstallManager.Update(_currentGame);
			RefreshCurrentGame();
		}
		catch (Exception e) {
			GD.PrintErr("Failed to update game: " + e);
		}
	}

	public void OnPlayPressed() {
		Task.Run(() => {
			InstallManager.Launch(_currentGame.Id);
			RefreshCurrentGame();
		});
	}

	public void OpenCurrentGameInBrowser() {
		OS.ShellOpen("https://games.serble.net/game/" + _currentGame.Id);
	}

	public async void AddCurrentGameToLibrary() {
		try {
			if (_currentGame.Price > 0) {
				// can't do it here
				// open in browser
				OpenCurrentGameInBrowser();
				return;
			}
			
			await AuthManager.Client.PurchaseGame(_currentGame.Id);
			RefreshCurrentGame();
		}
		catch (Exception e) {
			GD.PrintErr("Failed to add game to library: " + e);
		}
	}

	public void Quit() {
		GetTree().Quit();
	}

	public void Reload() {
		GetTree().ReloadCurrentScene();
	}

	public void OnUninstallPressed() {
		InstallManager.Uninstall(_currentGame.Id);
		RefreshCurrentGame();
	}
	
	public async void OnKillPressed() {
		await InstallManager.Kill(_currentGame.Id);
		RefreshCurrentGame();
	}
	
	public void RefreshCurrentGame(string id = null) {
		if (id != null && _currentGame != null && _currentGame.Id != id) {
			return;
		}
		CallDeferred("RefreshCurrentGameSync");
	}

	public void RefreshCurrentGameSync() {
		SelectGame(_currentGame);
	}
	
	public void OnLogoutPressed() {
		AuthManager.Logout();
		GetTree().ChangeSceneToFile("res://Menus/login.tscn");
	}
}
