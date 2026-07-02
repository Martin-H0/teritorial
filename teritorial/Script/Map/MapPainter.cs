using Godot;

namespace Teritorial.Map;

// editor mapy F6, hrac to neuvidi
[Tool]
public partial class MapPainter : Node2D
{
	[Export] public int MapWidth = 512;
	[Export] public int MapHeight = 512;
	[Export] public int BrushRadius = 8;
	[Export] public TerrainType CurrentBrush = TerrainType.Land;
	[Export] public string MapName = "map01";
	[Export] public string SavePath = "res://maps/data/map01.tres";

	[Export] public string ImportPngPath = "";

	private byte[] _terrain = null!;
	private ImageTexture _preview = null!;
	private Sprite2D _sprite = null!;
	private Label _helpLabel = null!;

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<Sprite2D>("Preview");
		if (_sprite == null)
		{
			_sprite = new Sprite2D { Name = "Preview", Centered = false };
			AddChild(_sprite);
		}

		if (!Engine.IsEditorHint())
		{
			CreateHelpLabel();
			SetProcess(true);
		}

		if (!TryLoadExisting())
			ResetTerrain(TerrainType.Water);
	}

	private void CreateHelpLabel()
	{
		_helpLabel = new Label
		{
			Position = new Vector2(8, 8),
			ZIndex = 100,
		};
		_helpLabel.AddThemeColorOverride("font_color", Colors.White);
		_helpLabel.AddThemeColorOverride("font_shadow_color", Colors.Black);
		_helpLabel.AddThemeConstantOverride("shadow_offset_x", 1);
		_helpLabel.AddThemeConstantOverride("shadow_offset_y", 1);
		AddChild(_helpLabel);
		UpdateHelpText();
	}

	private void UpdateHelpText()
	{
		if (_helpLabel == null)
			return;

		_helpLabel.Text =
			$"Soubor: {SavePath}\n" +
			$"Štětec: {BrushName(CurrentBrush)}  |  poloměr: {BrushRadius}\n" +
			"LMB táhnutí = kreslit  |  1/2/3 = typ  |  kolečko = velikost\n" +
			"S = uložit  |  L = načíst  |  R = nová mapa (celá voda)";
	}

	private static string BrushName(TerrainType type) => type switch
	{
		TerrainType.Water => "voda",
		TerrainType.Mountains => "hory",
		_ => "pevnina",
	};

	private bool TryLoadExisting()
	{
		if (!ResourceLoader.Exists(SavePath))
			return false;

		var data = MapData.TryLoad(SavePath);
		if (data == null || data.Terrain.Length != data.Width * data.Height)
			return false;

		MapWidth = data.Width;
		MapHeight = data.Height;
		MapName = data.MapName;
		_terrain = (byte[])data.Terrain.Clone();
		RefreshPreview();
		GD.Print($"Mapa načtena pro editaci: {SavePath}");
		return true;
	}

	private void ResetTerrain(TerrainType fill)
	{
		_terrain = new byte[MapWidth * MapHeight];
		byte value = (byte)fill;
		for (int i = 0; i < _terrain.Length; i++)
			_terrain[i] = value;
		RefreshPreview();
		GD.Print($"Nová mapa: celá {BrushName(fill)}");
	}

	private void RefreshPreview()
	{
		var rgb = Image.CreateEmpty(MapWidth, MapHeight, false, Image.Format.Rgb8);
		for (int y = 0; y < MapHeight; y++)
		for (int x = 0; x < MapWidth; x++)
		{
			var terrain = (TerrainType)_terrain[y * MapWidth + x];
			rgb.SetPixel(x, y, MapBaker.ColorForTerrain(terrain));
		}

		_preview = ImageTexture.CreateFromImage(rgb);
		_sprite.Texture = _preview;
		_sprite.Position = Vector2.Zero;
	}

	// drzim LMB a maluju v _Process
	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
			return;

		if (!Input.IsMouseButtonPressed(MouseButton.Left))
			return;

		if (TryPaintAtMouse())
			RefreshPreview();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (Engine.IsEditorHint())
			return;

		if (@event is InputEventKey key && key.Pressed && !key.Echo)
		{
			switch (key.Keycode)
			{
				case Key.Key1:
					CurrentBrush = TerrainType.Land;
					UpdateHelpText();
					GD.Print("Štětec: pevnina");
					GetViewport().SetInputAsHandled();
					return;
				case Key.Key2:
					CurrentBrush = TerrainType.Water;
					UpdateHelpText();
					GD.Print("Štětec: voda");
					GetViewport().SetInputAsHandled();
					return;
				case Key.Key3:
					CurrentBrush = TerrainType.Mountains;
					UpdateHelpText();
					GD.Print("Štětec: hory");
					GetViewport().SetInputAsHandled();
					return;
				case Key.S:
					BakeAndSave();
					GetViewport().SetInputAsHandled();
					return;
				case Key.L:
					TryLoadExisting();
					GetViewport().SetInputAsHandled();
					return;
				case Key.R:
					ResetTerrain(TerrainType.Water);
					GetViewport().SetInputAsHandled();
					return;
			}
		}

		if (@event is InputEventMouseButton wheel
			&& wheel.Pressed
			&& (wheel.ButtonIndex == MouseButton.WheelUp || wheel.ButtonIndex == MouseButton.WheelDown))
		{
			BrushRadius += wheel.ButtonIndex == MouseButton.WheelUp ? 1 : -1;
			BrushRadius = Mathf.Clamp(BrushRadius, 1, 64);
			UpdateHelpText();
			GD.Print($"Poloměr štětce: {BrushRadius}");
			GetViewport().SetInputAsHandled();
		}
	}

	private bool TryPaintAtMouse()
	{
		var local = _sprite.GetLocalMousePosition();
		int x = (int)local.X;
		int y = (int)local.Y;

		// mimo mapu nic
		if (x < 0 || y < 0 || x >= MapWidth || y >= MapHeight)
			return false;

		PaintCircle(x, y, BrushRadius, (byte)CurrentBrush);
		return true;
	}

	private void PaintCircle(int cx, int cy, int radius, byte value)
	{
		int r2 = radius * radius;
		for (int py = cy - radius; py <= cy + radius; py++)
		for (int px = cx - radius; px <= cx + radius; px++)
		{
			if (px < 0 || py < 0 || px >= MapWidth || py >= MapHeight)
				continue;
			if ((px - cx) * (px - cx) + (py - cy) * (py - cy) > r2)
				continue;

			_terrain[py * MapWidth + px] = value;
		}
	}

	public void BakeAndSave()
	{
		var mapData = MapBaker.BakeMapData(_terrain, MapWidth, MapHeight, MapName);
		var err = ResourceSaver.Save(mapData, SavePath);
		if (err == Error.Ok)
			GD.Print($"Mapa uložena: {SavePath}");
		else
			GD.PushError($"Uložení mapy selhalo: {err}");
	}

	public void ImportFromPng()
	{
		if (string.IsNullOrWhiteSpace(ImportPngPath) || !ResourceLoader.Exists(ImportPngPath))
		{
			GD.PushError($"Import PNG: soubor neexistuje ({ImportPngPath})");
			return;
		}

		var tex = ResourceLoader.Load<Texture2D>(ImportPngPath);
		var image = tex.GetImage();
		var pixels = MapBaker.TerrainFromImage(image, MapWidth, MapHeight);
		if (pixels == null)
			return;

		_terrain = pixels;
		RefreshPreview();
		GD.Print($"Importováno z {ImportPngPath}");
	}

#if TOOLS
	[ExportToolButton("Bake and Save Map")]
	private Callable BakeButton => Callable.From(BakeAndSave);

	[ExportToolButton("Import from PNG")]
	private Callable ImportButton => Callable.From(ImportFromPng);

	[ExportToolButton("New Map (all water)")]
	private Callable ResetButton => Callable.From(() => ResetTerrain(TerrainType.Water));
#endif
}
