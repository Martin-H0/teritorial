using System;
using Godot;

namespace Teritorial.Game;

// 0 = hrac (pevna barva), 1..N = boti (generovane proceduralne)
public static class PlayerColors
{
	// Výrazná barva hráče – sytá citrónová žluť, nezaměnitelná s čímkoli
	public static readonly Color PlayerColor = Color.FromHsv(0.15f, 1f, 1f); // zlatá/žlutá

	// Cache generovaných bot barev (naplní se při prvním volání GenerateBotColors)
	private static Color[]? _botCache;
	private static int _lastBotCount = -1;

	/// <summary>
	/// Vrátí barvu pro daného vlastníka.
	/// ownerId == 0 → fixní barva hráče
	/// ownerId > 0  → procedurálně generovaná barva bota
	/// ownerId < 0  → průhledná (žádný vlastník)
	/// </summary>
	public static Color Get(short ownerId)
	{
		if (ownerId < 0)  return Colors.Transparent;
		if (ownerId == 0) return PlayerColor;

		// botId 1..N
		int botIndex = ownerId - 1;
		return GetBotColor(botIndex);
	}

	/// <summary>
	/// Vrátí barvu bota s daným indexem (0-based).
	/// Barvy jsou rovnoměrně rozmístěny po barevném kruhu,
	/// ale s minimální vzdáleností od barvy hráče.
	/// </summary>
	public static Color GetBotColor(int botIndex)
	{
		// Pokud se počet botů nezměnil, použij cache
		// (cache invalidujeme jen pokud volající resetuje _lastBotCount)
		if (_botCache == null || botIndex >= _botCache.Length)
			EnsureCache(botIndex + 1);

		return _botCache![botIndex];
	}

	/// <summary>
	/// Vygeneruje (nebo rozšíří) cache barev pro zadaný počet botů.
	/// </summary>
	public static void EnsureCache(int botCount)
	{
		if (_botCache != null && _lastBotCount >= botCount)
			return;

		_botCache = GenerateBotColors(botCount);
		_lastBotCount = botCount;
	}

	// -------------------------------------------------------------------------
	// Generátor
	// -------------------------------------------------------------------------

	private static Color[] GenerateBotColors(int count)
	{
		if (count <= 0) return Array.Empty<Color>();

		// Hue hráče (0.15 = zlatá)
		const float playerHue = 0.15f;
		// Minimální vzdálenost hue od hráče (na kruhu 0-1)
		const float minDistFromPlayer = 0.15f;
		// Minimální vzdálenost hue mezi boty
		const float minDistBetweenBots = 0.08f;

		var colors = new Color[count];

		// Zlatý řez pro rozptyl – zajistí dobré pokrytí i pro velký počet botů
		const double goldenRatio = 0.618033988749895;

		// Začínáme naproti hráčově barvě (offset 0.5 = opačná strana kruhu)
		double hue = playerHue + 0.5;

		int generated = 0;
		int maxAttempts = count * 50;

		for (int attempt = 0; attempt < maxAttempts && generated < count; attempt++)
		{
			hue = (hue + goldenRatio) % 1.0;
			float h = (float)hue;

			// Zkontroluj vzdálenost od hráče
			float distPlayer = HueDist(h, playerHue);
			if (distPlayer < minDistFromPlayer)
				continue;

			// Zkontroluj vzdálenost od již vygenerovaných bot barev
			bool tooClose = false;
			for (int i = 0; i < generated; i++)
			{
				colors[i].ToHsv(out float eh, out _, out _);
				if (HueDist(h, eh) < minDistBetweenBots)
				{
					tooClose = true;
					break;
				}
			}
			if (tooClose)
				continue;

			// Střídáme sytost a jas aby byly barvy odlišné i když mají podobný hue
			float saturation = (generated % 3 == 0) ? 1.0f : (generated % 3 == 1) ? 0.85f : 0.70f;
			float value      = (generated % 2 == 0) ? 0.95f : 0.80f;

			colors[generated] = Color.FromHsv(h, saturation, value);
			generated++;
		}

		// Pokud se nepodařilo vygenerovat dostatek barev (příliš mnoho botů),
		// zaplníme zbytek s uvolněnějšími omezeními
		if (generated < count)
		{
			hue = playerHue + 0.5;
			for (int i = generated; i < count; i++)
			{
				hue = (hue + goldenRatio) % 1.0;
				float h = (float)hue;
				float saturation = 0.75f + (i % 3) * 0.08f;
				float value      = 0.70f + (i % 2) * 0.20f;
				colors[i] = Color.FromHsv(h, saturation, value);
			}
		}

		return colors;
	}

	/// <summary>Vzdálenost dvou hue hodnot na barevném kruhu (0-0.5).</summary>
	private static float HueDist(float a, float b)
	{
		float d = Math.Abs(a - b);
		return d > 0.5f ? 1f - d : d;
	}
}
