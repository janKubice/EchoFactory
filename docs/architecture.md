# Architektura

Tento dokument popisuje strukturu kódu, vrstvy, datové modely a — nejdůležitější
— **pravidla determinismu**. Časový/simulační model má vlastní dokument:
[simulation-engine.md](./simulation-engine.md).

---

## 1. Vrstvy a závislosti

Striktní jednosměrný tok závislostí. Šipka = „závisí na". `Core` nezávisí na
ničem.

```
   EchoFactory.Game ──┐         EchoFactory.Cli
   (MonoGame)         │              │
                      ▼              ▼
                EchoFactory.Content ─┴──► EchoFactory.Core
                (JSON loading)              (headless engine)
                      ▲
   EchoFactory.Steam ─┘
   (Steamworks)
```

**Železné pravidlo:** `EchoFactory.Core` nesmí mít *žádnou* závislost na
MonoGame, Steamworks, System.Drawing, ani na ničem, co dělá I/O nebo grafiku.
Důvod: Core je deterministický výpočetní stroj, který musí jít spustit v CI,
na serveru i v testech bez GUI. Pokud někdy budeš v Core psát `using
Microsoft.Xna...`, je to bug.

### Projekty (solution layout)

```
EchoFactory.sln
├── src/
│   ├── EchoFactory.Core/          # Engine. Zero deps. POCO + struct.
│   ├── EchoFactory.Content/       # JSON schémata, loader, validace, registry.
│   ├── EchoFactory.Game/          # MonoGame frontend (scény, render, input, audio).
│   ├── EchoFactory.Steam/         # Steamworks.NET za rozhraním IPlatformServices.
│   └── EchoFactory.Cli/           # Headless: validate / simulate / verify / bench.
├── tests/
│   ├── EchoFactory.Core.Tests/    # Unit + snapshot + determinismus testy.
│   └── EchoFactory.Content.Tests/ # Validace ukázkových dat, round-trip JSON.
├── data/                          # Veškerý herní obsah (viz content-format.md).
│   ├── nodes/                     # Definice uzlů (*.json).
│   ├── levels/                    # Definice levelů (*.json).
│   ├── locale/                    # Lokalizační stringy.
│   └── schema/                    # JSON schémata pro validaci.
└── docs/                          # Tato dokumentace.
```

### Proč zrovna takhle

| Rozhodnutí | Důvod | ADR |
|------------|-------|-----|
| Headless `Core` bez závislostí | Testovatelnost, determinismus, CI, server-side verify | [0001](./adr/0001-headless-core.md) |
| MonoGame za rendering abstrakcí | Zralost + možnost výměny | [0002](./adr/0002-monogame-over-raylib.md) |
| Steam za `IPlatformServices` | Hra jde spustit bez Steamu (vývoj, testy) | — |

## 2. Datové modely (Core)

Návrh preferuje `struct` pro malé hodnotové typy (cache-friendly, bez GC tlaku)
a `class` pro entity se životním cyklem.

```csharp
// Souřadnice buňky. Hodnotový typ, použitelný jako klíč (deterministická rovnost).
public readonly struct GridPoint : IEquatable<GridPoint>
{
    public readonly int X;
    public readonly int Y;
    public GridPoint(int x, int y) { X = x; Y = y; }
    // GetHashCode/Equals deterministicky z (X, Y).
}

// Směr pohybu (pásy, výstupy uzlů). Žádné floaty.
public enum Direction : byte { Up, Right, Down, Left }

// Pohybující se hodnota.
public readonly struct Item : IEquatable<Item>
{
    public readonly ItemId Id;     // DETERMINISTICKÉ id, ne Guid! (ADR-0003)
    public readonly int Value;
    public Item(ItemId id, int value) { Id = id; Value = value; }
}

// Deterministická identita itemu napříč passy/smyčkami.
public readonly struct ItemId : IEquatable<ItemId>
{
    public readonly ulong Raw;     // hash z (sourceNodeId, spawnTick, sequence)
}
```

```csharp
// Kompletní snímek mřížky v jednom tiku. Neměnný po dokončení tiku.
public sealed class GridState
{
    public int Tick;

    // Mapa obsazení. Pozn.: List<Item> kvůli merge-uzlům; jinak max 1 prvek.
    // Pro determinismus se NIKDY neiteruje v pořadí Dictionary — viz §4.
    public Dictionary<GridPoint, List<Item>> ItemMap;

    // Události čistě pro vizualizaci (spawn, merge, math, portal-in/out, collision).
    // Core je generuje, ale sám na nich nezávisí. Frontend je přehrává.
    public List<VisualEvent> VisualEvents;
}
```

```csharp
// Statická entita na mřížce. Implementace dodává Content/Core registry.
public interface INode
{
    GridPoint Position { get; }
    NodeId Id { get; }                 // deterministické, stabilní id uzlu

    // Čistá funkce: čte currentState (T), zapisuje NÁVRHY do nextState (T+1).
    // NESMÍ mutovat currentState. NESMÍ mít side-effecty mimo nextState.
    void Evaluate(SimContext ctx, GridState currentState, GridStateBuilder nextState);
}
```

