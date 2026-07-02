using System.Collections.Generic;
using Godot;
using Teritorial.Map;

namespace Teritorial.Game;

internal enum ExpeditionPhase { NavalTravel, LandClaim }

internal sealed class Expedition
{
	public short OwnerId;
	public float Strength;
	public bool Active = true;
	public ExpeditionPhase Phase;
	public HashSet<long> TargetLandmass = new();
	public Vector2I LandingBeach;
	public bool LandingUsed;

	public List<Vector2I> WaterPath = [];
	public int WaterPathIndex;
	public float WaterTravelProgress;

	public readonly Queue<Vector2I> Frontier = new();
	public readonly HashSet<long> Visited = new();
}

internal sealed class PendingNaval
{
	public NavalRoute Route;
	public float SendPercent;
}

public sealed class ExpeditionSystem
{
	private readonly TerrainGrid _terrain;
	private readonly OwnershipGrid _ownership;
	private readonly PopulationGrid _population;
	private readonly WaterBodyIndex _water;
	private readonly GameConfig _cfg;
	private readonly List<Expedition> _active = new();
	private PendingNaval? _pendingNaval;

	public int ActivePlayerExpeditionCount => CountActive(PopulationSimulation.PlayerId);
	public bool HasPendingNavalAttack => _pendingNaval != null;
	public int MaxExpeditions => _cfg.MaxPlayerExpeditions;

	public ExpeditionSystem(TerrainGrid terrain, OwnershipGrid ownership, PopulationGrid population, GameConfig cfg)
	{
		_terrain = terrain;
		_ownership = ownership;
		_population = population;
		_cfg = cfg;
		_water = new WaterBodyIndex(terrain);
	}

	public int CountActive(short ownerId)
	{
		int n = 0;
		foreach (var e in _active)
		{
			if (e.OwnerId == ownerId)
				n++;
		}

		return n;
	}

	// hrac

	public string TryLandAttack(int clickX, int clickY, float sendPercent)
	{
		short player = PopulationSimulation.PlayerId;
		if (!TryLaunchLand(player, clickX, clickY, sendPercent, out string msg))
			return msg;
		return msg;
	}

	public string TryPrepareNavalAttack(int clickX, int clickY, float sendPercent)
	{
		if (CountActive(PopulationSimulation.PlayerId) >= _cfg.MaxPlayerExpeditions)
			return $"Max {_cfg.MaxPlayerExpeditions} útoků najednou";

		short player = PopulationSimulation.PlayerId;
		var route = NavalRouteFinder.TryPlan(_terrain, _ownership, _water, player, clickX, clickY);
		if (!route.Ok)
			return route.Message;

		float total = _population.GetTotal(player, _ownership);
		float amount = total * (sendPercent / 100f);
		if (amount < _cfg.MinExpeditionStrength)
			return "Moc málo lidí";

		_pendingNaval = new PendingNaval { Route = route, SendPercent = sendPercent };
		int lossPct = route.WaterPath.Count > 0
			? (int)((1f - NavalRouteFinder.ApplyWaterLoss(amount, route.WaterPath.Count, _cfg.WaterLossPerTile) / amount) * 100f)
			: 0;
		return $"{route.Message} (−{lossPct} %). Potvrď.";
	}

	public string ConfirmNavalAttack()
	{
		if (_pendingNaval == null)
			return "Nic k potvrzení";

		var p = _pendingNaval;
		_pendingNaval = null;

		short player = PopulationSimulation.PlayerId;
		if (!TryTakeArmy(player, p.SendPercent, out float amount, out string err))
			return err;

		LaunchNaval(player, p.Route, amount);
		return $"Námořní útok: {amount:0} lidí, {p.Route.WaterPath.Count} px vody ({CountActive(player)}/{MaxExpeditions})";
	}

	public void CancelNavalAttack() => _pendingNaval = null;

	// boti

	public bool TryBotLandAttack(short botId, int clickX, int clickY, float sendPercent) =>
		TryLaunchLand(botId, clickX, clickY, sendPercent, out _);

	public bool TryBotNavalAttack(short botId, int clickX, int clickY, float sendPercent)
	{
		if (CountActive(botId) >= _cfg.MaxBotExpeditions)
			return false;

		var route = NavalRouteFinder.TryPlan(_terrain, _ownership, _water, botId, clickX, clickY);
		if (!route.Ok)
			return false;

		if (!TryTakeArmy(botId, sendPercent, out float amount, out _))
			return false;

		LaunchNaval(botId, route, amount);
		return true;
	}

	// tick expedici

