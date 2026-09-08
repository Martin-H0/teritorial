using Godot;
using Teritorial.Map;

namespace Teritorial.Game;

public partial class GameController : Node2D
{
	[Export] public GameConfig Config = null!;

	private GameConfig _cfg = null!;
	private TerrainGrid _terrain = null!;
	private OwnershipGrid _ownership = null!;
	private PopulationGrid _population = null!;
	private TerritoryRenderer _overlay = null!;
	private GameHud _hud = null!;
	private ExpeditionSystem _expeditions = null!;
	private BotAI? _bots;
	private float _tickTimer;
	private int _playerCount;
	private int _totalLand;
	private bool _gameEnded;

	public override void _Ready()
	{
		// Vždy načteme přes LoadOrDefault(), které zkontroluje user:// nastavení z menu.
		// Config ze scény použijeme jen pokud LoadOrDefault() nic nenajde (nemělo by nastat).
		_cfg = GameConfig.LoadOrDefault() ?? Config ?? new GameConfig();

		string mapPath = GameSession.SelectedMapPath ?? _cfg.MapDataPath;
		GameSession.Clear();

		if (!ResourceLoader.Exists(mapPath))
		{
			GD.PushError($"Mapa nenalezena: {mapPath}");
			return;
		}

		_terrain = TerrainGrid.Load(mapPath);
		_totalLand = _terrain.CountClaimableLand();
		_ownership = new OwnershipGrid(_terrain.Width, _terrain.Height);
		_population = new PopulationGrid(_terrain.Width, _terrain.Height);
		_playerCount = 1 + _cfg.BotCount;

		_expeditions = new ExpeditionSystem(_terrain, _ownership, _population, _cfg);
		if (_cfg.BotCount > 0)
			_bots = new BotAI(_terrain, _ownership, _population, _expeditions, _cfg, _cfg.BotCount, _cfg.SpawnSeed);

		AddChild(new Sprite2D
		{
			Name = "MapVisual",
			Texture = _terrain.Data.VisualTexture,
			Centered = false,
		});

		SpawnPlayers();

		_overlay = new TerritoryRenderer { Name = "TerritoryOverlay", ZIndex = 1 };
		AddChild(_overlay);
		_overlay.Setup(_terrain.Width, _terrain.Height);
		_overlay.Rebuild(_ownership, _terrain);

		_hud = new GameHud { Name = "Hud" };
		AddChild(_hud);
		_hud.BindConfirm(OnConfirmNaval, OnCancelNaval);
		_hud.BindRestart(OnRestart);
		_hud.BindMainMenu(OnMainMenu);

		SetupCamera(_terrain.Width, _terrain.Height);
		RefreshHud();

		GD.Print($"Start: {_terrain.Data.MapName}, hráč + {_cfg.BotCount} botů");
	}

	private void SpawnPlayers()
	{
		var spawns = SpawnPlacement.Place(
			_terrain,
			_playerCount,
			_cfg.MinIslandArea,
			_cfg.SpawnBlobRadius,
			_cfg.SpawnClearanceRadius,
			_cfg.SpawnSeed);

		int target = OwnershipGrid.TargetPixelsForRadius(_cfg.SpawnBlobRadius);
		int maxExpand = _cfg.SpawnBlobRadius * 3;

		foreach (var spawn in spawns)
		{
			_ownership.FillLandBlob(spawn.X, spawn.Y, target, maxExpand, spawn.OwnerId, _terrain);
			_population.SeedPlayer(spawn.OwnerId, _ownership, _cfg.StartingPopulation);

			string who = spawn.OwnerId == 0 ? "hráč" : $"bot {spawn.OwnerId}";
			GD.Print($"Spawn {who}: ({spawn.X}, {spawn.Y})");
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_gameEnded)
			return;
		if (@event is not InputEventMouseButton mouse || !mouse.Pressed)
			return;
		if (_hud.IsMouseOverUi(mouse.Position))
			return;

		var mapPos = ScreenToMap(mouse.Position);
		if (mapPos == null)
			return;

		string result;
		if (mouse.ButtonIndex == MouseButton.Left)
		{
			result = _expeditions.TryLandAttack(mapPos.Value.X, mapPos.Value.Y, _hud.SendPercent);
			if (result.Length > 0)
			{
				_hud.SetStatus(result);
				GD.Print(result);
			}

			_hud.ShowNavalConfirm(false);
			_overlay.Rebuild(_ownership, _terrain);
		}
		else if (mouse.ButtonIndex == MouseButton.Right)
		{
			result = _expeditions.TryPrepareNavalAttack(mapPos.Value.X, mapPos.Value.Y, _hud.SendPercent);
			if (result.Length > 0)
			{
				_hud.SetStatus(result);
				GD.Print(result);
			}

			_hud.ShowNavalConfirm(_expeditions.HasPendingNavalAttack);
		}
		else
		{
			return;
		}

		RefreshHud();
		GetViewport().SetInputAsHandled();
	}

