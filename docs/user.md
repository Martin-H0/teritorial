# Uživatelská dokumentace - Teritorial

## Spuštění

Postup je v [README](../README.md) v kořeni repozitáře. Stručně:

1. Otevři projekt `teritorial` v Godot 4 (.NET)
2. **Build** (kladivo)
3. **F5** -> hlavní menu

Není potřeba nic instalovat kromě Godotu a .NET SDK.

## Hlavní menu

- **Mapa** - výběr z map v `maps/data/` (map01, map02, ...)
- **Hrát** - začne hra s vybranou mapou
- **Ukončit** - zavře program

Po skončení hry (výhra/prohra): **Hrát znovu** nebo **Hlavní menu**.

## Ovládání ve hře

### Kamera

- **WASD** nebo **šipky** - posun
- **kolečko myši** - zoom

### Expedice

V levém panelu nastav **% populace** poslané na výpravu.

| Akce | Ovládání |
|------|----------|
| Expanze / útok po zemi | **levé** tlačítko myši na cílovou pevninu (musí sousednit s tvým územím) |
| Námořní útok | **pravé** tlačítko na ostrov přes moře -> potvrzení v HUD |

Námořní útok funguje jen když s cílem **nesousedíš po pevnině**, ale jde tam cesta po vodě z tvého pobřeží.

### HUD

Zobrazuje populaci, území, růst, počet aktivních útoků a **kolik % mapy držíš** (cíl pro výhru je v configu, default 70 %).

## Pravidla (stručně)

- Populace roste logisticky - blízko maxima roste pomalu
- Větší expedice obsazuje rychleji, ale ne rychleji než stíháš znovu „rodit“ lidi
- Útok na cizí území stojí víc než neutrální pevnina
- Přes vodu se ztrácí část armády podle délky cesty
- Najednou můžeš mít omezený počet útoků (viz config)

## Výhra a prohra

**Výhra** (stačí jedna podmínka):

1. Ovládneš alespoň nastavené % **veškeré pevniny** na mapě (výchozí 70 %)
2. Všichni boti přijdou o území (0 pixelů)

**Prohra:** nemáš žádné území.

## Nastavení obtížnosti

Hodnoty hry (počet botů, rychlost růstu, cíl výhry, ...) jsou v souboru  
`teritorial/config/game_balance.tres` - otevřeš v Godotu dvojklikem a upravíš v Inspectoru. Pro běžné hraní to měnit nemusíš.

## Editor map (jen vývoj)

Mapy kreslíš ve scénách `scenes/maps/map_01.tscn` atd., spuštění **F6**:

- **LMB** — pevnina / voda / hory (podle zvoleného režimu v UI scény)
- **S** — uložit do `maps/data/`
- **R** — nová mapa samé vody

Hráč v hotové hře editor nevidí.