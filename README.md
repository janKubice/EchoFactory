# EchoFactory

> 2D grid-based logická puzzle hra o budování výrobních linek, které manipulují
> s čísly v **diskrétním čase a prostoru**. Jádrem hry je práce s **kauzalitou**
> a **časovými paradoxy** — item poslaný portálem do minulosti ovlivní svoji
> vlastní budoucnost.

*(Původní pracovní název: ChronoMath. Repozitář a finální název: **EchoFactory** —
„Echo" = časové ozvěny/smyčky, „Factory" = budování linek.)*

---

## Stav projektu

✅ **Engine hotový (M0–M3) + hratelný frontend.** Deterministické jádro vč.
**časových smyček**, JSON data-driven obsah (12 uzlů, 6 levelů kampaně),
headless CLI a **MonoGame frontend** (menu → výběr levelu → build/compile/
playback). **30 testů** zelených. Detaily a další kroky v [`ROADMAP.md`](./ROADMAP.md).

## Co to bude

Hráč staví na 2D mřížce výrobní linku z pásů, generátorů, matematických uzlů a
**časových portálů**. Po stisku „Compile" se linka **deterministicky předpočítá**
do pole stavů (`GridState[]`) a hráč si výsledek přehraje jako film s časovou
osou (play / pause / scrubbing). Cílem je dopravit do cíle (`Sink`) správnou
sekvenci čísel ve správný čas.

Tři pilíře (detail v [`docs/design.md`](./docs/design.md)):

1. **Determinismus nade vše** — stejný vstup = vždy stejný výstup, na jakémkoli
   stroji. Umožňuje to časové smyčky, ověřitelné žebříčky i snadné testování.
2. **Data-Driven Design** — veškerý obsah (uzly, levely) je definován v externím
   JSON. Přidat nový stroj znamená přidat soubor, ne kompilovat kód.
3. **Vizuální čistota** — vektorový minimalismus inspirovaný Mini Metro,
   dark-mode paleta s neonovými akcenty.

## Architektura ve zkratce

```
┌─────────────────────────────────────────────────────────────┐
│  EchoFactory.Game  (MonoGame)   ── prezentace, vstup, zvuk   │
│  EchoFactory.Steam (Steamworks) ── žebříčky, Workshop        │
├─────────────────────────────────────────────────────────────┤
│  EchoFactory.Content            ── načítání a validace JSON  │
├─────────────────────────────────────────────────────────────┤
│  EchoFactory.Core   ── HEADLESS deterministický engine       │
│  (žádné závislosti, čisté C#, plně testovatelný)             │
└─────────────────────────────────────────────────────────────┘
```

`Core` neví nic o grafice ani Steamu. Jde spustit z příkazové řádky
(`EchoFactory.Cli`), což využijeme pro CI, validaci levelů a ověřování
žebříčků re-simulací.

## Dokumentace

| Dokument | Obsah |
|----------|-------|
| [`ROADMAP.md`](./ROADMAP.md) | **Hlavní plán** — milníky M0–M8, exit kritéria, rizika |
| [`docs/design.md`](./docs/design.md) | Herní vize, pilíře, mechaniky, navržená vylepšení |
| [`docs/architecture.md`](./docs/architecture.md) | Struktura solution, pravidla determinismu, tok dat |
| [`docs/simulation-engine.md`](./docs/simulation-engine.md) | Multi-pass kompilátor, časový model, paradoxy |
| [`docs/content-format.md`](./docs/content-format.md) | JSON schémata (node, level, save), modding, verzování |
| [`docs/frontend.md`](./docs/frontend.md) | MonoGame, scény, vykreslování, vizuální styl |
| [`docs/ui-ux.md`](./docs/ui-ux.md) | Kompletní herní shell — menu, nastavení, HUD, save, onboarding |
| [`docs/meta-services.md`](./docs/meta-services.md) | Steam, žebříčky, Workshop, anti-cheat |
| [`docs/testing.md`](./docs/testing.md) | Strategie testů, determinismus, CI |
| [`docs/glossary.md`](./docs/glossary.md) | Sdílený slovník pojmů (CZ ↔ EN) |
| [`docs/adr/`](./docs/adr/) | Architecture Decision Records — proč jsme se rozhodli jak |
| [`CONTRIBUTING.md`](./CONTRIBUTING.md) | Konvence, git workflow, jak přispívat |

## Technologie

- **Backend:** čisté C# / .NET 8+, POCO, `struct`, žádné závislosti.
- **Frontend:** MonoGame (za rendering abstrakcí — viz [ADR-0002](./docs/adr/0002-monogame-over-raylib.md)).
- **Serializace:** `System.Text.Json` (source-generated, AOT-friendly).
- **Integrace:** Steamworks.NET (žebříčky + Workshop), izolovaná za rozhraním.

## Rychlý start

Potřebuješ **.NET 8 SDK**. (V Claude Code na webu ho doinstaluje SessionStart hook.)

```bash
# Backend testy (žádné GUI, běží i v CI)
dotnet test

# Headless: validace obsahu, výpis, spuštění levelu jako ASCII trace
dotnet run --project src/EchoFactory.Cli -- validate
dotnet run --project src/EchoFactory.Cli -- list
dotnet run --project src/EchoFactory.Cli -- run lvl_loop_01 sol_loop_01

# Spuštění HRY (potřebuje desktop s OpenGL — ne headless server)
dotnet run --project src/EchoFactory.Game
```

### Ovládání hry

**Build mode:** `1–5` výběr nástroje (pás / +add / ×mul / splitter / portál) ·
levým tlačítkem (drag) položit · pravým smazat · `R` otočit · `L` načíst
referenční řešení · `X` smazat vše · `Space`/`COMPILE` spustit simulaci · `Esc` zpět.

**Playback:** `Space` play/pauza · `←/→` krok · tažením po časové ose scrubbing ·
`B` zpět do editace · `Esc` zpět. Zobrazí se výsledek (Solved/Paradox) a hvězdičky.

> Tip: na novém levelu zmáčkni `L` (načte referenční řešení) a `Space` — uvidíš
> hru hned v akci, vč. `lvl_loop_01`, kde item dorazí do cíle *dřív, než vznikne*.

## Licence

TBD — viz [ROADMAP.md → M0](./ROADMAP.md).
