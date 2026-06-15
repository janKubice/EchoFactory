# Design — vize, pilíře a mechaniky

Tento dokument shrnuje *co* stavíme a *proč*. Technické *jak* je v
[`architecture.md`](./architecture.md), [`simulation-engine.md`](./simulation-engine.md)
a [`content-format.md`](./content-format.md).

---

## 1. Pitch

> Postav výrobní linku, která dopraví správná čísla na správné místo ve správný
> tik. Pak ji zkompiluj a sleduj, jak se rozběhne. Když ti dojdou políčka nebo
> tiky, **pošli si pomoc z budoucnosti** — ale pozor, ať si nezničíš vlastní
> minulost.

EchoFactory kombinuje:

- **Zachtronics-style optimalizaci** (otevřené puzzle, žebříčky za efektivitu),
- **factory-builder budování** (pásy, rozdělovače, tok materiálu),
- **time-loop mechaniku** jako *unique selling point* — žádná jiná logická hra
  nestaví celou kampaň na sebe-konzistentních časových smyčkách.

## 2. Designové pilíře

Každé rozhodnutí poměřujeme těmito pilíři. Když je něco v rozporu, vyhrává pilíř
výše.

### Pilíř 1 — Determinismus nade vše
Stejná sestava + stejný level = **bitově shodný** výsledek na jakémkoli stroji,
OS i běhu. Není to jen technický detail, je to *herní mechanika*: hráč musí
důvěřovat, že simulace je předvídatelná, jinak nelze řešit hádanky s časovými
smyčkami. Determinismus zároveň zdarma dává:
- ověřitelné žebříčky (server re-simuluje sestavu),
- triviální replaye (replay = jen sestava, ne nahrávka),
- snadné snapshot testy.

> **Důsledek pro implementaci:** žádný floating point v simulaci, žádná závislost
> na pořadí iterace `Dictionary`, žádné náhodné `Guid`. Detail v
> [architecture.md → Pravidla determinismu](./architecture.md).

### Pilíř 2 — Obsah jsou data, ne kód
Uzly i levely jsou JSON. Hra je „prázdný engine", který si obsah načte za běhu.
Důsledek: modder nepotřebuje kompilátor a Workshop obsah je **bezpečný** (čistá
data, žádné spustitelné instrukce).

### Pilíř 3 — Čitelnost a klid
Vizuální minimalismus (Mini Metro). Hráč má v hlavě složitý časový model — UI mu
nesmí přidávat kognitivní zátěž. Vše plynule animované, vysoký kontrast,
barevné kódování.

### Pilíř 4 — Hádanka má víc řešení
Nikdy „jedno správné řešení". Levely jsou otevřené; žebříčky odměňují různé
styly (rychlost vs. velikost). To je palivo pro re-hratelnost a komunitu.

## 3. Herní smyčka (core loop)

```
        ┌──────────────┐     Compile      ┌───────────────┐
        │  BUILD MODE  │ ───────────────► │   SIMULACE    │
        │ stavíš linku │                  │ (na pozadí)   │
        └──────────────┘                  └───────┬───────┘
              ▲                                    │
              │ uprav a zkus znovu                 │ GridState[] / Paradox
              │                                    ▼
        ┌─────┴────────┐    scrubbing /    ┌───────────────┐
        │  výsledky /  │ ◄─────────────────│ PLAYBACK MODE │
        │  žebříček    │     replay        │ play/pause/⏩  │
        └──────────────┘                   └───────────────┘
```

Hra **neběží v reálném čase**. Build → Compile → Playback je striktně oddělené.
To je zásadní odlišení od factory her jako Factorio: hráč nestaví do živé
simulace, ale navrhuje a pak verifikuje. Umožňuje to „časové scrubbing" a
plánování smyček s výhledem na celou časovou osu.

## 4. Entity (shrnutí)

Detailní datové modely viz [architecture.md](./architecture.md), JSON definice
viz [content-format.md](./content-format.md).

| Entita | Role | Stavová? |
|--------|------|----------|
| `Generator` | Spawnuje itemy podle rozvrhu. | ne |
| `Sink` | Cíl; ověřuje přijatou sekvenci. | akumuluje |
| `Belt` | Posun o 1 buňku. | ne |
| `Splitter` | Střídá výstup L/R. | ano (přepínač) |
| `GenericMathNode` | Operace dle JSON. | dle definice |
| `PortalNode` | Posun v čase o `TimeOffset`. | ne (ale globální efekt) |

## 5. Časová mechanika (srdce hry)

Portál má `TimeOffset Δ`:
- **Δ > 0 — do minulosti** (item vstoupí v `T_in`, vyjede v `T_out = T_in − Δ`).
  Vytváří *kauzální smyčku* → vyžaduje multi-pass řešení na pevný bod.
- **Δ < 0 — do budoucnosti** (zpoždění). Triviálně kauzální, jen buffer.
- **Δ = 0** — zakázáno (degeneruje na teleport, řeší se jiným uzlem).

Z toho plynou tři druhy smyček, které jsou **záměrně** designovým prostorem:

1. **Stabilní smyčka (bootstrap paradox)** — item existuje jen proto, že byl
   poslán zpět. Sebe-konzistentní → engine ji *povolí*. Skvělé „aha" momenty.
2. **Grandfather paradox** — item poslaný zpět zabrání sám sobě vstoupit do
   portálu. Nikdy nekonverguje → `TemporalParadox`. Hráč musí návrh opravit.
