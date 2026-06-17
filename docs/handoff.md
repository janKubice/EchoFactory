# Handoff — EchoFactory

Předávací dokument pro **další LLM/agenta**, který bude pokračovat. Čti celé.
Doplňky: vize a design ([design.md](./design.md)), engine
([simulation-engine.md](./simulation-engine.md)), feedback/úkoly
([playtest-feedback.md](./playtest-feedback.md)), roadmap ([../ROADMAP.md](../ROADMAP.md)).

---

## 1. Co to je a kde to je

**EchoFactory** = 2D grid puzzle hra (Zachtronics/Mini Metro styl) o budování
výrobních linek manipulujících s čísly v diskrétním čase; USP = časové portály.
Plně **data-driven** (JSON) a postavené na **deterministickém headless enginu**.

- **Git:** vyvíjí se na větvi **`claude/beautiful-faraday-hjkuoh`** repozitáře
  `janKubice/EchoFactory`. Hlavní/jediná větev (zatím se nemerguje do main).
- **Komunikace s uživatelem: ČESKY.** Kód, JSON klíče, commity, identifikátory:
  **anglicky** (viz [glossary.md](./glossary.md)).
- **Stav (2026-06-16):** hratelná hra s 14 levely (kampaň s onboardingem),
  8 typy uzlů, editorem levelů, lokálními žebříčky, zvuky, nastavením, undo/redo,
  anti-cheat. **~54 testů**, vše builduje. ~22 commitů.

## 2. Prostředí, build, test, push (DŮLEŽITÉ)

- **Cloud/headless:** GUI (`EchoFactory.Game`) tu **nejde spustit** (není
  displej/GPU) — jen se **kompiluje**. Vizuální chování ověřuje **uživatel
  lokálně**; spoléhej na jeho playtest feedback. Engine/Content/CLI jsou plně
  testovatelné headless.
- **.NET 8 SDK:** ve webových sessionách ho doinstaluje **SessionStart hook**
  (`.claude/hooks/session-start.sh` → `~/.dotnet`). Jinak:
  `curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0 --install-dir ~/.dotnet`
  a `export PATH="$HOME/.dotnet:$PATH"`.
- **Příkazy** (z kořene repa):
  ```bash
  dotnet build EchoFactory.sln
  dotnet test  EchoFactory.sln
  dotnet run --project src/EchoFactory.Cli -- validate   # ověří /data (uzly+levely+řešení)
  dotnet run --project src/EchoFactory.Cli -- list|run|verify|bench
  dotnet run --project src/EchoFactory.Game              # jen lokálně (desktop+OpenGL)
  ```
- **Push:** git proxy v tomto prostředí je **read-only** (403). Pushuje se
  **přímo na github.com tokenem, který dodá uživatel v chatu**:
  `git push "https://x-access-token:<TOKEN>@github.com/janKubice/EchoFactory.git" <branch>:<branch>`
  — **TOKEN nikdy nedávej do souborů/commitů**, z výstupu ho rediguj (`sed`).
  Po dokončení uživateli připomeň rotaci tokenu (je v historii chatu).
- **MCP GitHub** nástroje jsou v tomto prostředí **read-only** (write = 403).
- **Artefakty:** `bin/`, `obj/`, `.dotnet/` jsou v `.gitignore` — nikdy je
  necommituj (kontroluj `git status` před commitem).

## 3. Architektura (vrstvy, jednosměrné závislosti)

```
EchoFactory.Game (MonoGame DesktopGL) ─┐         EchoFactory.Cli
                                       ▼              ▼
                           EchoFactory.Content ──────┴──► EchoFactory.Core
                           (JSON load/save/validate)      (headless engine, 0 deps)
```
Testy: `EchoFactory.Core.Tests` (28), `EchoFactory.Content.Tests` (20),
`EchoFactory.Game.Tests` (6 — běží headless, netýká se MonoGame typů).

**Core (`src/EchoFactory.Core`)** — čistý C#, žádné závislosti, `TreatWarningsAsErrors`.
- Primitivy: `GridPoint`, `Direction`, `Item`, deterministické `ItemId`/`NodeId`
  (FNV-1a, **nikdy Guid** — ADR-0003), `GridSize`.
