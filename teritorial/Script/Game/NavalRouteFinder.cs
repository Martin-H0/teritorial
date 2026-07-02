using System.Collections.Generic;
using Godot;
using Teritorial.Map;

namespace Teritorial.Game;

internal readonly struct NavalRoute
{
	public List<Vector2I> WaterPath { get; init; }
	public Vector2I LandingBeach { get; init; }
	public HashSet<long> TargetLandmass { get; init; }
	public string Message { get; init; }
	public bool Ok => WaterPath != null && WaterPath.Count > 0;
}

// BFS pres vodu, driv to delalo bfs na kazdy pixel kontinentu a padalo to
internal static class NavalRouteFinder
{
	public static NavalRoute TryPlan(
		TerrainGrid terrain,
		OwnershipGrid ownership,
		WaterBodyIndex water,
		short attackerId,
		int clickX,
		int clickY)
	{
		var empty = new NavalRoute { WaterPath = [], TargetLandmass = new HashSet<long>(), Message = "" };

		var land = LandmassHelper.NearestLand(terrain, clickX, clickY);
		if (land == null)
			return empty with { Message = "" };

		int tx = land.Value.X;
		int ty = land.Value.Y;
		if (ownership.GetOwner(tx, ty) == attackerId)
			return empty with { Message = "" };

		var landmass = LandmassHelper.FloodLandmass(terrain, tx, ty);
		if (landmass.Count == 0)
			return empty with { Message = "" };

		if (LandmassHelper.HasLandBorder(ownership, attackerId, landmass))
			return empty with { Message = "" };

		var beaches = LandmassHelper.CollectBeaches(terrain, landmass);
		if (beaches.Count == 0)
			return empty with { Message = "Žádné pobřeží" };

		var coastalWater = CollectCoastalWater(terrain, ownership, attackerId);
		if (coastalWater.Count == 0)
			return empty with { Message = "Nemáš pobřeží" };

		if (!TryShortestRoute(terrain, water, coastalWater, beaches, out var path, out var landing))
			return empty with { Message = "Cesta přes vodu neexistuje" };

		short owner = ownership.GetOwner(tx, ty);
		string kind = owner == OwnershipGrid.Unowned ? "expanze" : "útok";
		return new NavalRoute
		{
			WaterPath = path,
			LandingBeach = landing,
			TargetLandmass = landmass,
			Message = $"Námořní {kind}: {path.Count} px vody, vylodění ({landing.X},{landing.Y})",
		};
	}

	private static HashSet<int> CollectCoastalWater(TerrainGrid terrain, OwnershipGrid ownership, short ownerId)
	{
		var set = new HashSet<int>();
		int w = terrain.Width;

		for (int y = 0; y < terrain.Height; y++)
		for (int x = 0; x < terrain.Width; x++)
		{
			if (ownership.GetOwner(x, y) != ownerId)
				continue;
			foreach (var d in LandmassHelper.Dirs)
			{
				int wx = x + d.X;
				int wy = y + d.Y;
				if (terrain.IsWater(wx, wy))
					set.Add(wy * w + wx);
			}
		}

		return set;
	}

	private static bool TryShortestRoute(
		TerrainGrid terrain,
		WaterBodyIndex water,
		HashSet<int> sources,
		List<(Vector2I beach, int waterIdx)> beaches,
		out List<Vector2I> path,
		out Vector2I landing)
	{
		path = [];
		landing = default;
		int w = terrain.Width;

		// cile podle vodni komponenty
		var goalsByComp = new Dictionary<int, List<(Vector2I beach, int waterIdx)>>();
		foreach (var b in beaches)
		{
			int comp = water.GetComponent(b.waterIdx % w, b.waterIdx / w);
			if (comp < 0)
				continue;
			if (!goalsByComp.TryGetValue(comp, out var list))
			{
				list = new List<(Vector2I, int)>();
				goalsByComp[comp] = list;
			}

			list.Add(b);
		}

		if (goalsByComp.Count == 0)
			return false;

		int bestGoal = -1;
		Vector2I bestLanding = default;
		int bestDist = int.MaxValue;
		Dictionary<int, int>? bestParent = null;

		foreach (var kv in goalsByComp)
		{
			int comp = kv.Key;
			var goals = new HashSet<int>();
			foreach (var g in kv.Value)
				goals.Add(g.waterIdx);

			var compSources = new List<int>();
			foreach (int s in sources)
			{
				if (water.GetComponent(s % w, s / w) == comp)
					compSources.Add(s);
			}

			if (compSources.Count == 0)
				continue;

			if (!MultiSourceBfs(terrain, water, w, comp, compSources, goals, out int goalIdx, out int dist, out var parent))
				continue;

			if (dist < bestDist)
			{
				bestDist = dist;
				bestGoal = goalIdx;
				bestParent = parent;
				foreach (var g in kv.Value)
				{
					if (g.waterIdx == goalIdx)
					{
						bestLanding = g.beach;
						break;
					}
				}
			}
		}

		if (bestGoal < 0 || bestParent == null)
			return false;

		path = Reconstruct(bestParent, w, bestGoal);
		landing = bestLanding;
		return path.Count > 0;
	}

	private static bool MultiSourceBfs(
		TerrainGrid terrain,
		WaterBodyIndex water,
		int w,
		int comp,
		List<int> sources,
		HashSet<int> goals,
		out int reachedGoal,
		out int dist,
		out Dictionary<int, int> parent)
	{
		parent = new Dictionary<int, int>();
		var distance = new Dictionary<int, int>();
		var q = new Queue<int>();

		foreach (int s in sources)
		{
			if (parent.ContainsKey(s))
				continue;
			parent[s] = s;
			distance[s] = 0;
			q.Enqueue(s);
		}

		reachedGoal = -1;
		dist = int.MaxValue;
		var parents = parent;

		while (q.Count > 0)
		{
			int idx = q.Dequeue();
			int d = distance[idx];

			if (goals.Contains(idx) && d < dist)
			{
				dist = d;
				reachedGoal = idx;
			}

			if (reachedGoal >= 0 && d > dist)
				break;

			int x = idx % w;
			int y = idx / w;
			EnqueueNeighbor(x + 1, y, idx, d);
			EnqueueNeighbor(x - 1, y, idx, d);
			EnqueueNeighbor(x, y + 1, idx, d);
			EnqueueNeighbor(x, y - 1, idx, d);
		}

		return reachedGoal >= 0;

		void EnqueueNeighbor(int x, int y, int fromIdx, int fromDist)
		{
			if (!terrain.IsWater(x, y) || water.GetComponent(x, y) != comp)
				return;
			int i = y * w + x;
			if (parents.ContainsKey(i))
				return;
			parents[i] = fromIdx;
			distance[i] = fromDist + 1;
			q.Enqueue(i);
		}
	}

	private static List<Vector2I> Reconstruct(Dictionary<int, int> parent, int w, int goalIdx)
	{
		var path = new List<Vector2I>();
		int idx = goalIdx;
		while (true)
		{
			path.Add(new Vector2I(idx % w, idx / w));
			if (parent[idx] == idx)
				break;
			idx = parent[idx];
		}

		path.Reverse();
		return path;
	}

	public static float ApplyWaterLoss(float strength, int waterTiles, float lossPerTile)
	{
		for (int i = 0; i < waterTiles; i++)
			strength *= 1f - lossPerTile;
		return strength;
	}
}
