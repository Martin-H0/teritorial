using Godot;
using System;
using System.Collections.Generic;

namespace Teritorial.Game;

/// <summary>
/// Obrazovka nastavení – zobrazuje a ukládá všechny hodnoty GameConfig.
/// Uloží se do user://game_balance_user.tres; GameConfig.LoadOrDefault() preferuje tuto cestu.
/// </summary>
public partial class SettingsMenu : Control
{
    public const string UserConfigPath = "user://game_balance_user.tres";

    // Callback volaný po zavření (Back / Uložit)
    public Action? OnClose;

    private GameConfig _working = null!;
    private GameConfig _defaults = null!;

    // registry spinboxů: jméno property -> SpinBox
    private readonly Dictionary<string, SpinBox> _spinBoxes = new();

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        // --- tmavé pozadí ---
        var bg = new ColorRect
        {
            Color = new Color(0.06f, 0.07f, 0.11f, 0.97f),
            AnchorsPreset = (int)LayoutPreset.FullRect,
        };
        AddChild(bg);

        // --- vnější margin ---
        var margin = new MarginContainer { AnchorsPreset = (int)LayoutPreset.FullRect };
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        margin.AddThemeConstantOverride("margin_left", 40);
        margin.AddThemeConstantOverride("margin_right", 40);
        AddChild(margin);

        var outerBox = new VBoxContainer();
        margin.AddChild(outerBox);

        // --- nadpis ---
        var title = new Label
        {
            Text = "⚙  Nastavení hry",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 26);
        outerBox.AddChild(title);
        outerBox.AddChild(new HSeparator());

