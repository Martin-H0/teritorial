using System.Collections.Generic;
using Godot;
using Teritorial.Map;

namespace Teritorial.Game;

public readonly struct SpawnPoint
{
	public short OwnerId { get; init; }
	public int X { get; init; }
	public int Y { get; init; }
}

internal sealed class LandIsland
{
	public required List<Vector2I> Pixels { get; init; }
	public int Area => Pixels.Count;
}

// kam dat hrace na zacatku
public static class SpawnPlacement
{
	private const int MaxAttemptsPerSpawn = 120;

	public static List<SpawnPoint> Place(
		TerrainGrid terrain,
		int totalPlayers,
		int minIslandArea,
		int blobRadius,
		int clearanceRadius,
		ulong seed = 0)
	{
		var rng = new RandomNumberGenerator();
		rng.Seed = seed == 0 ? (ulong)Time.GetTicksMsec() : seed;

		var islands = FindIslands(terrain, minIslandArea);
		if (islands.Count == 0)
		{
			GD.PushError("SpawnPlacement: na mapě není dost velká pevnina");
			return [];
		}

		var allocation = AllocateSpawns(islands, totalPlayers);
		var placed = new List<SpawnPoint>();
		var usedGlobal = new List<Vector2I>();
		int targetPixels = OwnershipGrid.TargetPixelsForRadius(blobRadius);
		int maxExpand = blobRadius * 3;

		for (int i = 0; i < islands.Count; i++)
		{
			int count = allocation[i];
			if (count <= 0)
				continue;

			float minDist = Mathf.Sqrt(islands[i].Area / (float)count) * 0.55f;
			minDist = Mathf.Max(minDist, blobRadius * 3f);

			for (int s = 0; s < count; s++)
			{
				short ownerId = (short)placed.Count;
				if (!TryPickSpot(islands[i], minDist, clearanceRadius, targetPixels, maxExpand, terrain, rng, usedGlobal, out var spot))
				{
					GD.PushWarning($"SpawnPlacement: ostrov {i} — nepodařilo se umístit spawn {s + 1}/{count}");
					continue;
				}

				placed.Add(new SpawnPoint { OwnerId = ownerId, X = spot.X, Y = spot.Y });
				usedGlobal.Add(spot);
			}
		}

		while (placed.Count < totalPlayers)
		{
			short ownerId = (short)placed.Count;
			bool ok = false;

			foreach (var island in islands)
			{
				float minDist = Mathf.Sqrt(island.Area / 4f) * 0.55f;
				if (TryPickSpot(island, minDist, clearanceRadius, targetPixels, maxExpand, terrain, rng, usedGlobal, out var spot))
				{
					placed.Add(new SpawnPoint { OwnerId = ownerId, X = spot.X, Y = spot.Y });
					usedGlobal.Add(spot);
					ok = true;
					break;
				}
			}

			if (!ok)
			{
				GD.PushWarning($"SpawnPlacement: umístěno jen {placed.Count}/{totalPlayers} spawnů");
				break;
			}
		}

		return placed;
	}

	private static List<LandIsland> FindIslands(TerrainGrid terrain, int minArea)
	{
		var visited = new bool[terrain.Width * terrain.Height];
		var islands = new List<LandIsland>();

		for (int y = 0; y < terrain.Height; y++)
		for (int x = 0; x < terrain.Width; x++)
		{
			if (!terrain.IsClaimable(x, y) || visited[y * terrain.Width + x])
				continue;

			var pixels = new List<Vector2I>();
			var queue = new Queue<Vector2I>();
			queue.Enqueue(new Vector2I(x, y));
			visited[y * terrain.Width + x] = true;

			while (queue.Count > 0)
			{
				var p = queue.Dequeue();
				pixels.Add(p);

				TryEnqueue(p.X + 1, p.Y);
				TryEnqueue(p.X - 1, p.Y);
				TryEnqueue(p.X, p.Y + 1);
				TryEnqueue(p.X, p.Y - 1);
			}

			if (pixels.Count >= minArea)
				islands.Add(new LandIsland { Pixels = pixels });

			void TryEnqueue(int px, int py)
			{
				if (!terrain.InBounds(px, py) || !terrain.IsClaimable(px, py))
					return;
				int idx = py * terrain.Width + px;
				if (visited[idx])
					return;
				visited[idx] = true;
				queue.Enqueue(new Vector2I(px, py));
			}
		}

		islands.Sort((a, b) => b.Area.CompareTo(a.Area));
		return islands;
	}

	private static int[] AllocateSpawns(List<LandIsland> islands, int totalPlayers)
	{
		int totalLand = 0;
		foreach (var island in islands)
			totalLand += island.Area;

		var allocation = new int[islands.Count];
		var remainder = new (int index, float frac)[islands.Count];

		int assigned = 0;
		for (int i = 0; i < islands.Count; i++)
		{
			float quota = totalPlayers * (islands[i].Area / (float)totalLand);
			allocation[i] = Mathf.FloorToInt(quota);
			assigned += allocation[i];
			remainder[i] = (i, quota - allocation[i]);
		}

		System.Array.Sort(remainder, (a, b) => b.frac.CompareTo(a.frac));
		for (int r = 0; r < totalPlayers - assigned; r++)
			allocation[remainder[r % remainder.Length].index]++;

		return allocation;
	}

	private static bool TryPickSpot(
		LandIsland island,
		float minDist,
		int clearanceRadius,
		int targetPixels,
		int maxExpandRadius,
		TerrainGrid terrain,
		RandomNumberGenerator rng,
		List<Vector2I> usedGlobal,
		out Vector2I spot)
	{
		spot = default;
		float minDist2 = minDist * minDist;
		int neededClear = Mathf.RoundToInt(Mathf.Pi * clearanceRadius * clearanceRadius * 0.55f);

		for (int attempt = 0; attempt < MaxAttemptsPerSpawn; attempt++)
		{
			var candidate = island.Pixels[rng.RandiRange(0, island.Pixels.Count - 1)];

			if (!HasClearance(candidate.X, candidate.Y, clearanceRadius, neededClear, terrain))
				continue;

			if (OwnershipGrid.CountReachableLand(candidate.X, candidate.Y, maxExpandRadius, terrain) < targetPixels)
				continue;

			bool tooClose = false;
			foreach (var other in usedGlobal)
			{
				float dx = candidate.X - other.X;
				float dy = candidate.Y - other.Y;
				if (dx * dx + dy * dy < minDist2)
				{
					tooClose = true;
					break;
				}
			}

			if (tooClose)
				continue;

			spot = candidate;
			return true;
		}

		return false;
	}

	private static bool HasClearance(int cx, int cy, int radius, int neededLand, TerrainGrid terrain)
	{
		int r2 = radius * radius;
		int land = 0;

		for (int py = cy - radius; py <= cy + radius; py++)
		for (int px = cx - radius; px <= cx + radius; px++)
		{
			if ((px - cx) * (px - cx) + (py - cy) * (py - cy) > r2)
				continue;
			if (terrain.IsClaimable(px, py))
				land++;
		}

		return land >= neededLand;
	}
}