- `INode` + uzly: `GeneratorNode`, `BeltNode`, `SinkNode`, `GenericMathNode`
  (binární i unární s konstantou), `SplitterNode`, `PortalNode`, `FilterNode`.
- Tik = **propose/commit** (`GridStateBuilder`): uzly navrhnou výstupy proti
  stavu T, builder je **deterministicky** složí do T+1; veškerý nedeterminismus
  izolovaný do jednoho seřazeného kroku.
- **Multi-pass kompilátor** (`SimulationCompiler`): bez portálů 1 průchod; s
  portály iteruje **pevný bod injekcí** `F(I*) = I*` (`InjectionSet`), detekuje
  oscilaci/nekonvergenci → `TemporalParadox`. Po vyřešení **ořízne** časovou osu
  (early stop).
- Paradoxy: Collision / Void / Math / Temporal.
- Metadata levelu (nepoužité v simulaci): `Par`, `Inventory`, `Name`,
  `Description`, `Order`, `StrictTiming`, `MaxTemporalPasses`.
- `StarRating`, `StateHasher`.

**Content (`src/EchoFactory.Content`)** — JSON ↔ POCO, žádné MonoGame.
- Loadery: `NodeRegistry`, `LevelLoader`, `SolutionLoader` (fail-loud chyby).
- **Writery: `LevelWriter`, `SolutionWriter`** (round-trip testované) — základ
  editoru a ukládání.
- `ContentLibrary.Validate` (re-simuluje referenční řešení), `InventoryCheck`,
  `SubmissionVerifier` (anti-cheat), `Leaderboard` + `LeaderboardStore`.
- `schema_version` gate; `JsonConfig.Options` (snake_case, ignore null, indented).
- Interní DTO v `Internal/Dtos.cs`.

**Game (`src/EchoFactory.Game`)** — MonoGame DesktopGL, **bez externích assetů**
(vlastní bitmapový font `VectorFont` + primitiva `Renderer`).
- `EchoGame` (host) → `SceneManager` (drží `Catalog`, `Settings`, `Audio`,
  `Leaderboard`, `Quit`, `ScreenW/H`).
- Scény: `MainMenuScene`, `LevelSelectScene`, `GameplayScene` (build→compile→
  playback), `LevelEditorScene`, `SettingsScene`.
- `Renderer`: `FillRect/RectOutline/Line/Disc/Ring/Text/**TextCenteredFit**`.
  **POZOR na font:** glyf je 5×7 „pixelů"; `pixel` parametr = velikost jednoho
  pixelu → `TextCenteredFit(text, center, maxW, maxH, color)` text **automaticky
  zmenší do boxu** (historicky byl bug, kdy text byl 1,4× větší než buňka — vždy
  používej Fit pro věci v buňkách).
- `BuildEditor` (snapshot undo/redo), `GridView` (mřížka↔obrazovka),
  `LevelCatalog` (`.Reload()` re-scan), `DataLocator` (najde `/data` výš od exe).
- Audio: `Synth` (PCM tóny) + `AudioManager` (fail-soft bez zvukovky).
- `Settings` (`GameSettings` + `SettingsStore`, JSON vedle exe).

**Data (`/data`)** — `nodes/` (13 def), `levels/` (14), `solutions/` (14).
Formát: [content-format.md](./content-format.md). Levely se kopírují k exe (csproj
`<None Include="..\..\data\**">`).

## 4. Neporušitelná pravidla (determinismus)
Viz [architecture.md §4](./architecture.md). Stručně: v Core **žádný float**,
**žádná závislost na pořadí iterace `Dictionary`** (vždy seřadit), **žádné Guid**,
žádný `DateTime.Now`/kultura. Test: zkompiluj 2× → bitově shodný `StateHasher`.
Porušení = P0 (rozbije časové smyčky i žebříčky).

