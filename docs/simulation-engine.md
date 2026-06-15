# Simulační engine — multi-pass kompilátor

Srdce hry. Tento dokument je formální a implementační. Předpokládá znalost
[architecture.md](./architecture.md) (zvlášť pravidel determinismu).

---

## 1. Přehled

Simulace **neběží v reálném čase**. „Compile" je čistá funkce:

```
Compile : (LevelDefinition, Build) ─► SimulationResult
```

Výstupem je pole `GridState[0 .. MaxTicks]` (kompletní film) nebo `ParadoxError`.
Bez časových portálů jde o prostou dopřednou simulaci. S portály jde o hledání
**pevného bodu** (fixed point) iterací více průchodů.

## 2. Jeden tik: model Propose / Commit

Tik je čistá funkce `Step(stateₜ) → stateₜ₊₁`. Aby byl deterministický bez
závislosti na pořadí uzlů, probíhá ve dvou fázích:

```
Fáze A — PROPOSE (paralelizovatelná, read-only nad stateₜ):
    pro každý uzel n (v libovolném pořadí):
        n.Evaluate(ctx, stateₜ, builder)   // zapisuje NÁVRHY do builderu

Fáze B — COMMIT (sériová, deterministická):
    seřaď všechny návrhy podle (cílová buňka, priorita uzlu, ItemId)
    pro každou cílovou buňku:
        pokud 0 návrhů → buňka prázdná
        pokud 1 návrh  → ulož item
        pokud >1 návrh:
            pokud buňka/uzel umožňuje MERGE → slož dle pravidla
            jinak → CollisionParadox(tick, cell, items)
```

Klíčové vlastnosti:
- **Pořadí uzlů ve fázi A nehraje roli** — všichni čtou stejný `stateₜ`.
- **Veškerý nedeterminismus je izolován do fáze B**, kde je *jediný* seřazený,
  deterministický slučovací algoritmus.
- Fáze A je čistě read-only → triviálně paralelizovatelná, až to bude potřeba
  (zatím neoptimalizovat).

### Sémantika pohybu (belt / splitter / math)

- **Belt**: navrhne přesun itemu z `pos` na `pos + dir`.
- **Splitter**: dle vnitřního přepínače navrhne L nebo R výstup; přepínač
  překlopí. (Stav splitteru je součástí stavu uzlu, ne `GridState` — resetuje se
  na začátku každého passu.)
- **GenericMathNode**: čte itemy na vstupních buňkách, aplikuje operaci, navrhne
  item s výsledkem na výstupní buňku. Dělení nulou / přetečení → `MathParadox`.
- **Generator**: pokud `tick ∈ schedule`, navrhne nový item s deterministickým
  `ItemId`.
- **Sink**: konzumuje item na své buňce, zapisuje do své přijaté sekvence.

### Hranice mřížky

