# Teritorial

Zápočtový program - 2D strategická hra inspirovaná territorial.io. Single player proti botům, pixelová mapa, expanze území a populace.

## Specifikace

Hráč ovládá stát na mapě z pevniny a vody. Roste mu populace, posílá expedice na sousední neutrální území nebo na nepřátele. Útok přes moře jde jen pravým tlačítkem (námořní výprava). Proti hráči hrají jednoduchí boti s náhodnými útoky.

Výhra: ovládnout nastavené % pevniny na mapě (výchozí 70 %) nebo vyhnat všechny protivníky. Prohra: přijít o veškeré území.

Mapy se netvoří ve hře — jsou předpřipravené v `teritorial/maps/data/`. Editor map je jen pro vývojáře (Godot scény `map_01`, `map_02`, ...).

## Struktura repozitáře

```
student-hornycma/
  README.md           — tento soubor
  docs/               — dokumentace pro uživatele a programátory
  teritorial/         — Godot 4 projekt (C#)
    project.godot
    scenes/           — MainMenu, Game, editor map
    Script/           — herní logika
    maps/data/        — hotové mapy (.tres)
    config/           — vyvážení hry (game_balance.tres)
```

## Instalace a spuštění

### Co potřebuješ

1. **Godot 4.x** s podporou **.NET** (na webu „Godot Engine - .NET“)
2. **.NET SDK** 8 (nebo verze co Godot vyžaduje)

### Spuštění v Godotu (doporučené)

1. Otevři Godot -> **Import** -> složka `teritorial` (soubor `project.godot`)
2. Po prvním otevření: horní lišta **Build** (kladivo), počkej na úspěšný build C#
3. Stiskni **F5** - spustí se hlavní menu, vyber mapu, **Hrát**

| Klávesa | Co dělá |
|---------|---------|
| **F5** | Hra od začátku (MainMenu -> výběr mapy) |
| **F6** | Spustí právě otevřenou scénu v editoru |

Editor mapy (jen pro tvorbu map): otevři `scenes/maps/map_01.tscn` (nebo map_02, map_03) -> **F6**.

### Build z příkazové řádky

```bash
cd teritorial
dotnet build
```

Když Godot hlásí chyby typu `InvalidCastException` u MapData, nejdřív udělej build (v Godotu nebo `dotnet build`) a případně **Project -> Reload Current Project**.

## Dokumentace

- [Uživatelská dokumentace](docs/user.md) - ovládání, menu, pravidla
- [Programátorská dokumentace](docs/programmer.md) - struktura kódu, mapy, config

Hra nepotřebuje externí vstupní soubory od uživatele (mapy jsou v repozitáři).