## 5. Konvence / gotchas
- Core/Content mají `TreatWarningsAsErrors` → drž čistý build (analyzery). Game/CLI
  ne. Content **nemá** warnaserror (kvůli JSON DTO + CA pravidlům).
- Po každé práci: `dotnet build` + `dotnet test` + `validate` zelené; commit
  (anglicky, věcně); push tokenem; pak česky shrnout uživateli.
- Nový **level** = JSON v `data/levels/` + **referenční řešení** v
  `data/solutions/` (jinak `validate` u řešení ne­ověří; level sám projde) →
  spustit `validate`, opravit dokud nesolví. Par/timing dolaď podle výstupu.
- Nový **typ uzlu** = `NodeKind` + `*Config` + `*Node` v Core + case v
  `NodeFactory` (v `SimulationCompiler.cs`) + `Tokens.Kind`/parser v
  `Content/Internal/JsonConfig.cs` + `SolutionLoader`/`SolutionWriter` case +
  `node_*.json` + frontend (paleta/render/DefId/ToolOf v `GameplayScene`). +testy.
- Frontend nejde vizuálně ověřit → piš obranně (try/catch u IO/audio), drž
  `dotnet build src/EchoFactory.Game` zelený, a **explicitně řekni uživateli, ať
  to proklikne**.

## 6. Co dělat dál (priorita z [playtest-feedback.md](./playtest-feedback.md))
1. **Konfigurace uzlů klikem** (panel po kliknutí na uzel) + zřetelné vstupy/
   výstupy math a splitteru. (Současné +/-/Tab „adjust" hráč nechápe.)
2. **Editor → klikací UI:** jméno levelu, max ticks, inventář, velikost gridu,
   pořádný číselník (čísla > 9), tlačítko TEST, klikací paleta uzlů.
3. **Kampaně + scroll:** pole `campaign` v levelu + editoru; výběr Play → kampaň →
   levely; neomezeně (scroll kolečkem).
4. **Smyčky (design ⭐, dohodni s hráčem):** router/podmíněný splitter (2 výstupy
   dle podmínky), akumulátor (běžící součet), levely kolem akumulačních smyček.
   Vyjasnit portál (temporální) vs prostorová belt-smyčka. Lepší vysvětlení/
   tutoriál portálu.
5. **Vizuál:** zatočené pásy (oblouk dle vstup/výstup), hezčí tlačítka (mřížka/
   ikony), mírně lepší uzly, **animované pozadí menu** (běžící „nesmyslná" továrna).
6. **Build/nastavení:** `dotnet publish` win-x64 self-contained **EXE** (do README);
   nastavení **fullscreen/windowed** + rozlišení (aplikovat na
   `GraphicsDeviceManager`, přepočítat layouty scén — berou `ScreenW/H`).

> Bod 4 je **designové rozhodnutí** — než kódovat router/akumulátor, ptej se
> uživatele (AskUserQuestion): chce portál jako prostorovou smyčku, nebo přidat
> nové uzly a portál nechat temporální? Viz [playtest-feedback.md §H](./playtest-feedback.md).

## 7. Rychlá orientace v souborech
| Chci… | Soubor |
|------|--------|
| změnit pravidla simulace | `Core/Simulation/SimulationCompiler.cs`, `GridStateBuilder.cs` |
| přidat/upravit uzel (engine) | `Core/Nodes/*` + `NodeFactory` v `SimulationCompiler.cs` |
| JSON formát / parsování | `Content/LevelLoader.cs`, `SolutionLoader.cs`, `Internal/Dtos.cs`, `Internal/JsonConfig.cs` |
| ukládání levelu/řešení | `Content/LevelWriter.cs`, `SolutionWriter.cs` |
| herní obrazovku / UX | `Game/Scenes/*` (hlavně `GameplayScene.cs`, `LevelEditorScene.cs`) |
| kreslení / font | `Game/Rendering/Renderer.cs`, `VectorFont.cs`, `Palette.cs` |
| žebříčky / anti-cheat | `Content/Leaderboard.cs`, `SubmissionVerifier.cs` |
| levely/obsah | `data/levels/*`, `data/solutions/*`, `data/nodes/*` |
