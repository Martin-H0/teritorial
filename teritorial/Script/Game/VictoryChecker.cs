using Teritorial.Map;

namespace Teritorial.Game;

public enum VictoryReason
{
	Domination,
	Elimination,
}

public enum DefeatReason
{
	NoTerritory,
}

public readonly struct GameOutcome
{
	public bool IsWin { get; init; }
	public VictoryReason? WinReason { get; init; }
	public DefeatReason? LossReason { get; init; }
	public float PlayerLandPercent { get; init; }
	public string Message { get; init; }
}

public static class VictoryChecker
{
	public static GameOutcome? Evaluate(
		TerrainGrid terrain,
		OwnershipGrid ownership,
		short playerId,
		int playerCount,
		GameConfig cfg)
	{
		int totalLand = terrain.CountClaimableLand();
		if (totalLand <= 0)
			return null;

		int playerLand = ownership.CountOwnedBy(playerId);
		float pct = (float)playerLand / totalLand;

		if (playerLand <= 0)
		{
			return new GameOutcome
			{
				IsWin = false,
				LossReason = DefeatReason.NoTerritory,
				PlayerLandPercent = 0f,
				Message = "Prohrál jsi — přišel jsi o všechno území.",
			};
		}

		if (pct >= cfg.WinLandPercent)
		{
			return new GameOutcome
			{
				IsWin = true,
				WinReason = VictoryReason.Domination,
				PlayerLandPercent = pct,
				Message = $"Vyhrál jsi — ovládáš {pct * 100f:0}% mapy (cíl {cfg.WinLandPercent * 100f:0} %).",
			};
		}

		if (playerCount > 1 && AllEnemiesEliminated(ownership, playerId, playerCount))
		{
			return new GameOutcome
			{
				IsWin = true,
				WinReason = VictoryReason.Elimination,
				PlayerLandPercent = pct,
				Message = $"Vyhrál jsi — všechny protivníky jsi vyhnal z mapy ({pct * 100f:0} % území).",
			};
		}

		return null;
	}

	private static bool AllEnemiesEliminated(OwnershipGrid ownership, short playerId, int playerCount)
	{
		for (short id = 0; id < playerCount; id++)
		{
			if (id == playerId)
				continue;
			if (ownership.CountOwnedBy(id) > 0)
				return false;
		}

		return true;
	}
}