	private void OnConfirmNaval()
	{
		string result = _expeditions.ConfirmNavalAttack();
		_hud.SetStatus(result);
		_hud.ShowNavalConfirm(false);
		GD.Print(result);
		_overlay.Rebuild(_ownership, _terrain);
		RefreshHud();
	}

	private void OnCancelNaval()
	{
		_expeditions.CancelNavalAttack();
		_hud.ShowNavalConfirm(false);
		_hud.SetStatus("Námořní útok zrušen");
		RefreshHud();
	}

	public override void _Process(double delta)
	{
		if (_gameEnded)
			return;

		_tickTimer += (float)delta;
		if (_tickTimer < _cfg.TickInterval)
			return;

		float dt = _tickTimer;
		_tickTimer = 0;

		for (short id = 0; id < _playerCount; id++)
			PopulationSimulation.Tick(_population, _ownership, id, dt, _cfg);

		bool changed = false;
		if (_expeditions.Tick(dt, out bool expChanged) && expChanged)
			changed = true;

		if (_bots != null && _bots.Tick(dt))
			changed = true;

		if (changed)
			_overlay.Rebuild(_ownership, _terrain);

		if (changed)
			_overlay.Rebuild(_ownership, _terrain);

		RefreshHud();
		CheckVictory();
	}

	private void CheckVictory()
	{
		if (_gameEnded)
			return;

		var outcome = VictoryChecker.Evaluate(
			_terrain, _ownership, PopulationSimulation.PlayerId, _playerCount, _cfg);
		if (outcome == null)
			return;

		_gameEnded = true;
		_hud.ShowEndScreen(outcome.Value);
		GD.Print(outcome.Value.Message);
	}

	private void OnRestart() => GetTree().ReloadCurrentScene();

	private void OnMainMenu()
	{
		GameSession.Clear();
		GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
	}

	private void RefreshHud()
	{
		float pop = _population.GetTotal(PopulationSimulation.PlayerId, _ownership);
		int land = _population.CountLand(PopulationSimulation.PlayerId, _ownership);
		float maxPop = PopulationSimulation.GetMaxPopulation(land, _cfg);
		float growth = PopulationSimulation.ComputeGrowth(pop, land, 1f, _cfg);
		float mapPct = _totalLand > 0 ? (float)land / _totalLand : 0f;
		_hud.UpdatePlayerStats(
			pop, land, growth, maxPop,
			_expeditions.ActivePlayerExpeditionCount, _expeditions.MaxExpeditions,
			mapPct, _cfg.WinLandPercent);
	}

	private Vector2I? ScreenToMap(Vector2 screenPos)
	{
		var cam = GetViewport().GetCamera2D();
		if (cam == null)
			return null;

		Vector2 world = cam.GetCanvasTransform().AffineInverse() * screenPos;
		int x = (int)world.X;
		int y = (int)world.Y;
		if (x < 0 || y < 0 || x >= _terrain.Width || y >= _terrain.Height)
			return null;

		return new Vector2I(x, y);
	}

	private void SetupCamera(int mapW, int mapH)
	{
		var cam = GetNodeOrNull<CameraController>("Camera2D");
		if (cam == null)
			return;

		cam.MapSize = new Vector2(mapW, mapH);
		cam.Position = new Vector2(mapW * 0.5f, mapH * 0.5f);
		cam.MakeCurrent();
	}
}
