# Roadmap — EchoFactory

Fázovaný plán vývoje. **Strategie:** nejdřív postavit *neprůstřelný
deterministický single-pass engine*, pak na něj posadit *časové smyčky* (USP),
teprve pak grafiku, editor a Steam. Nejrizikovější technický kus (temporální
pevný bod) přichází v M3 — po pevných základech, ale dřív, než na něm bude stát
hromada UI práce.

> Tohle je **roadmap, ne kalendář.** Místo vymyšlených dat používá relativní
> velikost (S / M / L / XL) a jasná *exit kritéria*. U sólo/malého týmu je
> pořadí a „hotovo = splněná kritéria" důležitější než termíny.

## Orientace: Now / Next / Later

| | Milníky | Stav |
|---|---|---|
| **DONE** | engine + **frontend** 🎮 · inventář · strict-timing · filter · **anti-cheat verify** | ✅ 43 testů, builduje + hraje se lokálně |
| **NOW** | Doladit frontend (nastavení, konfigurace uzlů v UI, audio) + víc obsahu | 🔜 |
| **NEXT** | M6 editor levelů · M7 Steam (žebříčky, Workshop) | navrženo |
| **LATER** | M8 release (přístupnost, polish) | navrženo |

> **Frontend (hratelný slice):** `EchoFactory.Game` (MonoGame DesktopGL, vektorové
> vykreslování bez externích assetů — vlastní bitmapový font + primitiva). Scény
> MainMenu → LevelSelect → Gameplay (build mode: umisťování pásů/math/splitter/
> portálů myší, rotace, načtení referenčního řešení; Compile; Playback s časovou
> osou, scrubbingem a tweeningem itemů; výsledek + hvězdičky). Spuštění:
> `dotnet run --project src/EchoFactory.Game` (vyžaduje desktop s OpenGL). Vizuální
> ladění a chybějící kusy (SettingsScene, plná konfigurace uzlů, undo/redo, audio)
> jsou „NOW".

