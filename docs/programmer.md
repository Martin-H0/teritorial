# Programátorská dokumentace - Teritorial

## Technologie

- **Godot 4.7**, jazyk **C#** (.NET)
- Projekt: složka `teritorial/`, solution `Teritorial.sln`
- Hlavní scéna: `scenes/MainMenu.tscn` (v `project.godot` -> `run/main_scene`)

## Přehled modulů

### Scény (`teritorial/scenes/`)

| Scéna | Účel |
|-------|------|
| `MainMenu.tscn` | výběr mapy, start hry |
| `Game.tscn` | samotná hra (GameController, kamera, config) |
| `maps/map_01.tscn` ... | dev editor mapy (MapPainter), F6 |

### Mapa (`Script/Map/`)

| Soubor | Účel |
|--------|------|
| `TerrainType.cs` | enum pevnina / voda / hory |
| `MapData.cs` | resource uložené mapy (.tres) |
| `TerrainGrid.cs` | načtený terén za běhu |
| `MapBaker.cs` | byte[] terénu -> MapData + textura |
| `MapPainter.cs` | kreslení mapy ve editoru |
| `MapManager.cs` | starší wrapper, hra používá GameController |

### Hra (`Script/Game/`)

| Soubor | Účel |
|--------|------|
| `GameController.cs` | hlavní smyčka, input, spawn, tick |
| `GameConfig.cs` + `config/game_balance.tres` | všechny balance konstanty |
| `GameSession.cs` | předání vybrané mapy z menu |
| `MainMenu.cs` | UI menu, scan `maps/data/*.tres` |
| `OwnershipGrid.cs` | vlastník každého pixelu |
| `PopulationGrid.cs` / `PopulationSimulation.cs` | populace a růst |
| `TerritoryRenderer.cs` | barevný overlay území |
| `SpawnPlacement.cs` | rozmístění hráče a botů |
| `ExpeditionSystem.cs` | expedice po zemi i mořem |
| `LandmassHelper.cs` | flood fill ostrovů, hranice |
| `NavalRouteFinder.cs` | cesta po vodě (BFS) |
| `WaterBodyIndex.cs` | souvislé vodní plochy |
| `BotAI.cs` | jednoduchá AI botů |
| `VictoryChecker.cs` | výhra / prohra |
| `GameHud.cs` | UI ve hře |
| `CameraController.cs` | pohyb kamery |
| `PlayerColors.cs` | barvy hráčů |

## Tok spuštění

1. `MainMenu` načte `.tres` mapy ze `res://maps/data/`
2. Po **Hrát** uloží cestu do `GameSession.SelectedMapPath`
3. `GameController` načte mapu, spawn, spustí `ExpeditionSystem` a `BotAI`
4. Každý tick: růst populace -> expedice -> boti -> kontrola výhry

## Datový model

- **Terén** - statický v `MapData.Terrain` (byte na pixel)
- **Vlastnictví** - `OwnershipGrid`, `-1` = nikdo
- **Populace** - `PopulationGrid`, jen na vlastněných pixelech
- **Expedice** - seznam aktivních výprav v `ExpeditionSystem` (fronta BFS na cílovém regionu)

## Přidání nové mapy

1. Duplikovat scénu `map_01.tscn` nebo použít MapPainter
2. V Inspectoru nastavit **Save Path** na `res://maps/data/mapXX.tres`
3. F6 -> nakreslit -> **S** uložit
4. Nový `.tres` se objeví v menu automaticky

## Build a časté problémy

```bash
cd teritorial
dotnet build
```

- **InvalidCastException** při načítání MapData -> C# projekt není buildnutý v Godotu
- špatná mapa v editoru -> zkontrolovat **Save Path** na scéně mapy
- změny v `game_balance.tres` -> reload projektu nebo restart hry

## Config

`config/game_balance.tres` - počet botů, růst, náklady expedice, `WinLandPercent` (0.7 = 70 % mapy), chování botů atd.

Třída `GameConfig` je `[GlobalClass]` resource, hodnoty jdou měnit v Inspectoru bez překompilování logiky.