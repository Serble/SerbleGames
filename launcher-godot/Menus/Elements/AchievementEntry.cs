using System;
using Godot;
using LauncherGodot.Scripts;
using SerbleGames.Client;

namespace LauncherGodot.Menus.Elements;

public partial class AchievementEntry : Control {
    private TextureRect _icon;
    private Label _name;
    private Label _subtext;
    private Panel _panel;
    
    private Material _greyscale = GD.Load<Material>("res://Assets/greyscale.tres");

    public async void LoadData(Achievement achievement, bool unlocked) {
        try {
            _icon = GetNode<TextureRect>("%Icon");
            _name = GetNode<Label>("%Name");
            _subtext = GetNode<Label>("%Subtext");
            _panel = GetNode<Panel>("%Panel");
            
            if (achievement == null) {
                GD.PrintErr("Achievement is null");
                return;
            }
            _name.Text = achievement.Title;
            _subtext.Text = achievement.Description;
            _icon.Texture = await Global.GetAchievementIcon(achievement);
            _panel.ThemeTypeVariation = unlocked ? "GrantedAchievement" : "";

            if (!unlocked) {
                _icon.Material = _greyscale;
            }
        }
        catch (Exception e) {
            GD.PrintErr("Failed to load achievement data: " + e);
        }
    }
}