	public bool Tick(float delta, out bool territoryChanged)
	{
		territoryChanged = false;
		for (int i = _active.Count - 1; i >= 0; i--)
		{
			var exp = _active[i];
			if (exp.Phase == ExpeditionPhase.NavalTravel)
				StepNaval(exp, delta);
			else if (StepLand(exp) > 0)
				territoryChanged = true;

			if (!exp.Active)
				_active.RemoveAt(i);
		}

		return territoryChanged;
	}

	// spusteni utoku

	private bool TryLaunchLand(short ownerId, int clickX, int clickY, float sendPercent, out string msg)
	{
		msg = "";
		int max = ownerId == PopulationSimulation.PlayerId ? _cfg.MaxPlayerExpeditions : _cfg.MaxBotExpeditions;
		if (CountActive(ownerId) >= max)
		{
			msg = ownerId == PopulationSimulation.PlayerId ? $"Max {max} útoků najednou" : "";
			return false;
		}

		var land = LandmassHelper.NearestLand(_terrain, clickX, clickY);
		if (land == null)
		{
			msg = "Tady není pevnina";
			return false;
		}

		int tx = land.Value.X;
		int ty = land.Value.Y;
		short targetOwner = _ownership.GetOwner(tx, ty);
		if (targetOwner == ownerId)
		{
			msg = "To je tvoje území";
			return false;
		}

		var region = LandmassHelper.FloodOwnedLand(_terrain, _ownership, tx, ty, targetOwner);
		if (region.Count == 0)
		{
			msg = "Neplatný cíl";
			return false;
		}

		var front = LandmassHelper.FindLandFront(_ownership, ownerId, region);
		if (front.Count == 0)
		{
			msg = "Cíl nesousedí s tvým územím — zkus RMB přes moře";
			return false;
		}

		if (!TryTakeArmy(ownerId, sendPercent, out float amount, out string err))
		{
			msg = err;
			return false;
		}

		var exp = new Expedition
		{
			OwnerId = ownerId,
			Strength = amount,
			Phase = ExpeditionPhase.LandClaim,
			TargetLandmass = region,
		};
		foreach (var p in front)
			exp.Frontier.Enqueue(p);

		_active.Add(exp);
		if (ownerId == PopulationSimulation.PlayerId)
		{
			string kind = targetOwner == OwnershipGrid.Unowned ? "Expanze" : "Útok";
			msg = $"{kind}: {amount:0} lidí, fronta {front.Count} px ({CountActive(ownerId)}/{max})";
		}

		return true;
	}

	private void LaunchNaval(short ownerId, NavalRoute route, float amount)
	{
		_active.Add(new Expedition
		{
			OwnerId = ownerId,
			Strength = amount,
			Phase = ExpeditionPhase.NavalTravel,
			TargetLandmass = route.TargetLandmass,
			LandingBeach = route.LandingBeach,
			WaterPath = route.WaterPath,
		});
	}

	private bool TryTakeArmy(short owner, float sendPercent, out float amount, out string err)
	{
		amount = 0;
		err = "";
		float total = _population.GetTotal(owner, _ownership);
		if (total < 1f)
		{
			err = "Nemáš populaci";
			return false;
		}

		amount = total * (sendPercent / 100f);
		if (amount < _cfg.MinExpeditionStrength)
		{
			err = "Moc málo lidí";
			return false;
		}

		_population.RemoveProportional(owner, _ownership, amount);
		return true;
	}

	private void StepNaval(Expedition exp, float delta)
	{
		if (exp.WaterPath.Count == 0)
		{
			BeginLandfall(exp);
			return;
		}

		exp.WaterTravelProgress += _cfg.NavalSpeed * delta;
		while (exp.WaterTravelProgress >= 1f && exp.WaterPathIndex < exp.WaterPath.Count)
		{
			exp.WaterTravelProgress -= 1f;
			exp.Strength = NavalRouteFinder.ApplyWaterLoss(exp.Strength, 1, _cfg.WaterLossPerTile);
			exp.WaterPathIndex++;

			if (exp.Strength < _cfg.MinExpeditionStrength)
			{
				Finish(exp);
				return;
			}
		}

		if (exp.WaterPathIndex >= exp.WaterPath.Count)
			BeginLandfall(exp);
	}

	private void BeginLandfall(Expedition exp)
	{
		exp.Phase = ExpeditionPhase.LandClaim;
		exp.Frontier.Clear();
		exp.Visited.Clear();
		exp.LandingUsed = false;
		exp.TargetLandmass = LandmassHelper.FloodLandmass(_terrain, exp.LandingBeach.X, exp.LandingBeach.Y);
		exp.Frontier.Enqueue(exp.LandingBeach);
	}

