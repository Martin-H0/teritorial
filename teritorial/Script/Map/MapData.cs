using Godot;

namespace Teritorial.Map;

// ulozena mapa z editoru
[GlobalClass]
public partial class MapData : Resource
{
	[Export] public string MapName { get; set; } = "";
	[Export] public int Width { get; set; }
	[Export] public int Height { get; set; }

	// width*height terrain typu
	[Export] public byte[] Terrain { get; set; } = [];

	// nahled barvy
	[Export] public ImageTexture VisualTexture { get; set; } = null!;

	// Load<MapData> mi obcas hazelo chybu takze TryLoad
	public static MapData? TryLoad(string path)
	{
		if (!ResourceLoader.Exists(path))
			return null;

		var resource = ResourceLoader.Load(path);
		if (resource is MapData data)
			return data;

		GD.PushError(
			$"Soubor {path} není MapData. Nejdřív v Godotu: Build → Build Solution (nebo horní tlačítko Build).");
		return null;
	}
}
