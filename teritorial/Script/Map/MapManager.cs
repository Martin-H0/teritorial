using Godot;
using Teritorial.Game;

namespace Teritorial.Map;

// stara verze, ted je GameController
public partial class MapManager : Node2D
{
	[Export] public string MapDataPath = "res://maps/data/map01.tres";
	[Export] public int BotCount = 2;
	[Export] public int SpawnBlobRadius = 12;
	[Export] public int SpawnClearanceRadius = 28;
	[Export] public int MinIslandArea = 800;
	[Export] public ulong SpawnSeed;

	private TerrainGrid _terrain = null!;
	private OwnershipGrid _ownership = null!;

	public override void _Ready()
	{
		if (!ResourceLoader.Exists(MapDataPath))
		{
			GD.PushError($"Mapa nenalezena: {MapDataPath} — nejdřív ji ulož z MapPainteru (S)");
			return;
		}

		_terrain = TerrainGrid.Load(MapDataPath);
		_ownership = new OwnershipGrid(_terrain.Width, _terrain.Height);

		var mapSprite = new Sprite2D
		{
			Name = "MapVisual",
			Texture = _terrain.Data.VisualTexture,
			Centered = false,
		};
		AddChild(mapSprite);

		int totalPlayers = 1 + BotCount;
		var spawns = SpawnPlacement.Place(
			_terrain,
			totalPlayers,
			MinIslandArea,
			SpawnBlobRadius,
			SpawnClearanceRadius,
			SpawnSeed);

		foreach (var spawn in spawns)
		{
			int target = OwnershipGrid.TargetPixelsForRadius(SpawnBlobRadius);
			int maxExpand = SpawnBlobRadius * 3;
			int pixels = _ownership.FillLandBlob(spawn.X, spawn.Y, target, maxExpand, spawn.OwnerId, _terrain);
			string label = spawn.OwnerId == 0 ? "hráč" : $"bot {spawn.OwnerId}";
			GD.Print($"Spawn {label}: ({spawn.X}, {spawn.Y}), {pixels}/{target} px");
		}

		var overlay = new TerritoryRenderer { Name = "TerritoryOverlay", ZIndex = 1 };
		AddChild(overlay);
		overlay.Setup(_terrain.Width, _terrain.Height);
		overlay.Rebuild(_ownership, _terrain);

		SetupCamera(_terrain.Width, _terrain.Height);

		GD.Print($"Mapa: {_terrain.Data.MapName} ({_terrain.Width}x{_terrain.Height}), hráč + {BotCount} botů");
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
