using Godot;

namespace Teritorial.Game;

// 0 = hrac zbytek boti
public static class PlayerColors
{
	public static readonly Color[] Palette =
	[
		new(0.25f, 0.55f, 0.95f),
		new(0.92f, 0.28f, 0.28f),
		new(0.95f, 0.82f, 0.22f),
		new(0.72f, 0.35f, 0.92f),
		new(0.95f, 0.55f, 0.18f),
		new(0.92f, 0.45f, 0.72f),
		new(0.35f, 0.88f, 0.82f),
		new(0.62f, 0.92f, 0.35f),
	];

	public static Color Get(short ownerId)
	{
		if (ownerId < 0)
			return Colors.Transparent;
		return Palette[ownerId % Palette.Length];
	}
}