> **Implementační stav (M1–M3 + prohloubení):** `EchoFactory.Core` má
> deterministický engine — propose/commit tik; uzly Generator/Belt/Sink/
> **GenericMath** (binární i **unární** s konstantou)/**Splitter**/**Portal**;
> **multi-pass kompilátor** hledající pevný bod injekcí (`F(I*) = I*`) s detekcí
> oscilace a nekonvergence; paradoxy Collision/Void/Math/**Temporal**; **par +
> hvězdičkové hodnocení**. `EchoFactory.Content` načítá a validuje JSON s
> `schema_version` gate a „fail loud" chybami. `/data/` má 12 uzlů + **6 levelů**
> (kampaň: line, adder, splitter, unární ×, dvoustupňová linka, **časová smyčka**)
> + 6 referenčních řešení. `EchoFactory.Cli` umí
> `demo`/`list`/`validate`/`run`/`verify`(★)/`bench` — `run lvl_loop_01` ukáže item
> doručený **dřív, než vznikne**. **30 testů** (determinismus, bootstrap i
> nestabilní smyčky, unární math, hvězdičky). Celá hra je „hratelná" přes CLI vč.
> času. `Game`/`Steam` → M4/M7.

## Klíčové mezníky (milestones napříč fázemi)

- 🧪 **Engine-complete** = konec **M3** — celá hra je hratelná „v hlavě" / přes
  CLI, vč. časových smyček. Žádná grafika, ale jádro hotové a otestované.
- 🎮 **Vertical slice (hratelné)** = konec **M5** — myší postavíš, zkompiluješ,
  přehraješ level. První „opravdová hra".
- 📦 **Content-complete** = konec **M6** — editor + sada levelů + lokalizace.
- 🚀 **1.0 / Steam release** = konec **M8**.

## Graf závislostí

```
M0 ─► M1 ─► M2 ─► M3 ─────────────► (engine-complete)
                   │
                   ▼
            M4 ─► M5 ─► M6 ─► M7 ─► M8 ─► (1.0)
```

M4+ (frontend) může *začít* paralelně po M2 (umí renderovat statický
`GridState`), ale plnou hodnotu má až nad M3.

---

# Fáze

## M0 — Setup & rozhodnutí · `S`
**Cíl:** prázdný, ale správně nastavený projekt; rozhodnuté základy.

**Deliverables**
- `EchoFactory.sln` + skeleton projektů (`Core`, `Content`, `Game`, `Steam`,
  `Cli`) a testů — viz [architecture.md → §1](./docs/architecture.md).
- `Directory.Build.props`, `.editorconfig`, `.gitignore`, analyzátory,
  `nullable`, `TreatWarningsAsErrors` v `Core`.
- CI skeleton (build + test na Linuxu) — [testing.md → §6](./docs/testing.md).
- Volba **licence** (open-source? proprietární?) — *otevřená otázka, viz níže*.
- Stub JSON schémata v `/data/schema/`.

**Exit kritéria**
- `dotnet build` a `dotnet test` projdou (i s prázdnými testy).
- CI je zelené na první commit.

**Rizika:** žádná zásadní. Hlavně neztratit čas na bikeshedding.

---

## M1 — Core: deterministický single-pass engine · `L`
**Cíl:** srdce simulace *bez portálů*. Tohle musí být betonové.

**Deliverables**
- Datové modely: `GridPoint`, `Item`, `ItemId`, `GridState`, `INode`,
  `SimulationResult` — [architecture.md → §2](./docs/architecture.md).
- **Propose/Commit tik** — [simulation-engine.md → §2](./docs/simulation-engine.md).
- Základní uzly v kódu: `Generator`, `Sink`, `Belt`.
- Deterministické `ItemId` — [ADR-0003](./docs/adr/0003-deterministic-item-ids.md).
- Paradoxy `Collision` a `Void`.
- Vyhodnocení splnění levelu (sink sekvence).
- `EchoFactory.Cli simulate <level> <build>` (textový výpis stavů).

**Exit kritéria**
- Unit + snapshot testy pro tik a uzly zelené.
- **Property test determinismu** (dvě kompilace = bitově shodné) zelený —
  [testing.md → §2](./docs/testing.md).
- Triviální level (generator → belt → sink) se vyřeší přes CLI.

**Rizika**
- Skrytý nedeterminismus (pořadí kolekcí). *Mitigace:* helper
  `DeterministicOrder`, determinismus test od prvního dne.

---

## M2 — Data pipeline + CLI nástroje · `M`
**Cíl:** obsah je opravdu data; engine instancuje uzly z JSON.

**Deliverables**
- Loader + validace JSON (uzly, levely) proti schématům, **fail loud** —
  [content-format.md](./docs/content-format.md).
- **Node registry**; `GenericMathNode` (pevná sada operací) +
  `Splitter` — [ADR-0004](./docs/adr/0004-fixed-operation-set-vs-expression-dsl.md).
- `schema_version` + migrační rámec.
- CLI: `validate`, `verify <solution>`, `bench`.
- První „shipnutá" sada `/data/nodes/` (add, sub, mul, div, mod, belt, splitter,
  generator, sink) + pár tutorial levelů.

**Exit kritéria**
- `validate ./data` zelené v CI.
- Matematický level vyřešen z čistě JSON-definovaných uzlů (žádný hardcode uzlu
  navíc).
- Round-trip a migrační testy zelené.

**Rizika**
- Návrh schématu, který se brzy změní. *Mitigace:* `schema_version` od začátku,
  migrace zlevňují změny.

---

## M3 — Temporální engine (USP) · `L`
**Cíl:** časové portály a multi-pass pevný bod. Nejtěžší a nejvíc odlišující kus.

**Deliverables**
- `PortalNode` (`TimeOffset`, do minulosti i budoucnosti).
- **Multi-pass kompilátor** s hledáním pevného bodu, detekcí konvergence,
  oscilace a nekonvergence — [simulation-engine.md → §3](./docs/simulation-engine.md).
- `TemporalParadox` (+ `MathParadox`, pokud ještě nebyl).
- Testy: bootstrap (konverguje), grandfather (paradox), oscilace (paradox), cap
  passů.
- Cross-platform determinismus pro temporální levely (Linux+Windows v CI).

**Exit kritéria** → 🧪 **engine-complete**
- Bootstrap level se vyřeší a je stabilní (`F(I*) = I*`).
- Grandfather a oscilace vrátí správný `TemporalParadox`.
- Celá hra je „hratelná" přes CLI vč. časových smyček.

**Rizika (nejvyšší v projektu)**
- Konvergence/oscilace subtilní, těžko laditelné. *Mitigace:* malé
  reprodukovatelné testovací levely, hashování stavů, detailní logy passů,
  detekce cyklu hashí.
- Kombinatorická exploze passů u mnoha portálů. *Mitigace:* cap
  `max_temporal_passes`, měkký limit portálů v editoru, `bench`.

---

## M4 — Frontend foundation + UI shell (MonoGame) · `L`
**Cíl:** vykreslit `GridState`, mít kostru scén a **základ herního UI**. (Může
začít paralelně po M2.)

**Deliverables**
- MonoGame projekt, `IRenderer` + `MonoGameRenderer` —
  [frontend.md](./docs/frontend.md), [ADR-0002](./docs/adr/0002-monogame-over-raylib.md).
- **Vlastní lehký UI toolkit** (tlačítka, panely, slidery, taby, dialogy, toasty,
  fokus) — [ADR-0005](./docs/adr/0005-ui-toolkit.md), [ui-ux.md](./docs/ui-ux.md).
- `SceneManager` + overlay stack; `BootScene`, `MainMenuScene`, **`SettingsScene`**.
- **Nastavení + persistence** (`settings.json`): obraz/zvuk/ovládání/jazyk/přístupnost.
- Render statického `GridState` ve vektorovém stylu (mřížka, uzly, itemy).
- `IPlatformServices` + `NullPlatformServices` (Steam zatím no-op).

**Exit kritéria**
- Hra se spustí, ukáže **funkční menu a nastavení** (uložení/načtení), a vykreslí
  zkompilovaný `GridState` v cílovém vizuálním stylu.

**Rizika:** podcenění vektorového renderu (linky, antialiasing). *Mitigace:*
brzká stylová „spike".

---

## M5 — Playback + Build mode (hratelný slice) · `L`
**Cíl:** kompletní core loop myší. První opravdová hra.

**Deliverables**
- `GameplayScene`: Build → Compile (na pozadí) → Playback.
- **Herní HUD** ([ui-ux.md §5](./docs/ui-ux.md)): paleta uzlů, počítadla inventáře/
  footprint/tick, Compile tlačítko, panel paradoxu, výsledková karta (★).
- **Build mode**: paleta z `inventory`, umisťování, rotace, **undo/redo**
  (command pattern).
- **Playback**: play/pause/step/reset, **timeline scrubbing**, rychlosti.
- **`PauseOverlay`** (ESC) + potvrzovací dialogy.
- **Tweening** itemů + `VisualEvent` efekty (spawn, merge, math, portal, paradox).
- **Save řešení + osobní rekordy** (auto-save, `schema_version`, `level_hash`).

**Exit kritéria** → 🎮 **vertical slice**
- Hráč myší postaví, zkompiluje a přehraje tutorial level vč. levelu s portálem,
  s plným HUD a pauzou; řešení se uloží a načte.
- Paradox se zobrazí srozumitelně, ne jako pád.

**Rizika:** UX kompilace na pozadí (nesmí škubat). *Mitigace:* background thread,
progress, immutabilní `SimulationResult`.

---

## M6 — Level editor + obsah + lokalizace · `L`
**Cíl:** tvorba levelů a skutečná náplň hry. → 📦 **content-complete**

**Deliverables**
- `LevelEditorScene`: definice gridu, umístění generátorů/sinků/překážek,
  omezení inventáře, export JSON.
- Editor ověří **řešitelnost** (autor dodá referenční řešení; spustí `validate`).
- Sada levelů: tutorial + kampaň po obtížnostních pásmech (vč. temporálních).
- **Lokalizace** (`/data/locale`, cs + en), externalizace všech textů.
- **Onboarding + Codex** ([ui-ux.md §8](./docs/ui-ux.md)): tutoriálové levely,
  kontextové nápovědy, encyklopedie uzlů generovaná z JSON, vizualizace smyček.

**Exit kritéria**
- Level vytvořený v editoru jde uložit, znovu načíst a vyřešit.
- Hratelná kampaň od tutoriálu po pokročilé temporální levely.

**Rizika:** křivka učení časových smyček. *Mitigace:* pečlivý onboarding, debug
overlay „časové stopy", postupné zavádění mechanik.

---

## M7 — Steam integrace · `M`
**Cíl:** žebříčky a Workshop.

**Deliverables**
- `SteamPlatformServices` (Steamworks.NET) za `IPlatformServices`.
- **Žebříčky**: dvě tabulky/level (Fewest Ticks, Smallest Footprint) —
  [meta-services.md → §2](./docs/meta-services.md).
- **Anti-cheat klient-side**: odeslání = sestava + re-simulace + ověření metriky.
- **Workshop**: browse / subscribe / auto-download / publish (levely i node-mody),
  validace importu.
- Manifest balíčků (`pack.json`), priorita načítání (core < workshop < local).

**Exit kritéria**
- Skóre se zapíše jen po úspěšné re-simulaci sestavy.
- Komunitní level lze publikovat, stáhnout a zahrát.
- Hra plně funguje i **bez** Steamu (`NullPlatformServices`).

**Rizika:** specifika Steamworks (callbacky, UGC limity). *Mitigace:* izolace za
rozhraním, manuální testy na Steamu.

---

## M8 — Polish, přístupnost, release · `L`
**Cíl:** z „funguje" udělat „vyladěné". → 🚀 **1.0**

**Deliverables**
- **Přístupnost**: colorblind-safe paleta, tvary+ikony (ne jen barva),
  škálování písma, reduced motion, high-contrast — [ui-ux.md §9/12](./docs/ui-ux.md).
- **Audio** (Mini Metro styl), nastavení, klávesové mapování.
- **`ProfileScene`** (statistiky, rekordy) + **`CreditsScene`** + toast notifikace.
- **Steam Cloud** sync profilu/savů/nastavení; ruční sloty.
- **Achievementy** (datově, z `SimulationResult`).
- **Server-side re-validace** žebříčků (posílení anti-cheatu) —
  [meta-services.md → §2](./docs/meta-services.md).
- Balancing parů (`par`), výkonové ladění (`bench`), store assety, build/release
  pipeline.

**Exit kritéria**
- Stabilní, lokalizovaný, přístupný build připravený na Steam.
- Žebříčky odolné proti podvádění; Workshop živý.

---

# Souhrn rizik (cross-cutting)

| Riziko | Dopad | Mitigace | Kde |
|--------|-------|----------|-----|
| Skrytý nedeterminismus | rozbije smyčky i žebříčky | determinismus testy od M1, cross-platform CI | [testing.md](./docs/testing.md) |
| Temporální konvergence | nejtěžší kus | malé reprodukce, hash/cyklus detekce, cap | [simulation-engine.md](./docs/simulation-engine.md) |
| Scope creep (editor+workshop+žebříčky) | nedodání | striktní anti-scope, fázování, slice v M5 | [design.md → §8](./docs/design.md) |
| Křivka učení (čas) | hráč to nepochopí | onboarding, debug overlay, postupné mechaniky | [design.md](./docs/design.md) |
| Steam specifika | zdržení v M7 | izolace za `IPlatformServices` | [meta-services.md](./docs/meta-services.md) |

# Backlog (zaparkované nápady)

Prohloubení enginu/obsahu odložené ve prospěch frontendu — vrátit se k nim:

- ✅ **Strict-timing cíle** — HOTOVO: sink s `expected_ticks` + `strict_timing`;
  kompilátor ověřuje přesné tiky doručení. Level `lvl_timed_01`.
- ✅ **Filter/Gate uzel** — HOTOVO: `FilterNode` (eq/ne/lt/le/gt/ge vs. konstanta),
  jinak item zahodí. Level `lvl_filter_01` (propusť ≥3); nástroj `6` ve frontendu.
- ✅ **Inventory enforcement** — HOTOVO: model `LevelInventory` (whitelist/
  blacklist + limity), parsování JSON, enforcement ve `validate` i v editoru
  (paleta ukazuje použito/limit, blokované nástroje). Levely kampaně mají limity.
- **Víc kampaňových levelů** + těžší temporální hádanky (bootstrap, víc portálů).
- ✅ **Server-side verify** — HOTOVO: `SubmissionVerifier` re-simuluje submit a
  ověří inventář + že řeší + tvrzené metriky. CLI `verify ... --ticks N --footprint M`.

# Otevřené otázky (k rozhodnutí)

1. **Licence projektu** (M0) — open-source vs proprietární? Ovlivní přístup
   komunity k modům i kódu.
2. **Strict timing sinků** — default přesné tiky, nebo jen pořadí? (návrh:
   per-level flag `strict_timing`, default `false`).
3. **Priorita uzlů při merge** — pevně dle `NodeId`, nebo explicitní `priority`
   v JSON? (návrh: explicitní s fallbackem).
4. **Limit portálů na level** — tvrdý, nebo měkký s varováním? (návrh: měkký).
5. **Rozsah kampaně pro 1.0** — kolik levelů je „dost"? (návrh: stanovit v M6).

---

*Tato roadmapa je živý dokument. Po každém milníku ji aktualizuj (stav Now/Next/
Later a otevřené otázky).*