Item navržený mimo mřížku bez navazujícího pásu/uzlu → `VoidParadox`. (Pás
mířící „ven" je chyba návrhu, ne tiché zmizení.)

## 3. Časové portály a pevný bod

### 3.1 Intuice

Portál s `TimeOffset Δ > 0` vezme item, který do něj vstoupí v `T_in`, a
**naplánuje jeho emisi** na výstupu v `T_out = T_in − Δ` (v minulosti). Tím
vzniká kauzální smyčka:

```
   item ovlivní minulost ──► minulost ovlivní, co vjede do portálu ──┐
        ▲                                                            │
        └──────────────── ... což ovlivní minulost ──────────────────┘
```

Hledáme **sebe-konzistentní** časovou osu: takovou, kde to, co portály *emitují*,
přesně odpovídá tomu, co je v té ose donutí emitovat.

### 3.2 Formálně

Nechť **I** je *injection set* — multimnožina záznamů
`(exitPos, T_out, Item)`, tj. „co a kdy mají portály vyplivnout".

Jeden plný průchod simulace je funkce:

```
F : I  ─►  I'
```

kde `I` jsou *vstřiky předpokládané na začátku průchodu* a `I'` jsou *vstřiky
zaznamenané portály během průchodu* (tj. co by se mělo vstříknout, aby to bylo
konzistentní). Hledáme **pevný bod** `I*` takový, že `F(I*) = I*`.

Iterace:

```
I₀ = ∅                       // Pass 0: žádné vstřiky
I₁ = F(I₀)                   // Pass 1: portály něco zaznamenají
I₂ = F(I₁)
...
zastav, když  Iₙ = Iₙ₋₁      // KONVERGENCE → stabilní osa
```

### 3.3 Algoritmus kompilace

```
function Compile(level, build):
    nodes        = instantiate(build, level)        // deterministicky seřazené
    injections   = ∅
    seenHashes   = {}                               // pro detekci oscilace
    MAX_PASSES   = level.MaxTemporalPasses ?? 5

    for pass in 0 .. MAX_PASSES:
        states, recorded = RunSinglePass(level, nodes, injections)
        if recorded.AnyParadox:
            return Paradox(recorded.firstParadox)   // collision/math/void

        newInjections = recorded.injections
        if newInjections == injections:             // multiset rovnost
            return Success(states, evaluateOutcome(states, level))

        h = hash(newInjections)
        if h in seenHashes:                         // cyklus = oscilace
            return Paradox(TemporalParadox(reason = "oscillation", pass))
        seenHashes.add(h)

        injections = newInjections                  // další iterace

    return Paradox(TemporalParadox(reason = "no convergence", MAX_PASSES))


function RunSinglePass(level, nodes, injections):
    reset(nodes)                                    // stav splitterů atd.
    states[0] = initialState(level)
    recorded  = new RecordedPass()
    for T in 0 .. level.MaxTicks - 1:
        builder = new GridStateBuilder(states[T])
        // 1) vstřikni naplánované portálové emise pro tento čas
        for inj in injections where inj.T_out == T:
            builder.Inject(inj.exitPos, inj.item)
        // 2) PROPOSE: všechny uzly
        for node in nodes:                          // pořadí nerozhoduje
            node.Evaluate(ctx(T, recorded), states[T], builder)
        // 3) COMMIT: deterministicky vyřeš a vytvoř states[T+1]
        states[T+1] = builder.Commit(out paradox)
        if paradox != null:
            recorded.firstParadox = paradox; recorded.AnyParadox = true; break
    return (states, recorded)
```

> Portál ve fázi PROPOSE *nevyrábí* výstup. Místo toho zaznamená do `recorded`
> záznam `(exitPos, T_in − Δ, item)`. Tyto záznamy tvoří `newInjections` pro
> další pass. Item, který portál pohltil, z aktuálního toku **zmizí** (je
> „přenesen" jinam v čase).

### 3.4 Důležité jemnosti

- **Multiset rovnost a hash** musí být deterministické → proto deterministické
  `ItemId` ([ADR-0003](./adr/0003-deterministic-item-ids.md)). Bez něj nelze dva
  passy porovnat.
- **Emise do minulosti** (`T_out < pass start`) v jednom passu nelze „doběhnout"
  zpět — proto se promítne až do *dalšího* passu. To je podstata iterace.
- **Emise do budoucnosti** (`Δ < 0`, `T_out > T_in`) je kauzální v rámci jednoho
  passu (buffer/zpoždění) a obvykle konverguje hned v Pass 1.
- **Konvergence není zaručena** obecně — proto cap `MAX_PASSES` a detekce cyklu.

## 4. Typologie smyček (důsledky modelu)

| Typ | Chování `F` | Výsledek |
|-----|-------------|----------|
| Žádný backward portál | `I` zůstane ∅ nebo se ustálí v Pass 1 | Success |
| **Bootstrap** (sebe-konzistentní) | `F(I*) = I*`, `I* ≠ ∅` | Success — povoleno |
| **Grandfather** | `F` nikdy nedosáhne pevného bodu | `TemporalParadox` |
| **Oscilace** | `F` cykluje mezi stavy | `TemporalParadox` (detekce cyklem) |

Bootstrap paradox (item existuje jen díky tomu, že byl poslán zpět) je **validní
a žádoucí** herní mechanika — sebe-konzistentní smyčka. Engine ji nepovažuje za
chybu.

## 5. Paradoxy (chybové stavy)

Všechny dědí z `ParadoxError` a nesou kontext pro UI i ladění.

| Paradox | Spouštěč | Klíčový kontext |
|---------|----------|-----------------|
| `CollisionParadox` | >1 item na buňce v tiku (bez merge) | `Tick`, `Cell`, `Item[]` |
| `MathParadox` | dělení nulou, přetečení `int` | `Tick`, `NodeId`, operace, operandy |
| `TemporalParadox` | nekonvergence / oscilace | `reason`, `passes`, příp. divergující buňky |
| `VoidParadox` | item opustil platný systém | `Tick`, `Cell`, `ItemId` |

Návrhový princip: paradox **není pád enginu**, je to *normální výsledek*
simulace (`SimulationResult.Outcome = Paradox`). Hra ho zobrazí jako nápovědu
„kde a proč" — ideálně zvýrazní problémovou buňku/tik v playbacku.

## 6. Vyhodnocení úspěchu levelu

Po úspěšné kompilaci (žádný paradox) se ověří cíle:

- Každý `Sink` musí přijmout **požadovanou sekvenci** itemů ve **správném pořadí**
  (a volitelně ve správných ticích, dle definice levelu).
- `Outcome = Solved`, pokud všechny sinky splněny; jinak `Failed` (ne paradox —
  jen nesplněný cíl).
- `Stats.FinalTick` = poslední tik s aktivitou (metrika žebříčku „Fewest Ticks").
- `Stats.Footprint` = počet uzlů v sestavě (metrika „Smallest Footprint").

## 7. Výkon a paměť

- Naivně: `GridState[MaxTicks]` × `MAX_PASSES`. Pro `MaxTicks = 150` a malé
  mřížky zanedbatelné.
- **Neoptimalizovat předčasně.** Až bude potřeba (velké levely), možnosti:
  - ukládat jen *delty* mezi tiky místo plných snímků,
  - oddělit „vizuální" snímky (pro render) od „logických" (pro výpočet),
  - reuse bufferů mezi passy (states pole se přepisuje).
- Měřit, ne hádat: `EchoFactory.Cli benchmark` (M2) změří kompilaci sad levelů.

## 8. Testovatelnost (proč je tento design dobrý)

Protože `Compile` je čistá deterministická funkce:
- **Snapshot testy**: ulož hash `GridState[]` pro known-good sestavy; regrese se
  pozná okamžitě.
- **Property test determinismu**: `Compile(x) == Compile(x)` bitově.
- **Paradox testy**: ručně sestavené minimální reprodukce každého paradoxu.
- **Fixed-point testy**: bootstrap level musí konvergovat; grandfather level musí
  vrátit `TemporalParadox`.

Detaily v [testing.md](./testing.md).

## 9. Otevřené otázky k doladění během M3

- Mají sinky vyžadovat *přesné tiky*, nebo jen *pořadí*? (Návrh: konfigurovatelné
  per-level flagem `strict_timing`.)
- Priorita uzlů ve fázi COMMIT při merge — pevné pořadí dle `NodeId`, nebo
  explicitní `priority` v JSON? (Návrh: explicitní `priority`, fallback `NodeId`.)
- Limit počtu portálů na level (kvůli kombinatorické explozi passů)? (Návrh:
  měkký limit + varování v editoru.)
