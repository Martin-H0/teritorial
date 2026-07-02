using Godot;

namespace Teritorial.Game;

// nastaveni hry, menim v config/game_balance.tres
[GlobalClass]
public partial class GameConfig : Resource
{
	public const string DefaultPath = "res://config/game_balance.tres";

	// mapa

	[ExportGroup("Mapa")]
	[Export] public string MapDataPath { get; set; } = "res://maps/data/map01.tres";

	// spawny

	[ExportGroup("Spawny")]
	[Export(PropertyHint.Range, "0,8,1")]
	public int BotCount { get; set; } = 2;

	[Export(PropertyHint.Range, "4,32,1")]
	public int SpawnBlobRadius { get; set; } = 12;

	[Export(PropertyHint.Range, "8,64,1")]
	public int SpawnClearanceRadius { get; set; } = 28;

	[Export(PropertyHint.Range, "100,5000,50")]
	public int MinIslandArea { get; set; } = 800;

	// 0 = random seed
	[Export(PropertyHint.Range, "0,999999999,1")]
	public ulong SpawnSeed { get; set; }

	// populace

	[ExportGroup("Populace")]
	[Export(PropertyHint.Range, "1,2000,1")]
	public float StartingPopulation { get; set; } = 250f;

	[Export(PropertyHint.Range, "0.05,2,0.05")]
	public float TickInterval { get; set; } = 0.25f;

	[Export(PropertyHint.Range, "0.5,10,0.1")]
	public float MaxPopPerPixel { get; set; } = 2.5f;

	[Export(PropertyHint.Range, "0.005,0.2,0.001")]
	public float GrowthRate { get; set; } = 0.045f;

	[Export(PropertyHint.Range, "1,6,0.1")]
	public float SlowdownPower { get; set; } = 2.8f;

	[Export(PropertyHint.Range, "0.9,1,0.001")]
	public float SoftCap { get; set; } = 0.995f;

	[Export(PropertyHint.Range, "0.05,1,0.05")]
	public float MinDensityFactor { get; set; } = 0.25f;

	// expedice

	[ExportGroup("Expedice")]
	[Export(PropertyHint.Range, "1,50,0.5")]
	public float ClaimCostNeutral { get; set; } = 3f;

	[Export(PropertyHint.Range, "2,100,0.5")]
	public float ClaimCostEnemy { get; set; } = 10f;

	[Export(PropertyHint.Range, "0,0.2,0.005")]
	public float WaterLossPerTile { get; set; } = 0.025f;

	[Export(PropertyHint.Range, "0,2,0.05")]
	public float EnemyDefenseFactor { get; set; } = 0.4f;

	[Export(PropertyHint.Range, "1,100,1")]
	public int ClaimPixelsPerTick { get; set; } = 12;

	[Export(PropertyHint.Range, "1,200,1")]
	public float MinExpeditionStrength { get; set; } = 5f;

	[Export(PropertyHint.Range, "0.1,1,0.05")]
	public float SettlerPopRatio { get; set; } = 0.6f;

	[Export(PropertyHint.Range, "1,20,1")]
	public int MaxPlayerExpeditions { get; set; } = 10;

	[Export(PropertyHint.Range, "1,50,1")]
	public float NavalSpeed { get; set; } = 12f;

	[Export(PropertyHint.Range, "1,6,0.1")]
	public float MaxEnemySpeedMultiplier { get; set; } = 3f;

	// boti

	[ExportGroup("Boti")]
	[Export(PropertyHint.Range, "1,30,0.5")]
	public float BotThinkInterval { get; set; } = 3f;

	[Export(PropertyHint.Range, "0,10,0.5")]
	public float BotThinkJitter { get; set; } = 2f;

	[Export(PropertyHint.Range, "0.05,0.9,0.01")]
	public float BotAttackPopMin { get; set; } = 0.12f;

	[Export(PropertyHint.Range, "0.1,1,0.01")]
	public float BotAttackPopMax { get; set; } = 0.85f;

	[Export(PropertyHint.Range, "0,1,0.05")]
	public float BotAttackChance { get; set; } = 0.7f;

	[Export(PropertyHint.Range, "5,80,1")]
	public float BotSendPercentMin { get; set; } = 18f;

	[Export(PropertyHint.Range, "10,100,1")]
	public float BotSendPercentMax { get; set; } = 35f;

	[Export(PropertyHint.Range, "1,10,1")]
	public int MaxBotExpeditions { get; set; } = 2;

	[Export(PropertyHint.Range, "5,80,1")]
	public int BotTargetAttempts { get; set; } = 25;

	// vyhra

	[ExportGroup("Vítězství")]
	[Export(PropertyHint.Range, "0.1,1,0.01")]
	public float WinLandPercent { get; set; } = 0.70f;

	public static GameConfig LoadOrDefault(string path = null)
	{
		path ??= DefaultPath;
		if (ResourceLoader.Exists(path))
		{
			var cfg = ResourceLoader.Load<GameConfig>(path);
			if (cfg != null)
				return cfg;
		}

		GD.PushWarning($"GameConfig nenalezen ({path}), používám výchozí hodnoty z kódu");
		return new GameConfig();
	}
}
