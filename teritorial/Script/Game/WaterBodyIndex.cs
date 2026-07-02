using System.Collections.Generic;
using Teritorial.Map;

namespace Teritorial.Game;

// ktere jezera/more jsou spojeny (kvuli lodim)
public sealed class WaterBodyIndex
{
	private readonly int[] _component;
	public int Width { get; }
	public int Height { get; }

	public WaterBodyIndex(TerrainGrid terrain)
	{
		Width = terrain.Width;
		Height = terrain.Height;
		int n = Width * Height;
		_component = new int[n];
		for (int i = 0; i < n; i++)
			_component[i] = -1;

		int nextId = 0;
		for (int y = 0; y < Height; y++)
		for (int x = 0; x < Width; x++)
		{
			if (!terrain.IsWater(x, y) || _component[y * Width + x] >= 0)
				continue;
			Flood(terrain, x, y, nextId++);
		}
	}

	public int GetComponent(int x, int y)
	{
		if (x < 0 || y < 0 || x >= Width || y >= Height)
			return -1;
		return _component[y * Width + x];
	}

	public bool SameWater(int x1, int y1, int x2, int y2)
	{
		int a = GetComponent(x1, y1);
		int b = GetComponent(x2, y2);
		return a >= 0 && a == b;
	}

	private void Flood(TerrainGrid terrain, int sx, int sy, int id)
	{
		var q = new Queue<(int x, int y)>();
		q.Enqueue((sx, sy));
		_component[sy * Width + sx] = id;

		while (q.Count > 0)
		{
			var (x, y) = q.Dequeue();
			Try(x + 1, y);
			Try(x - 1, y);
			Try(x, y + 1);
			Try(x, y - 1);
		}

		void Try(int x, int y)
		{
			if (!terrain.InBounds(x, y) || !terrain.IsWater(x, y))
				return;
			int idx = y * Width + x;
			if (_component[idx] >= 0)
				return;
			_component[idx] = id;
			q.Enqueue((x, y));
		}
	}
}
