using Godot;

namespace Teritorial.Game;

// rust populace, hodnoty z configu
public static class PopulationSimulation
{
	public const short PlayerId = 0;

	public static float GetMaxPopulation(int landPixels, GameConfig cfg) =>
		landPixels * cfg.MaxPopPerPixel;

	public static float ComputeGrowth(float totalPop, int landPixels, float delta, GameConfig cfg)
	{
		if (landPixels <= 0 || totalPop <= 0)
			return 0;

		float maxPop = GetMaxPopulation(landPixels, cfg);
		float fill = totalPop / maxPop;
		if (fill >= cfg.SoftCap)
			return 0;

		float headroom = 1f - fill;
		float slowdown = Mathf.Pow(headroom, cfg.SlowdownPower);
		float growth = totalPop * cfg.GrowthRate * slowdown * delta;

		float density = totalPop / landPixels;
		float minDensity = cfg.MaxPopPerPixel * cfg.MinDensityFactor;
		if (density < minDensity)
			growth *= density / minDensity;

		float roomLeft = maxPop * cfg.SoftCap - totalPop;
		return Mathf.Max(0, Mathf.Min(growth, roomLeft));
	}

	public static void Tick(PopulationGrid pop, OwnershipGrid ownership, short ownerId, float delta, GameConfig cfg)
	{
		float total = pop.GetTotal(ownerId, ownership);
		int land = pop.CountLand(ownerId, ownership);
		float growth = ComputeGrowth(total, land, delta, cfg);
		pop.AddGrowth(ownerId, ownership, growth);
	}
}