3. **Oscilace** — stav přeskakuje mezi dvěma konfiguracemi. Detekováno cyklem
   v hashích stavů → `TemporalParadox`.

Detailní formální model a algoritmus v
[simulation-engine.md](./simulation-engine.md).

## 6. Navržená vylepšení oproti původnímu konceptu

Toto jsou změny/doplňky, které doporučuji zapracovat. Ke každému je důvod.
Klíčová rozhodnutí mají vlastní ADR.

### 6.1 Deterministické ID itemů místo `Guid` — ⚠️ důležité
Původní návrh: `Guid Id`. Náhodné `Guid` **rozbíjí determinismus** — dvě
kompilace téže sestavy vygenerují jiná ID, takže nelze porovnat průchody
(injection set) napříč passy ani ověřit řešení re-simulací.

**Řešení:** deterministické `ItemId` odvozené z původu — např.
`(sourceNodeId, spawnTick, sequence)` zhashované do `ulong`, nebo prostý
monotónní čítač resetovaný na začátku každé kompilace. Identita itemu napříč
časovými smyčkami pak zůstává stabilní. Viz [ADR-0003](./adr/0003-deterministic-item-ids.md).

### 6.2 Pevná sada operací místo volného výrazového jazyka
Původní `logic.operation` je dobrý, ale „univerzální výrazový evaluator" by byl
bezpečnostní a determinismus riziko (Workshop obsah by mohl způsobit nekonečné
smyčky / přetečení). Doporučuji **registr pevných operací**
(`add, sub, mul, div, mod, min, max, const, gate, compare`) s čistým rozšiřovacím
bodem v kódu. Modder skládá uzly z těchto cihel, ne píše libovolný výraz. Viz
[ADR-0004](./adr/0004-fixed-operation-set-vs-expression-dsl.md).

### 6.3 Propose/Commit model ticku
Aby byl tik deterministický bez závislosti na pořadí uzlů: každý tik má **dvě
fáze** — (1) všechny uzly *navrhnou* výstupy ze stavu `T`, (2) engine
*deterministicky vyřeší* konflikty a zapíše `T+1`. Eliminuje order-dependence a
umožňuje budoucí paralelizaci. Detail v [simulation-engine.md](./simulation-engine.md).

### 6.4 Verzování schémat (`schema_version`)
Každý JSON (node, level, save) nese `schema_version`. Bez toho se rozbije
zpětná kompatibilita modů a uložených pozic při první změně formátu. Loader umí
migrace mezi verzemi.

### 6.5 Anti-cheat zdarma z determinismu
Žebříčkový záznam = sestava (malý JSON) + level id + tvrzená metrika. Server
(nebo klient před odesláním) re-simuluje a ověří, že sestava skutečně dává
tvrzený `tick`/`footprint`. Žádné „důvěřuj klientovi". Detail v
[meta-services.md](./meta-services.md).

### 6.6 Validace obsahu při načtení („fail loud")
Loader nikdy tiše nespolkne vadný JSON. Validuje proti schématu a vrací
**konkrétní, lidsky čitelné chyby** (soubor, řádek, co chybí). Kritické pro
moddery. Headless `validate` příkaz to umí spustit nad celou složkou v CI.

### 6.7 Debug overlay „časové stopy"
Pro řešení hádanek (a ladění) overlay zobrazující dráhu konkrétního itemu napříč
tiky — barevná „kometa" trajektorie. Obrovsky pomáhá pochopit smyčky. Patří do
playbacku jako přepínatelná vrstva.

### 6.8 Lokalizace a přístupnost od začátku
- Všechny user-facing stringy externí (`/data/locale/*.json`), ne hardcoded.
  (Vývojář je Čech, hra míří na Steam → l10n nutná.)
- Barevné kódování uzlů **nesmí** být jediný nositel informace — každý uzel má i
  ikonu/tvar. Paleta přepnutelná na colorblind-safe (paleta je beztak v JSON).

### 6.9 Undo/Redo přes Command pattern
Build mode i editor potřebují undo/redo. Postavit na command patternu hned,
ne dodatečně — každá editační akce je příkaz s `Do`/`Undo`.

### 6.10 Headless CLI jako první-třídní nástroj
`EchoFactory.Cli` (validate, simulate, benchmark, verify) není „nice to have".
Je to nástroj pro CI, moddery i ověřování žebříčků a vznikne brzy (M2).

## 7. Cílová skupina a srovnatelné hry

- **Komu:** fanoušci Zachtronics (Opus Magnum, SpaceChem), Mini Metro, Baba Is
  You, factory her. Lidé, co baví optimalizace a „elegantní řešení".
- **Reference:** SpaceChem (multi-pass „reactors"), Opus Magnum (žebříčky za
  cycles/area/cost — náš tick/footprint), Mini Metro (vizuál), Braid (čas jako
  mechanika).

## 8. Co NENÍ v plánu (anti-scope)

Aby se projekt nerozutekl, explicitně mimo rozsah v1.0:
- multiplayer / online co-op,
- 3D nebo isometrie (zůstáváme 2D top-down),
- mobilní porty (architektura je nevylučuje, ale není to cíl),
- procedurálně generované levely (levely jsou ručně/komunitně tvořené),
- vyprávěná kampaň s postavami (lehký rámec ano, RPG ne).