```csharp
// Výsledek kompilace.
public sealed class SimulationResult
{
    public bool IsSuccess;
    public GridState[] States;         // index = tick
    public LevelOutcome Outcome;       // Solved / Failed / Paradox
    public ParadoxError? Error;        // null při úspěchu
    public SimulationStats Stats;      // FinalTick, NodeCount (footprint), passes...
}
```

> **Poznámka k `GridStateBuilder`:** uzly nezapisují přímo do `GridState`, ale do
> *builderu*, který sbírá *návrhy*. Engine je pak deterministicky složí (propose/
> commit, viz [simulation-engine.md](./simulation-engine.md)). Tím odpadá
> závislost na pořadí vyhodnocení uzlů.

## 3. Tok dat: od sestavy k přehrávání

```
 Level JSON ─┐
             ├─► [Content] parse + validate ─► LevelDefinition (POCO)
 Node JSON ──┘                                       │
                                                     ▼
 Hráčova sestava (Build) ───────────────► [Core] SimulationCompiler
                                                     │  multi-pass
                                                     ▼
                              SimulationResult { GridState[], Outcome }
                                                     │
                              ┌──────────────────────┼───────────────────┐
                              ▼                       ▼                   ▼
                       [Game] Playback        [Cli] verify        [Steam] leaderboard
                       (render + scrub)        (CI / anti-cheat)   (submit/fetch)
```

Klíč: tentýž `SimulationResult` slouží grafice, ověřování i žebříčkům. Jeden
zdroj pravdy, žádná duplicitní logika.

## 4. Pravidla determinismu (NEPŘEKROČITELNÁ)

Toto je nejdůležitější sekce celého projektu. Porušení kteréhokoli pravidla je
bug priority P0 — rozbíjí časové smyčky i žebříčky.

1. **Žádný floating point v simulaci.** Veškeré herní hodnoty jsou `int`/`long`.
   Floaty patří výhradně do rendereru (tweening, animace) a nikdy se nevracejí
   do Core.

2. **Žádná závislost na pořadí iterace `Dictionary`/`HashSet`.** Pořadí iterace
   .NET kolekcí *není* garantované. Když je třeba iterovat, **vždy se nejdřív
   seřadí** podle deterministického klíče (např. `GridPoint` lexikograficky,
   nebo `NodeId`). Helper `DeterministicOrder()` na to bude jediná povolená cesta.

3. **Žádné náhodné `Guid`.** Identita (`ItemId`, `NodeId`) je deterministicky
   odvozená. Viz [ADR-0003](./adr/0003-deterministic-item-ids.md).

4. **RNG jen seedovaný a explicitní.** Pokud vůbec (např. dekorativní efekty),
   pak `System` RNG nikdy; pouze vlastní seedovaný PRNG, jehož seed je součástí
   levelu/sestavy. Vizuální (ne-simulační) RNG smí být ve frontendu.

5. **Propose/Commit místo in-place mutace.** Uzly nemodifikují aktuální stav.
   Konflikty řeší engine jediným deterministickým algoritmem (viz engine doc).

6. **Žádný čas/locale/kultura v Core.** Žádné `DateTime.Now`, žádné
   `ToString()` závislé na `CultureInfo`. JSON parsování s `InvariantCulture`.

7. **Stejný build = stejný hash.** Test: zkompiluj sestavu dvakrát, zhashuj
   `GridState[]`, hashe se musí rovnat. Toto je automatický test (viz
   [testing.md](./testing.md)).

> Tip: zvážit i cross-platform determinismus testy (Linux/Windows v CI), protože
> právě tady se skryté nedeterminismy odhalí.

## 5. Scény a herní smyčka (frontend)

Frontend používá **State pattern** nad hlavní `Game.Update/Draw` smyčkou. Každá
scéna je izolovaný stav s vlastním `Load/Update/Draw/Unload`. Detaily a vizuální
styl: [frontend.md](./frontend.md).

```
BootScene ─► MainMenuScene ─► LevelSelectScene ─► GameplayScene
                                   ▲                    │
                                   └────────────────────┘
                              LevelEditorScene  (z menu / level selectu)
```

| Scéna | Odpovědnost |
|-------|-------------|
| `BootScene` | Init `IPlatformServices` (Steam), načtení JSON registrů, assetů. |
| `MainMenuScene` | Navigace. |
| `LevelSelectScene` | Mapa/strom levelů, náhled žebříčků. |
| `GameplayScene` | Build → Compile → Playback (sub-stavy). |
| `LevelEditorScene` | Tvorba levelů, omezení inventáře, export JSON. |

## 6. Chybové stavy

Všechny paradoxy dědí z `ParadoxError` a nesou dost kontextu pro UI i ladění
(tik, souřadnice, zúčastněné itemy/uzly). Výčet a sémantika:
[simulation-engine.md → Paradoxy](./simulation-engine.md). Frontend je
prezentuje jako čitelné, nápomocné chyby — ne stacktrace.

## 7. Konfigurace a sestavení

- **`.NET 8+`**, `nullable enable`, `TreatWarningsAsErrors` v `Core`.
- `System.Text.Json` se **source generátorem** (rychlost, AOT-friendly, žádná
  reflexe za běhu).
- `Directory.Build.props` pro sdílené nastavení; analyzátory zapnuté.
- CI builduje a testuje `Core`/`Content`/`Cli` na Linuxu (bez GUI). `Game` se
  builduje, ale neběží headless. Viz [testing.md](./testing.md).
