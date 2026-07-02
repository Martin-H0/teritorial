using Godot;

namespace Teritorial.Game;

// populace na pixelu
public sealed class PopulationGrid
{
	public int Width { get; }
	public int Height { get; }

	private readonly float[] _pop;

	public PopulationGrid(int width, int height)
	{
		Width = width;
		Height = height;
		_pop = new float[width * height];
	}

	public float Get(int x, int y) => _pop[y * Width + x];

	public void Set(int x, int y, float value) => _pop[y * Width + x] = value;

	public float GetTotal(short ownerId, OwnershipGrid ownership)
	{
		float sum = 0;
		for (int i = 0; i < _pop.Length; i++)
			if (ownership.GetOwnerFlat(i) == ownerId)
				sum += _pop[i];
		return sum;
	}

	public int CountLand(short ownerId, OwnershipGrid ownership)
	{
		int n = 0;
		for (int i = 0; i < _pop.Length; i++)
			if (ownership.GetOwnerFlat(i) == ownerId)
				n++;
		return n;
	}

	// na zacatku rovnomerne
	public void SeedPlayer(short ownerId, OwnershipGrid ownership, float totalPop)
	{
		int land = CountLand(ownerId, ownership);
		if (land == 0)
			return;

		float perPixel = totalPop / land;
		for (int i = 0; i < _pop.Length; i++)
		{
			if (ownership.GetOwnerFlat(i) == ownerId)
				_pop[i] = perPixel;
		}
	}

	public void AddGrowth(short ownerId, OwnershipGrid ownership, float amount)
	{
		int land = CountLand(ownerId, ownership);
		if (land == 0 || amount <= 0)
			return;

		float perPixel = amount / land;
		for (int i = 0; i < _pop.Length; i++)
		{
			if (ownership.GetOwnerFlat(i) == ownerId)
				_pop[i] += perPixel;
		}
	}

	public void AddToOwner(short ownerId, OwnershipGrid ownership, float amount) =>
		AddGrowth(ownerId, ownership, amount);

	// expedice bere % populace
	public void RemoveProportional(short ownerId, OwnershipGrid ownership, float amount)
	{
		float total = GetTotal(ownerId, ownership);
		if (total <= 0 || amount <= 0)
			return;

		float ratio = Mathf.Clamp(amount / total, 0f, 1f);
		for (int i = 0; i < _pop.Length; i++)
		{
			if (ownership.GetOwnerFlat(i) == ownerId)
				_pop[i] *= 1f - ratio;
		}
	}
}
