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

    // Callback volaný po zavření
    public Action? OnClose;

    private GameConfig _working = null!;
    private readonly Dictionary<string, SpinBox> _spinBoxes = new();

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        // --- poloprůhledný tmavý overlay přes celou obrazovku ---
        var overlay = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.65f),
            AnchorsPreset = (int)LayoutPreset.FullRect,
        };
        AddChild(overlay);

        // --- centrovaný kontejner ---
        var center = new CenterContainer { AnchorsPreset = (int)LayoutPreset.FullRect };
        AddChild(center);

        // --- hlavní panel (pevná šířka, max výška) ---
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(560, 0),
        };
        center.AddChild(panel);

        // solidní tmavé pozadí panelu přes StyleBoxFlat
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.12f, 0.17f, 1f),
            CornerRadiusTopLeft    = 8,
            CornerRadiusTopRight   = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft   = 24,
            ContentMarginRight  = 24,
            ContentMarginTop    = 20,
            ContentMarginBottom = 20,
        };
        panel.AddThemeStyleboxOverride("panel", style);

        // --- layout uvnitř panelu ---
        var outerBox = new VBoxContainer();
        panel.AddChild(outerBox);

        // nadpis
        var title = new Label
        {
            Text = "Nastaveni hry",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 22);
        outerBox.AddChild(title);
        outerBox.AddChild(new HSeparator());

        // scrollovatelná oblast s formulářem
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 300),
        };
        // omez výšku na 70 % výšky viewportu – funguje přes size_flags, ne přes max_size
        outerBox.AddChild(scroll);

        var scrollBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(scrollBox);

        // načtu kopii aktuální konfigurace pro editaci
        _working = CloneConfig(GameConfig.LoadOrDefault(UserConfigPath));

        // --- sekce ---
        AddSection(scrollBox, "Hraci a boti");
        AddIntField(scrollBox, "BotCount",
            "Pocet botu  (fyzicky limit je pocet spawnů na mape)",
            0, 50, _working.BotCount,
            v => _working.BotCount = (int)v);

        AddSection(scrollBox, "Spawn");
        AddIntField(scrollBox, "SpawnBlobRadius", "Polomer spawnu (px)", 4, 64, _working.SpawnBlobRadius,
            v => _working.SpawnBlobRadius = (int)v);
        AddIntField(scrollBox, "SpawnClearanceRadius", "Bezpecna vzdalenost spawnů (px)", 8, 128, _working.SpawnClearanceRadius,
            v => _working.SpawnClearanceRadius = (int)v);
        AddIntField(scrollBox, "MinIslandArea", "Min. plocha ostrova (px²)", 100, 20000, _working.MinIslandArea,
            v => _working.MinIslandArea = (int)v, step: 50);
        AddULongField(scrollBox, "SpawnSeed", "Seed (0 = nahodny)", 0, 999999999, (long)_working.SpawnSeed,
            v => _working.SpawnSeed = (ulong)v);

        AddSection(scrollBox, "Populace");
        AddFloatField(scrollBox, "StartingPopulation", "Pocatecni populace", 1, 10000, _working.StartingPopulation,
            v => _working.StartingPopulation = v, step: 50, decimals: 0);
        AddFloatField(scrollBox, "TickInterval", "Interval tiku (s)", 0.05, 2, _working.TickInterval,
            v => _working.TickInterval = v, step: 0.05, decimals: 2);
        AddFloatField(scrollBox, "MaxPopPerPixel", "Max. populace na pixel", 0.5, 10, _working.MaxPopPerPixel,
            v => _working.MaxPopPerPixel = v, step: 0.1, decimals: 1);
        AddFloatField(scrollBox, "GrowthRate", "Rychlost rustu", 0.005, 0.2, _working.GrowthRate,
            v => _working.GrowthRate = v, step: 0.001, decimals: 3);
        AddFloatField(scrollBox, "SlowdownPower", "Exponent zpomaleni", 1, 6, _working.SlowdownPower,
            v => _working.SlowdownPower = v, step: 0.1, decimals: 1);
        AddFloatField(scrollBox, "SoftCap", "Mekky strop hustoty", 0.9, 1, _working.SoftCap,
            v => _working.SoftCap = v, step: 0.001, decimals: 3);
        AddFloatField(scrollBox, "MinDensityFactor", "Min. faktor hustoty", 0.05, 1, _working.MinDensityFactor,
            v => _working.MinDensityFactor = v, step: 0.05, decimals: 2);

        AddSection(scrollBox, "Expedice");
        AddFloatField(scrollBox, "ClaimCostNeutral", "Cena obsazeni neutralu", 1, 50, _working.ClaimCostNeutral,
            v => _working.ClaimCostNeutral = v, step: 0.5, decimals: 1);
        AddFloatField(scrollBox, "ClaimCostEnemy", "Cena obsazeni nepritele", 2, 100, _working.ClaimCostEnemy,
            v => _working.ClaimCostEnemy = v, step: 0.5, decimals: 1);
        AddFloatField(scrollBox, "WaterLossPerTile", "Ztrata pres vodu (na tile)", 0, 0.2, _working.WaterLossPerTile,
            v => _working.WaterLossPerTile = v, step: 0.005, decimals: 3);
        AddFloatField(scrollBox, "EnemyDefenseFactor", "Faktor obrany nepritele", 0, 2, _working.EnemyDefenseFactor,
            v => _working.EnemyDefenseFactor = v, step: 0.05, decimals: 2);
        AddIntField(scrollBox, "ClaimPixelsPerTick", "Pixelu obsazeno za tik", 1, 500, _working.ClaimPixelsPerTick,
            v => _working.ClaimPixelsPerTick = (int)v);
        AddFloatField(scrollBox, "MinExpeditionStrength", "Min. sila expedice", 1, 200, _working.MinExpeditionStrength,
            v => _working.MinExpeditionStrength = v, step: 1, decimals: 0);
        AddFloatField(scrollBox, "SettlerPopRatio", "Podil kolonistu", 0.1, 1, _working.SettlerPopRatio,
            v => _working.SettlerPopRatio = v, step: 0.05, decimals: 2);
        AddIntField(scrollBox, "MaxPlayerExpeditions", "Max. expedic hrace", 1, 50, _working.MaxPlayerExpeditions,
            v => _working.MaxPlayerExpeditions = (int)v);
        AddFloatField(scrollBox, "NavalSpeed", "Namorni rychlost", 1, 50, _working.NavalSpeed,
            v => _working.NavalSpeed = v, step: 1, decimals: 0);
        AddFloatField(scrollBox, "MaxEnemySpeedMultiplier", "Max. nasobik rychlosti nepritele", 1, 6, _working.MaxEnemySpeedMultiplier,
            v => _working.MaxEnemySpeedMultiplier = v, step: 0.1, decimals: 1);

        AddSection(scrollBox, "Boti - AI");
        AddFloatField(scrollBox, "BotThinkInterval", "Interval mysleni bota (s)", 1, 30, _working.BotThinkInterval,
            v => _working.BotThinkInterval = v, step: 0.5, decimals: 1);
        AddFloatField(scrollBox, "BotThinkJitter", "Rozptyl intervalu bota (s)", 0, 10, _working.BotThinkJitter,
            v => _working.BotThinkJitter = v, step: 0.5, decimals: 1);
        AddFloatField(scrollBox, "BotAttackPopMin", "Min. pop. podil pro utok", 0.05, 0.9, _working.BotAttackPopMin,
            v => _working.BotAttackPopMin = v, step: 0.01, decimals: 2);
        AddFloatField(scrollBox, "BotAttackPopMax", "Max. pop. podil pro utok", 0.1, 1, _working.BotAttackPopMax,
            v => _working.BotAttackPopMax = v, step: 0.01, decimals: 2);
        AddFloatField(scrollBox, "BotAttackChance", "Sance utoku bota", 0, 1, _working.BotAttackChance,
            v => _working.BotAttackChance = v, step: 0.05, decimals: 2);
        AddFloatField(scrollBox, "BotSendPercentMin", "Min. % vyslanych vojsk", 5, 80, _working.BotSendPercentMin,
            v => _working.BotSendPercentMin = v, step: 1, decimals: 0);
        AddFloatField(scrollBox, "BotSendPercentMax", "Max. % vyslanych vojsk", 10, 100, _working.BotSendPercentMax,
            v => _working.BotSendPercentMax = v, step: 1, decimals: 0);
        AddIntField(scrollBox, "MaxBotExpeditions", "Max. expedic bota", 1, 20, _working.MaxBotExpeditions,
            v => _working.MaxBotExpeditions = (int)v);
        AddIntField(scrollBox, "BotTargetAttempts", "Pocet pokusu o cil", 1, 200, _working.BotTargetAttempts,
            v => _working.BotTargetAttempts = (int)v);

        AddSection(scrollBox, "Vitezstvi");
        AddFloatField(scrollBox, "WinLandPercent", "Podil uzemi pro vyhru", 0.1, 1, _working.WinLandPercent,
            v => _working.WinLandPercent = v, step: 0.01, decimals: 2);

        // --- tlačítka ---
        outerBox.AddChild(new HSeparator());

        var btnRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        outerBox.AddChild(btnRow);

        var saveBtn  = new Button { Text = "Ulozit a zavrit",  CustomMinimumSize = new Vector2(150, 36) };
        var resetBtn = new Button { Text = "Obnovit vychozi",  CustomMinimumSize = new Vector2(150, 36) };
        var backBtn  = new Button { Text = "Zahodit zmeny",    CustomMinimumSize = new Vector2(150, 36) };

        btnRow.AddChild(saveBtn);
        btnRow.AddChild(resetBtn);
        btnRow.AddChild(backBtn);

        saveBtn.Pressed  += OnSave;
        resetBtn.Pressed += OnReset;
        backBtn.Pressed  += OnBack;
    }

    // -------------------------------------------------------
    // Pomocné metody pro přidání polí
    // -------------------------------------------------------

    private static void AddSection(VBoxContainer box, string sectionTitle)
    {
        var lbl = new Label { Text = sectionTitle };
        lbl.AddThemeFontSizeOverride("font_size", 15);
        lbl.AddThemeColorOverride("font_color", new Color(0.55f, 0.80f, 1f));
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
            CustomMinimumSize = new Vector2(130, 0),
            Rounded = false,
            SelectAllOnFocus = true,
        };
        spin.ValueChanged += v => setter((float)Math.Clamp(v, min, max));

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
            CustomMinimumSize = new Vector2(130, 0),
            SelectAllOnFocus = true,
        };
        spin.ValueChanged += v => setter(Math.Clamp(Math.Round(v), min, max));

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
            CustomMinimumSize = new Vector2(150, 0),
            SelectAllOnFocus = true,
        };
        spin.ValueChanged += v => setter(Math.Clamp(Math.Round(v), min, max));

        row.AddChild(spin);
        _spinBoxes[key] = spin;
    }

    private static HBoxContainer MakeRow(string labelText)
    {
        var row = new HBoxContainer { CustomMinimumSize = new Vector2(0, 30) };
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
        var err = ResourceSaver.Save(_working, UserConfigPath);
        if (err != Error.Ok)
            GD.PushWarning($"Nepodarilo se ulozit nastaveni: {err}");
        else
            GD.Print($"Nastaveni ulozeno do {UserConfigPath}");

        OnClose?.Invoke();
    }

    private void OnReset()
    {
        var d = new GameConfig();

        SetSpin("BotCount",               d.BotCount);
        SetSpin("SpawnBlobRadius",         d.SpawnBlobRadius);
        SetSpin("SpawnClearanceRadius",    d.SpawnClearanceRadius);
        SetSpin("MinIslandArea",           d.MinIslandArea);
        SetSpin("SpawnSeed",               (long)d.SpawnSeed);
        SetSpin("StartingPopulation",      d.StartingPopulation);
        SetSpin("TickInterval",            d.TickInterval);
        SetSpin("MaxPopPerPixel",          d.MaxPopPerPixel);
        SetSpin("GrowthRate",              d.GrowthRate);
        SetSpin("SlowdownPower",           d.SlowdownPower);
        SetSpin("SoftCap",                 d.SoftCap);
        SetSpin("MinDensityFactor",        d.MinDensityFactor);
        SetSpin("ClaimCostNeutral",        d.ClaimCostNeutral);
        SetSpin("ClaimCostEnemy",          d.ClaimCostEnemy);
        SetSpin("WaterLossPerTile",        d.WaterLossPerTile);
        SetSpin("EnemyDefenseFactor",      d.EnemyDefenseFactor);
        SetSpin("ClaimPixelsPerTick",      d.ClaimPixelsPerTick);
        SetSpin("MinExpeditionStrength",   d.MinExpeditionStrength);
        SetSpin("SettlerPopRatio",         d.SettlerPopRatio);
        SetSpin("MaxPlayerExpeditions",    d.MaxPlayerExpeditions);
        SetSpin("NavalSpeed",              d.NavalSpeed);
        SetSpin("MaxEnemySpeedMultiplier", d.MaxEnemySpeedMultiplier);
        SetSpin("BotThinkInterval",        d.BotThinkInterval);
        SetSpin("BotThinkJitter",          d.BotThinkJitter);
        SetSpin("BotAttackPopMin",         d.BotAttackPopMin);
        SetSpin("BotAttackPopMax",         d.BotAttackPopMax);
        SetSpin("BotAttackChance",         d.BotAttackChance);
        SetSpin("BotSendPercentMin",       d.BotSendPercentMin);
        SetSpin("BotSendPercentMax",       d.BotSendPercentMax);
        SetSpin("MaxBotExpeditions",       d.MaxBotExpeditions);
        SetSpin("BotTargetAttempts",       d.BotTargetAttempts);
        SetSpin("WinLandPercent",          d.WinLandPercent);

        // Smaže uložený user config
        string absPath = ProjectSettings.GlobalizePath(UserConfigPath);
        if (FileAccess.FileExists(UserConfigPath))
            DirAccess.RemoveAbsolute(absPath);

        GD.Print("Nastaveni obnoveno na vychozi hodnoty.");
    }

    private void OnBack() => OnClose?.Invoke();

    private void SetSpin(string key, double value)
    {
        if (_spinBoxes.TryGetValue(key, out var sb))
            sb.Value = value;
    }

    // -------------------------------------------------------
    // Hluboká kopie konfigurace
    // -------------------------------------------------------

    private static GameConfig CloneConfig(GameConfig src) => new()
    {
        MapDataPath              = src.MapDataPath,
        BotCount                 = src.BotCount,
        SpawnBlobRadius          = src.SpawnBlobRadius,
        SpawnClearanceRadius     = src.SpawnClearanceRadius,
        MinIslandArea            = src.MinIslandArea,
        SpawnSeed                = src.SpawnSeed,
        StartingPopulation       = src.StartingPopulation,
        TickInterval             = src.TickInterval,
        MaxPopPerPixel           = src.MaxPopPerPixel,
        GrowthRate               = src.GrowthRate,
        SlowdownPower            = src.SlowdownPower,
        SoftCap                  = src.SoftCap,
        MinDensityFactor         = src.MinDensityFactor,
        ClaimCostNeutral         = src.ClaimCostNeutral,
        ClaimCostEnemy           = src.ClaimCostEnemy,
        WaterLossPerTile         = src.WaterLossPerTile,
        EnemyDefenseFactor       = src.EnemyDefenseFactor,
        ClaimPixelsPerTick       = src.ClaimPixelsPerTick,
        MinExpeditionStrength    = src.MinExpeditionStrength,
        SettlerPopRatio          = src.SettlerPopRatio,
        MaxPlayerExpeditions     = src.MaxPlayerExpeditions,
        NavalSpeed               = src.NavalSpeed,
        MaxEnemySpeedMultiplier  = src.MaxEnemySpeedMultiplier,
        BotThinkInterval         = src.BotThinkInterval,
        BotThinkJitter           = src.BotThinkJitter,
        BotAttackPopMin          = src.BotAttackPopMin,
        BotAttackPopMax          = src.BotAttackPopMax,
        BotAttackChance          = src.BotAttackChance,
        BotSendPercentMin        = src.BotSendPercentMin,
        BotSendPercentMax        = src.BotSendPercentMax,
        MaxBotExpeditions        = src.MaxBotExpeditions,
        BotTargetAttempts        = src.BotTargetAttempts,
        WinLandPercent           = src.WinLandPercent,
    };
}
