using Godot;

namespace Teritorial.Map;

// teren nacteny z mapy, vlastnictvi je jinde
public sealed class TerrainGrid
{
	public MapData Data { get; }
	public int Width => Data.Width;
	public int Height => Data.Height;

	private TerrainGrid(MapData data) => Data = data;

	public static TerrainGrid Load(string resourcePath)
	{
		var data = MapData.TryLoad(resourcePath);
		if (data == null)
			throw new System.InvalidOperationException($"MapData nelze načíst: {resourcePath}");

		if (data.Terrain == null || data.Terrain.Length != data.Width * data.Height)
			throw new System.InvalidOperationException($"MapData má neplatná terénní data: {resourcePath}");

		return new TerrainGrid(data);
	}

	public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

	public TerrainType GetTerrain(int x, int y) =>
		(TerrainType)Data.Terrain[y * Width + x];

	public bool IsClaimable(int x, int y) =>
		InBounds(x, y) && GetTerrain(x, y) == TerrainType.Land;

	public bool IsPassable(int x, int y) =>
		InBounds(x, y) && GetTerrain(x, y) != TerrainType.Mountains;

	public bool IsWater(int x, int y) =>
		InBounds(x, y) && GetTerrain(x, y) == TerrainType.Water;

	public int CountClaimableLand()
	{
		int n = 0;
		foreach (byte t in Data.Terrain)
		{
			if (t == (byte)TerrainType.Land)
				n++;
		}

		return n;
	}
}
