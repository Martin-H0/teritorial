using System.Collections.Generic;
using Godot;
using Teritorial.Map;

namespace Teritorial.Game;

public partial class MainMenu : Control
{
	private const string GameScenePath = "res://scenes/Game.tscn";

	private OptionButton _mapSelect = null!;
	private readonly List<string> _mapPaths = new();
	private Control? _settingsOverlay;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);

		var bg = new ColorRect
		{
			Color = new Color(0.08f, 0.1f, 0.14f),
			AnchorsPreset = (int)LayoutPreset.FullRect,
		};
		AddChild(bg);

		var center = new CenterContainer
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
		};
		AddChild(center);

		var panel = new PanelContainer { CustomMinimumSize = new Vector2(360, 0) };
		center.AddChild(panel);

		var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		panel.AddChild(box);

		box.AddChild(new Label
		{
			Text = "Teritorial",
			HorizontalAlignment = HorizontalAlignment.Center,
		});

		box.AddChild(new Label
		{
			Text = "Strategická hra o území",
			HorizontalAlignment = HorizontalAlignment.Center,
		});

		box.AddChild(new HSeparator());

		box.AddChild(new Label { Text = "Mapa:" });
		_mapSelect = new OptionButton { CustomMinimumSize = new Vector2(280, 0) };
		box.AddChild(_mapSelect);

		var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		var startBtn = new Button { Text = "Hrát", CustomMinimumSize = new Vector2(120, 36) };
		var quitBtn = new Button { Text = "Ukončit", CustomMinimumSize = new Vector2(120, 36) };
		buttons.AddChild(startBtn);
		buttons.AddChild(quitBtn);
		box.AddChild(buttons);

		// Tlačítko Nastavení – celá šířka panelu
		box.AddChild(new HSeparator());
		var settingsBtn = new Button
		{
			Text = "Nastaveni",
			CustomMinimumSize = new Vector2(280, 36),
		};
		box.AddChild(settingsBtn);

		startBtn.Pressed += OnStart;
		quitBtn.Pressed += OnQuit;
		settingsBtn.Pressed += OnOpenSettings;

		PopulateMaps();
		if (_mapPaths.Count == 0)
		{
			_mapSelect.AddItem("(žádná mapa v maps/data/)");
			startBtn.Disabled = true;
		}
	}

	private void PopulateMaps()
	{
		_mapPaths.Clear();
		_mapSelect.Clear();

		using var dir = DirAccess.Open("res://maps/data");
		if (dir == null)
		{
			GD.PushWarning("Složka maps/data neexistuje");
			return;
		}

		dir.ListDirBegin();
		string file = dir.GetNext();
		while (file.Length > 0)
		{
			if (file.EndsWith(".remap"))
                file = file[..^".remap".Length];
			if (!dir.CurrentIsDir() && file.EndsWith(".tres"))
			{
				string path = $"res://maps/data/{file}";
				var data = MapData.TryLoad(path);
				string label = data != null && data.MapName.Length > 0 ? data.MapName : file;
				_mapSelect.AddItem(label);
				_mapPaths.Add(path);
			}

			file = dir.GetNext();
		}

		dir.ListDirEnd();

		// default mapa z configu
		var cfg = GameConfig.LoadOrDefault();
		int idx = _mapPaths.IndexOf(cfg.MapDataPath);
		if (idx >= 0)
			_mapSelect.Select(idx);
	}

	private void OnStart()
	{
		if (_mapPaths.Count == 0)
			return;

		int idx = _mapSelect.Selected;
		if (idx < 0 || idx >= _mapPaths.Count)
			idx = 0;

		GameSession.SelectedMapPath = _mapPaths[idx];
		GetTree().ChangeSceneToFile(GameScenePath);
	}

	private void OnQuit() => GetTree().Quit();

	private void OnOpenSettings()
	{
		// Pokud panel už existuje, jen ho zobrazíme
		if (_settingsOverlay != null)
		{
			_settingsOverlay.Visible = true;
			return;
		}

		var settings = new SettingsMenu
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
		};
		settings.OnClose = () =>
		{
			if (_settingsOverlay != null)
				_settingsOverlay.Visible = false;
		};

		AddChild(settings);
		_settingsOverlay = settings;
	}
}