	private int StepLand(Expedition exp)
	{
		int budget = CalcClaimBudget(exp);
		int claimed = 0;

		while (exp.Strength > 0 && exp.Frontier.Count > 0 && claimed < budget)
		{
			var p = exp.Frontier.Dequeue();
			long key = CellKey.Pack(p.X, p.Y);
			if (!exp.Visited.Add(key))
				continue;
			if (!exp.TargetLandmass.Contains(key))
				continue;
			if (!_terrain.IsClaimable(p.X, p.Y))
				continue;

			short cur = _ownership.GetOwner(p.X, p.Y);
			if (cur == exp.OwnerId)
				continue;

			bool isLanding = !exp.LandingUsed && p == exp.LandingBeach;
			if (!isLanding && !TouchesOwned(p.X, p.Y, exp.OwnerId))
				continue;

			if (isLanding)
				exp.LandingUsed = true;

			float cost = ClaimCost(p.X, p.Y, cur);
			if (exp.Strength < cost)
				continue;

			exp.Strength -= cost;
			_ownership.SetOwner(p.X, p.Y, exp.OwnerId);
			_population.Set(p.X, p.Y, cost * _cfg.SettlerPopRatio);
			claimed++;
			EnqueueNeighbors(exp, p);
		}

		if (exp.Strength > 0 && exp.Frontier.Count == 0)
			Finish(exp);

		return claimed;
	}

	private int CalcClaimBudget(Expedition exp)
	{
		float speed = _cfg.ClaimPixelsPerTick;
		float armyMult = Mathf.Clamp(Mathf.Sqrt(exp.Strength / _cfg.MinExpeditionStrength), 0.5f, 4f);
		speed *= armyMult;

		int land = _population.CountLand(exp.OwnerId, _ownership);
		float growth = PopulationSimulation.ComputeGrowth(
			_population.GetTotal(exp.OwnerId, _ownership), land, 1f, _cfg);
		float regenCap = growth / Mathf.Max(_cfg.ClaimCostNeutral, 0.1f);
		speed = Mathf.Min(speed, Mathf.Max(regenCap, _cfg.ClaimPixelsPerTick * 0.5f));

		float def = RegionDefense(exp);
		if (def > 0f)
		{
			float ratio = exp.Strength / def;
			if (ratio > 1f)
				speed *= Mathf.Clamp(Mathf.Sqrt(ratio), 1f, _cfg.MaxEnemySpeedMultiplier);
		}

		return Mathf.Max(1, Mathf.RoundToInt(speed));
	}

	private float RegionDefense(Expedition exp)
	{
		float sum = 0;
		int enemyPx = 0;
		foreach (long key in exp.TargetLandmass)
		{
			CellKey.Unpack(key, out int x, out int y);
			short o = _ownership.GetOwner(x, y);
			if (o == exp.OwnerId || o == OwnershipGrid.Unowned)
				continue;
			sum += _population.Get(x, y);
			enemyPx++;
		}

		if (enemyPx == 0)
			return 0;
		return sum + enemyPx * _cfg.ClaimCostEnemy * 0.25f;
	}

	private bool TouchesOwned(int x, int y, short ownerId)
	{
		foreach (var d in LandmassHelper.Dirs)
		{
			if (_ownership.GetOwner(x + d.X, y + d.Y) == ownerId)
				return true;
		}

		return false;
	}

	private void EnqueueNeighbors(Expedition exp, Vector2I p)
	{
		foreach (var d in LandmassHelper.Dirs)
		{
			int x = p.X + d.X;
			int y = p.Y + d.Y;
			long key = CellKey.Pack(x, y);
			if (!exp.TargetLandmass.Contains(key))
				continue;
			if (exp.Visited.Contains(key))
				continue;
			if (!_terrain.IsClaimable(x, y))
				continue;
			if (_ownership.GetOwner(x, y) == exp.OwnerId)
				continue;
			exp.Frontier.Enqueue(new Vector2I(x, y));
		}
	}

	private float ClaimCost(int x, int y, short defenderId)
	{
		if (defenderId == OwnershipGrid.Unowned)
			return _cfg.ClaimCostNeutral;
		return _cfg.ClaimCostEnemy + _population.Get(x, y) * _cfg.EnemyDefenseFactor;
	}

	private void Finish(Expedition exp)
	{
		if (exp.Strength > 0)
			_population.AddToOwner(exp.OwnerId, _ownership, exp.Strength);
		exp.Strength = 0;
		exp.Active = false;
	}
}