        // --- scrollovatelná oblast ---
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 200),
        };
        outerBox.AddChild(scroll);

        var scrollBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(scrollBox);

        // načtu výchozí hodnoty z kódu (bez souboru) a aktuální konfiguraci
        _defaults = new GameConfig();
        _working = GameConfig.LoadOrDefault(UserConfigPath);
        // pokud user config neexistuje, LoadOrDefault vrátí res:// config nebo default
        // chceme kopii aby jsme mohli editovat bez ovlivnění běžící hry
        _working = CloneConfig(_working);

        // --- skupiny polí ---
        AddSection(scrollBox, "👥 Hráči a boti");
        AddIntField(scrollBox, "BotCount", "Počet botů", 0, 15, _working.BotCount,
            v => _working.BotCount = (int)v);

        AddSection(scrollBox, "🗺  Spawn");
        AddIntField(scrollBox, "SpawnBlobRadius", "Poloměr spawnu (px)", 4, 32, _working.SpawnBlobRadius,
            v => _working.SpawnBlobRadius = (int)v);
        AddIntField(scrollBox, "SpawnClearanceRadius", "Bezpečná vzdálenost spawnů (px)", 8, 64, _working.SpawnClearanceRadius,
            v => _working.SpawnClearanceRadius = (int)v);
        AddIntField(scrollBox, "MinIslandArea", "Minimální plocha ostrova (px²)", 100, 5000, _working.MinIslandArea,
            v => _working.MinIslandArea = (int)v, step: 50);
        AddULongField(scrollBox, "SpawnSeed", "Seed (0 = náhodný)", 0, 999999999, (long)_working.SpawnSeed,
            v => _working.SpawnSeed = (ulong)v);

        AddSection(scrollBox, "👤 Populace");
        AddFloatField(scrollBox, "StartingPopulation", "Počáteční populace", 100, 2000, _working.StartingPopulation,
            v => _working.StartingPopulation = v, step: 50);
        AddFloatField(scrollBox, "TickInterval", "Interval tiku (s)", 0.05, 2, _working.TickInterval,
            v => _working.TickInterval = v, step: 0.05, decimals: 2);
        AddFloatField(scrollBox, "MaxPopPerPixel", "Max. pop. na pixel", 0.5, 10, _working.MaxPopPerPixel,
            v => _working.MaxPopPerPixel = v, step: 0.1, decimals: 1);
        AddFloatField(scrollBox, "GrowthRate", "Rychlost růstu", 0.005, 0.2, _working.GrowthRate,
            v => _working.GrowthRate = v, step: 0.001, decimals: 3);
        AddFloatField(scrollBox, "SlowdownPower", "Exponent zpomalení", 1, 6, _working.SlowdownPower,
            v => _working.SlowdownPower = v, step: 0.1, decimals: 1);
        AddFloatField(scrollBox, "SoftCap", "Měkký strop hustoty", 0.9, 1, _working.SoftCap,
            v => _working.SoftCap = v, step: 0.001, decimals: 3);
        AddFloatField(scrollBox, "MinDensityFactor", "Min. faktor hustoty", 0.05, 1, _working.MinDensityFactor,
            v => _working.MinDensityFactor = v, step: 0.05, decimals: 2);

        AddSection(scrollBox, "⚔  Expedice");
        AddFloatField(scrollBox, "ClaimCostNeutral", "Cena obsazení neutralu", 1, 50, _working.ClaimCostNeutral,
            v => _working.ClaimCostNeutral = v, step: 0.5, decimals: 1);
        AddFloatField(scrollBox, "ClaimCostEnemy", "Cena obsazení nepřítele", 2, 100, _working.ClaimCostEnemy,
            v => _working.ClaimCostEnemy = v, step: 0.5, decimals: 1);
        AddFloatField(scrollBox, "WaterLossPerTile", "Ztráta přes vodu (na tile)", 0, 0.2, _working.WaterLossPerTile,
            v => _working.WaterLossPerTile = v, step: 0.005, decimals: 3);
        AddFloatField(scrollBox, "EnemyDefenseFactor", "Faktor obrany nepřítele", 0, 2, _working.EnemyDefenseFactor,
            v => _working.EnemyDefenseFactor = v, step: 0.05, decimals: 2);
        AddIntField(scrollBox, "ClaimPixelsPerTick", "Pixelů obsazeno za tik", 1, 100, _working.ClaimPixelsPerTick,
            v => _working.ClaimPixelsPerTick = (int)v);
        AddFloatField(scrollBox, "MinExpeditionStrength", "Min. síla expedice", 1, 200, _working.MinExpeditionStrength,
            v => _working.MinExpeditionStrength = v, step: 1, decimals: 0);
        AddFloatField(scrollBox, "SettlerPopRatio", "Podíl kolonistů", 0.1, 1, _working.SettlerPopRatio,
            v => _working.SettlerPopRatio = v, step: 0.05, decimals: 2);
        AddIntField(scrollBox, "MaxPlayerExpeditions", "Max. expedic hráče", 1, 20, _working.MaxPlayerExpeditions,
            v => _working.MaxPlayerExpeditions = (int)v);
        AddFloatField(scrollBox, "NavalSpeed", "Námořní rychlost", 1, 50, _working.NavalSpeed,
            v => _working.NavalSpeed = v, step: 1, decimals: 0);
        AddFloatField(scrollBox, "MaxEnemySpeedMultiplier", "Max. násobič rychlosti nepřítele", 1, 6, _working.MaxEnemySpeedMultiplier,
            v => _working.MaxEnemySpeedMultiplier = v, step: 0.1, decimals: 1);

        AddSection(scrollBox, "🤖 Boti – AI");
        AddFloatField(scrollBox, "BotThinkInterval", "Interval myšlení bota (s)", 1, 30, _working.BotThinkInterval,
            v => _working.BotThinkInterval = v, step: 0.5, decimals: 1);
        AddFloatField(scrollBox, "BotThinkJitter", "Rozptyl intervalu bota (s)", 0, 10, _working.BotThinkJitter,
            v => _working.BotThinkJitter = v, step: 0.5, decimals: 1);
        AddFloatField(scrollBox, "BotAttackPopMin", "Min. pop. podíl pro útok", 0.05, 0.9, _working.BotAttackPopMin,
            v => _working.BotAttackPopMin = v, step: 0.01, decimals: 2);
        AddFloatField(scrollBox, "BotAttackPopMax", "Max. pop. podíl pro útok", 0.1, 1, _working.BotAttackPopMax,
            v => _working.BotAttackPopMax = v, step: 0.01, decimals: 2);
        AddFloatField(scrollBox, "BotAttackChance", "Šance útoku bota", 0, 1, _working.BotAttackChance,
            v => _working.BotAttackChance = v, step: 0.05, decimals: 2);
        AddFloatField(scrollBox, "BotSendPercentMin", "Min. % vyslaných vojsk", 5, 80, _working.BotSendPercentMin,
            v => _working.BotSendPercentMin = v, step: 1, decimals: 0);
        AddFloatField(scrollBox, "BotSendPercentMax", "Max. % vyslaných vojsk", 10, 100, _working.BotSendPercentMax,
            v => _working.BotSendPercentMax = v, step: 1, decimals: 0);
        AddIntField(scrollBox, "MaxBotExpeditions", "Max. expedic bota", 1, 10, _working.MaxBotExpeditions,
            v => _working.MaxBotExpeditions = (int)v);
        AddIntField(scrollBox, "BotTargetAttempts", "Počet pokusů o cíl", 5, 80, _working.BotTargetAttempts,
            v => _working.BotTargetAttempts = (int)v);

        AddSection(scrollBox, "🏆 Vítězství");
        AddFloatField(scrollBox, "WinLandPercent", "Podíl území pro výhru", 0.1, 1, _working.WinLandPercent,
            v => _working.WinLandPercent = v, step: 0.01, decimals: 2);

        // --- tlačítka ---
        outerBox.AddChild(new HSeparator());
        var btnRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        outerBox.AddChild(btnRow);

        var saveBtn = new Button { Text = "✔  Uložit a zavřít", CustomMinimumSize = new Vector2(160, 38) };
        var resetBtn = new Button { Text = "↺  Obnovit výchozí", CustomMinimumSize = new Vector2(160, 38) };
        var backBtn = new Button { Text = "✖  Zahodit změny", CustomMinimumSize = new Vector2(160, 38) };

        btnRow.AddChild(saveBtn);
        btnRow.AddChild(resetBtn);
        btnRow.AddChild(backBtn);

        saveBtn.Pressed += OnSave;
        resetBtn.Pressed += OnReset;
        backBtn.Pressed += OnBack;
    }

    // -------------------------------------------------------
    // Pomocné metody pro přidání polí
    // -------------------------------------------------------

    private static void AddSection(VBoxContainer box, string title)
    {
        var lbl = new Label { Text = title };
        lbl.AddThemeFontSizeOverride("font_size", 16);
        lbl.AddThemeColorOverride("font_color", new Color(0.6f, 0.85f, 1f));
        box.AddChild(lbl);
    }

    private void AddFloatField(VBoxContainer box, string key, string label,
        double min, double max, double currentValue, Action<float> setter,
        double step = 0.01, int decimals = 2)
    {
        var row = MakeRow(label);
        box.AddChild(row);

        var spin = new SpinBox
        {
            MinValue = min,
            MaxValue = max,
            Step = step,
            Value = currentValue,
            CustomMinimumSize = new Vector2(120, 0),
            Rounded = false,
            SelectAllOnFocus = true,
        };
        spin.GetLineEdit().MaxLength = 12;

        // prefix/suffix nechávám prázdné – čísla jsou srozumitelná
        spin.ValueChanged += v =>
        {
            double clamped = Math.Clamp(v, min, max);
            setter((float)clamped);
        };

        row.AddChild(spin);
        _spinBoxes[key] = spin;
    }

    private void AddIntField(VBoxContainer box, string key, string label,
        int min, int max, int currentValue, Action<double> setter, int step = 1)
    {
        var row = MakeRow(label);
        box.AddChild(row);

        var spin = new SpinBox
        {
            MinValue = min,
            MaxValue = max,
            Step = step,
            Value = currentValue,
            Rounded = true,
            CustomMinimumSize = new Vector2(120, 0),
            SelectAllOnFocus = true,
        };
        spin.ValueChanged += v =>
        {
            double clamped = Math.Clamp(Math.Round(v), min, max);
            setter(clamped);
        };

        row.AddChild(spin);
        _spinBoxes[key] = spin;
    }

    private void AddULongField(VBoxContainer box, string key, string label,
        long min, long max, long currentValue, Action<double> setter)
    {
        var row = MakeRow(label);
        box.AddChild(row);

        var spin = new SpinBox
        {
            MinValue = min,
            MaxValue = max,
            Step = 1,
            Value = currentValue,
            Rounded = true,
            CustomMinimumSize = new Vector2(140, 0),
            SelectAllOnFocus = true,
        };
        spin.ValueChanged += v =>
        {
            double clamped = Math.Clamp(Math.Round(v), min, max);
            setter(clamped);
        };

        row.AddChild(spin);
        _spinBoxes[key] = spin;
    }

    private static HBoxContainer MakeRow(string labelText)
    {
        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, 32),
        };
        var lbl = new Label
        {
            Text = labelText,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.AddChild(lbl);
        return row;
    }

    // -------------------------------------------------------
    // Tlačítka
    // -------------------------------------------------------

    private void OnSave()
    {
        // Uložíme do user://
        var err = ResourceSaver.Save(_working, UserConfigPath);
        if (err != Error.Ok)
            GD.PushWarning($"Nepodařilo se uložit nastavení: {err}");
        else
            GD.Print($"Nastavení uloženo do {UserConfigPath}");

        OnClose?.Invoke();
    }

    private void OnReset()
    {
        // Nastaví SpinBoxy na výchozí hodnoty z kódu
        _defaults = new GameConfig();
        SetSpinBox("BotCount", _defaults.BotCount);
        SetSpinBox("SpawnBlobRadius", _defaults.SpawnBlobRadius);
        SetSpinBox("SpawnClearanceRadius", _defaults.SpawnClearanceRadius);
        SetSpinBox("MinIslandArea", _defaults.MinIslandArea);
        SetSpinBox("SpawnSeed", (long)_defaults.SpawnSeed);
        SetSpinBox("StartingPopulation", _defaults.StartingPopulation);
        SetSpinBox("TickInterval", _defaults.TickInterval);
        SetSpinBox("MaxPopPerPixel", _defaults.MaxPopPerPixel);
        SetSpinBox("GrowthRate", _defaults.GrowthRate);
        SetSpinBox("SlowdownPower", _defaults.SlowdownPower);
        SetSpinBox("SoftCap", _defaults.SoftCap);
        SetSpinBox("MinDensityFactor", _defaults.MinDensityFactor);
        SetSpinBox("ClaimCostNeutral", _defaults.ClaimCostNeutral);
        SetSpinBox("ClaimCostEnemy", _defaults.ClaimCostEnemy);
        SetSpinBox("WaterLossPerTile", _defaults.WaterLossPerTile);
        SetSpinBox("EnemyDefenseFactor", _defaults.EnemyDefenseFactor);
        SetSpinBox("ClaimPixelsPerTick", _defaults.ClaimPixelsPerTick);
        SetSpinBox("MinExpeditionStrength", _defaults.MinExpeditionStrength);
        SetSpinBox("SettlerPopRatio", _defaults.SettlerPopRatio);
        SetSpinBox("MaxPlayerExpeditions", _defaults.MaxPlayerExpeditions);
        SetSpinBox("NavalSpeed", _defaults.NavalSpeed);
        SetSpinBox("MaxEnemySpeedMultiplier", _defaults.MaxEnemySpeedMultiplier);
        SetSpinBox("BotThinkInterval", _defaults.BotThinkInterval);
        SetSpinBox("BotThinkJitter", _defaults.BotThinkJitter);
        SetSpinBox("BotAttackPopMin", _defaults.BotAttackPopMin);
        SetSpinBox("BotAttackPopMax", _defaults.BotAttackPopMax);
        SetSpinBox("BotAttackChance", _defaults.BotAttackChance);
        SetSpinBox("BotSendPercentMin", _defaults.BotSendPercentMin);
        SetSpinBox("BotSendPercentMax", _defaults.BotSendPercentMax);
        SetSpinBox("MaxBotExpeditions", _defaults.MaxBotExpeditions);
        SetSpinBox("BotTargetAttempts", _defaults.BotTargetAttempts);
        SetSpinBox("WinLandPercent", _defaults.WinLandPercent);

        // Smaže uloženou konfiguraci
        if (FileAccess.FileExists(UserConfigPath))
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(UserConfigPath));

        GD.Print("Nastavení obnoveno na výchozí hodnoty.");
    }

    private void OnBack() => OnClose?.Invoke();

    private void SetSpinBox(string key, double value)
    {
        if (_spinBoxes.TryGetValue(key, out var sb))
            sb.Value = value;
    }

    // -------------------------------------------------------
    // Klonování configu aby jsme nemodifikovali načtený resource
    // -------------------------------------------------------

    private static GameConfig CloneConfig(GameConfig src)
    {
        var c = new GameConfig
        {
            MapDataPath = src.MapDataPath,
            BotCount = src.BotCount,
            SpawnBlobRadius = src.SpawnBlobRadius,
            SpawnClearanceRadius = src.SpawnClearanceRadius,
            MinIslandArea = src.MinIslandArea,
            SpawnSeed = src.SpawnSeed,
            StartingPopulation = src.StartingPopulation,
            TickInterval = src.TickInterval,
            MaxPopPerPixel = src.MaxPopPerPixel,
            GrowthRate = src.GrowthRate,
            SlowdownPower = src.SlowdownPower,
            SoftCap = src.SoftCap,
            MinDensityFactor = src.MinDensityFactor,
            ClaimCostNeutral = src.ClaimCostNeutral,
            ClaimCostEnemy = src.ClaimCostEnemy,
            WaterLossPerTile = src.WaterLossPerTile,
            EnemyDefenseFactor = src.EnemyDefenseFactor,
            ClaimPixelsPerTick = src.ClaimPixelsPerTick,
            MinExpeditionStrength = src.MinExpeditionStrength,
            SettlerPopRatio = src.SettlerPopRatio,
            MaxPlayerExpeditions = src.MaxPlayerExpeditions,
            NavalSpeed = src.NavalSpeed,
            MaxEnemySpeedMultiplier = src.MaxEnemySpeedMultiplier,
            BotThinkInterval = src.BotThinkInterval,
            BotThinkJitter = src.BotThinkJitter,
            BotAttackPopMin = src.BotAttackPopMin,
            BotAttackPopMax = src.BotAttackPopMax,
            BotAttackChance = src.BotAttackChance,
            BotSendPercentMin = src.BotSendPercentMin,
            BotSendPercentMax = src.BotSendPercentMax,
            MaxBotExpeditions = src.MaxBotExpeditions,
            BotTargetAttempts = src.BotTargetAttempts,
            WinLandPercent = src.WinLandPercent,
        };
        return c;
    }
}
