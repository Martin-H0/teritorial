using System;
using System.Collections.Generic;
using Godot;
using Teritorial.Map;

namespace Teritorial.Game;

// blbi boti jen random utoky
public sealed class BotAI
{
	private readonly TerrainGrid _terrain;
	private readonly OwnershipGrid _ownership;
	private readonly PopulationGrid _population;
	private readonly ExpeditionSystem _expeditions;
	private readonly GameConfig _cfg;
	private readonly Random _rng;
	private readonly float[] _thinkTimers;
	private readonly int _botCount;

	private readonly List<Vector2I> _landTargets = new();
	private readonly List<Vector2I> _neighborTargets = new();

	public BotAI(
		TerrainGrid terrain,
		OwnershipGrid ownership,
		PopulationGrid population,
		ExpeditionSystem expeditions,
		GameConfig cfg,
		int botCount,
		ulong seed = 0)
	{
		_terrain = terrain;
		_ownership = ownership;
		_population = population;
		_expeditions = expeditions;
		_cfg = cfg;
		_botCount = botCount;
		_rng = seed == 0 ? new Random() : new Random((int)seed);
		_thinkTimers = new float[botCount + 1];
		for (short id = 1; id <= botCount; id++)
			_thinkTimers[id] = 0.5f + _rng.NextSingle() * 2f;
	}

	public bool Tick(float delta)
	{
		bool acted = false;
		for (short botId = 1; botId <= _botCount; botId++)
		{
			_thinkTimers[botId] -= delta;
			if (_thinkTimers[botId] > 0f)
				continue;

			_thinkTimers[botId] = _cfg.BotThinkInterval + _rng.NextSingle() * _cfg.BotThinkJitter;
			if (TryAct(botId))
				acted = true;
		}

		return acted;
	}

	private bool TryAct(short botId)
	{
		if (_expeditions.CountActive(botId) >= _cfg.MaxBotExpeditions)
			return false;

		if (!IsReadyToAttack(botId))
			return false;

		if (_rng.NextDouble() > _cfg.BotAttackChance)
			return false;

		float send = _cfg.BotSendPercentMin +
			_rng.NextSingle() * (_cfg.BotSendPercentMax - _cfg.BotSendPercentMin);

		int first = _rng.Next(3);
		for (int i = 0; i < 3; i++)
		{
			if (TryAction(botId, send, (first + i) % 3))
				return true;
		}

		return false;
	}

	// utoci jen kdyz ma lidi ale jeste neni full
	private bool IsReadyToAttack(short botId)
	{
		int land = _population.CountLand(botId, _ownership);
		if (land <= 0)
			return false;

		float pop = _population.GetTotal(botId, _ownership);
		float max = PopulationSimulation.GetMaxPopulation(land, _cfg);
		if (max < 1f)
			return false;

		float ratio = pop / max;
		return ratio >= _cfg.BotAttackPopMin && ratio <= _cfg.BotAttackPopMax;
	}

	private bool TryAction(short botId, float sendPercent, int action) =>
		action switch
		{
			0 => TryLandFromBorder(botId, sendPercent, neighborsOnly: false),
			1 => TryNaval(botId, sendPercent),
			_ => TryLandFromBorder(botId, sendPercent, neighborsOnly: true),
		};

	// bere cile z hrnice ne random pixel mapy (to driv nefungovalo)
	private bool TryLandFromBorder(short botId, float sendPercent, bool neighborsOnly)
	{
		var list = neighborsOnly ? _neighborTargets : _landTargets;
		CollectBorderTargets(botId, neighborsOnly, list);
		if (list.Count == 0)
			return false;

		int tries = Math.Min(list.Count, _cfg.BotTargetAttempts);
		for (int i = 0; i < tries; i++)
		{
			var p = list[_rng.Next(list.Count)];
			if (_expeditions.TryBotLandAttack(botId, p.X, p.Y, sendPercent))
				return true;
		}

		return false;
	}

	private void CollectBorderTargets(short botId, bool neighborsOnly, List<Vector2I> outList)
	{
		outList.Clear();
		int w = _terrain.Width;
		int h = _terrain.Height;

		for (int y = 0; y < h; y++)
		for (int x = 0; x < w; x++)
		{
			if (_ownership.GetOwner(x, y) != botId)
				continue;

			foreach (var d in LandmassHelper.Dirs)
			{
				int tx = x + d.X;
				int ty = y + d.Y;
				if (!_terrain.IsClaimable(tx, ty))
					continue;

				short targetOwner = _ownership.GetOwner(tx, ty);
				if (targetOwner == botId)
					continue;
				if (neighborsOnly && targetOwner == OwnershipGrid.Unowned)
					continue;

				outList.Add(new Vector2I(tx, ty));
			}
		}
	}

	private bool TryNaval(short botId, float sendPercent)
	{
		for (int attempt = 0; attempt < _cfg.BotTargetAttempts; attempt++)
		{
			int x = _rng.Next(_terrain.Width);
			int y = _rng.Next(_terrain.Height);
			if (!_terrain.IsClaimable(x, y))
				continue;
			if (_ownership.GetOwner(x, y) == botId)
				continue;

			var landmass = LandmassHelper.FloodLandmass(_terrain, x, y);
			if (landmass.Count == 0)
				continue;
			if (LandmassHelper.HasLandBorder(_ownership, botId, landmass))
				continue;

			var beaches = LandmassHelper.CollectBeaches(_terrain, landmass);
			if (beaches.Count == 0)
				continue;

			var beach = beaches[_rng.Next(beaches.Count)];
			if (_expeditions.TryBotNavalAttack(botId, beach.beach.X, beach.beach.Y, sendPercent))
				return true;
		}

		return false;
	}
}
