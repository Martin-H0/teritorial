using Godot;

namespace Teritorial.Map;

// byte[] -> MapData + barvy
public static class MapBaker
{
	// barvy nesmis menit jinak se rozbije map painter
	public static readonly Color LandColor = new(0.36f, 0.62f, 0.28f);
	public static readonly Color WaterColor = new(0.18f, 0.38f, 0.78f);
	public static readonly Color MountainsColor = new(0.48f, 0.42f, 0.36f);

	public static MapData BakeMapData(byte[] terrain, int width, int height, string mapName)
	{
		if (terrain.Length != width * height)
		{
			GD.PushError($"BakeMapData: špatná délka pole ({terrain.Length} != {width * height})");
			return null!;
		}

		var visual = Image.CreateEmpty(width, height, false, Image.Format.Rgb8);
		for (int y = 0; y < height; y++)
		for (int x = 0; x < width; x++)
			visual.SetPixel(x, y, ColorForTerrain((TerrainType)terrain[y * width + x]));

		return new MapData
		{
			MapName = mapName,
			Width = width,
			Height = height,
			Terrain = terrain,
			VisualTexture = ImageTexture.CreateFromImage(visual),
		};
	}

	public static Color ColorForTerrain(TerrainType type) => type switch
	{
		TerrainType.Water => WaterColor,
		TerrainType.Mountains => MountainsColor,
		_ => LandColor,
	};

	// kdybych delal mapu mimo godot
	public static byte[] TerrainFromImage(Image image, int expectedWidth, int expectedHeight)
	{
		if (image.GetWidth() != expectedWidth || image.GetHeight() != expectedHeight)
		{
			GD.PushError(
				$"TerrainFromImage: rozměry {image.GetWidth()}x{image.GetHeight()} != {expectedWidth}x{expectedHeight}");
			return null;
		}

		if (!image.IsCompressed())
			image.Decompress();

		var pixels = new byte[expectedWidth * expectedHeight];
		for (int y = 0; y < expectedHeight; y++)
		for (int x = 0; x < expectedWidth; x++)
			pixels[y * expectedWidth + x] = (byte)ClassifyPixel(image.GetPixel(x, y));

		return pixels;
	}

	private static TerrainType ClassifyPixel(Color c)
	{
		float best = float.MaxValue;
		var bestType = TerrainType.Land;

		foreach (var (type, color) in new[]
				 {
					 (TerrainType.Land, LandColor),
					 (TerrainType.Water, WaterColor),
					 (TerrainType.Mountains, MountainsColor),
				 })
		{
			float dr = c.R - color.R;
			float dg = c.G - color.G;
			float db = c.B - color.B;
			float d = dr * dr + dg * dg + db * db;
			if (d < best)
			{
				best = d;
				bestType = type;
			}
		}

		return bestType;
	}
}
