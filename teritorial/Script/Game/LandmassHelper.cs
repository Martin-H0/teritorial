using System.Collections.Generic;
using Godot;
using Teritorial.Map;

namespace Teritorial.Game;

internal static class LandmassHelper
{
	internal static readonly Vector2I[] Dirs =
	[
		new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
	];

	public static Vector2I? NearestLand(TerrainGrid terrain, int cx, int cy, int radius = 24)
	{
		if (terrain.IsClaimable(cx, cy))
			return new Vector2I(cx, cy);

		int r2 = radius * radius;
		Vector2I? best = null;
		int bestD = int.MaxValue;
		for (int dy = -radius; dy <= radius; dy++)
		for (int dx = -radius; dx <= radius; dx++)
		{
			if (dx * dx + dy * dy > r2)
				continue;
			int x = cx + dx;
			int y = cy + dy;
			if (!terrain.IsClaimable(x, y))
				continue;
			int d = dx * dx + dy * dy;
			if (d < bestD)
			{
				bestD = d;
				best = new Vector2I(x, y);
			}
		}

		return best;
	}

	// cely ostrov bez ohledu na vlastnika
	public static HashSet<long> FloodLandmass(TerrainGrid terrain, int sx, int sy)
	{
		var region = new HashSet<long>();
		if (!terrain.IsClaimable(sx, sy))
			return region;

		var q = new Queue<Vector2I>();
		q.Enqueue(new Vector2I(sx, sy));
		region.Add(CellKey.Pack(sx, sy));

		while (q.Count > 0)
		{
			var p = q.Dequeue();
			foreach (var d in Dirs)
			{
				int x = p.X + d.X;
				int y = p.Y + d.Y;
				if (!terrain.IsClaimable(x, y))
					continue;
				long key = CellKey.Pack(x, y);
				if (!region.Add(key))
					continue;
				q.Enqueue(new Vector2I(x, y));
			}
		}

		return region;
	}

	// pevnina jednoho hrace (utok po zemi)
	public static HashSet<long> FloodOwnedLand(
		TerrainGrid terrain, OwnershipGrid ownership, int sx, int sy, short ownerId)
	{
		var region = new HashSet<long>();
		if (!terrain.IsClaimable(sx, sy) || ownership.GetOwner(sx, sy) != ownerId)
			return region;

		var q = new Queue<Vector2I>();
		q.Enqueue(new Vector2I(sx, sy));
		region.Add(CellKey.Pack(sx, sy));

		while (q.Count > 0)
		{
			var p = q.Dequeue();
			foreach (var d in Dirs)
			{
				int x = p.X + d.X;
				int y = p.Y + d.Y;
				if (!terrain.IsClaimable(x, y) || ownership.GetOwner(x, y) != ownerId)
					continue;
				long key = CellKey.Pack(x, y);
				if (!region.Add(key))
					continue;
				q.Enqueue(new Vector2I(x, y));
			}
		}

		return region;
	}

	public static bool HasLandBorder(OwnershipGrid ownership, short attackerId, HashSet<long> landmass)
	{
		foreach (long key in landmass)
		{
			CellKey.Unpack(key, out int x, out int y);
			foreach (var d in Dirs)
			{
				if (ownership.GetOwner(x + d.X, y + d.Y) == attackerId)
					return true;
			}
		}

		return false;
	}

	public static List<Vector2I> FindLandFront(OwnershipGrid ownership, short attackerId, HashSet<long> region)
	{
		var front = new List<Vector2I>();
		foreach (long key in region)
		{
			CellKey.Unpack(key, out int x, out int y);
			foreach (var d in Dirs)
			{
				if (ownership.GetOwner(x + d.X, y + d.Y) == attackerId)
				{
					front.Add(new Vector2I(x, y));
					break;
				}
			}
		}

		return front;
	}

	// plaz + sousedni voda
	public static List<(Vector2I beach, int waterIdx)> CollectBeaches(
		TerrainGrid terrain, HashSet<long> landmass)
	{
		var list = new List<(Vector2I, int)>();
		int w = terrain.Width;

		foreach (long key in landmass)
		{
			CellKey.Unpack(key, out int x, out int y);
			foreach (var d in Dirs)
			{
				int wx = x + d.X;
				int wy = y + d.Y;
				if (!terrain.IsWater(wx, wy))
					continue;
				list.Add((new Vector2I(x, y), wy * w + wx));
				break;
			}
		}

		return list;
	}
}
