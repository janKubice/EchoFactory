# Testovací strategie a CI

Determinismus z této hry dělá **mimořádně testovatelný** projekt. Headless
`Core` jde celý pokrýt bez GUI a běží v CI na Linuxu během sekund.

---

## 1. Pyramida testů

```
        ┌───────────────────────────┐
        │  Manuální / smoke (Game)  │  málo — vizuál, feel, vstup
        ├───────────────────────────┤
        │  Integrace (Cli, Content) │  validace dat, round-trip, verify
        ├───────────────────────────┤
        │  Snapshot + property      │  determinismus, golden states
        ├───────────────────────────┤
        │  Unit (Core)              │  hodně — tik, uzly, paradoxy, fixed point
        └───────────────────────────┘
```

## 2. Druhy testů v `Core`

### Unit
- **Tik / propose-commit**: belt posune item; splitter střídá; math počítá;
  hranice mřížky → `VoidParadox`.
- **Uzly**: každá operace (`add…compare`) vč. hraničních (přetečení, div 0 →
  `MathParadox`).
- **Collision**: dva itemy na buňku bez merge → `CollisionParadox`.

### Snapshot (golden)
Pro known-good sestavy ulož hash `GridState[]`. Regrese se pozná okamžitě.
Aktualizace goldenů je vědomý krok (review diffu).

```csharp
[Fact] void Tutorial01_ReferenceSolution_MatchesGolden()
{
    var result = Compile(level("lvl_tutorial_01"), referenceBuild);
    Assert.Equal(LevelOutcome.Solved, result.Outcome);
    Assert.Equal(GoldenHash("lvl_tutorial_01"), HashStates(result.States));
}
```

### Property — determinismus (nejdůležitější)
```csharp
[Property] void Compile_IsDeterministic(Build b)
{
    Assert.Equal(HashStates(Compile(level, b).States),
                 HashStates(Compile(level, b).States));   // bitově shodné
}
```

### Fixed-point / temporální
- **Bootstrap** level → konverguje, `Solved`.
- **Grandfather** level → `TemporalParadox(reason = no convergence)`.
- **Oscilace** level → `TemporalParadox(reason = oscillation)` (detekce cyklem).
- Konvergence v ≤ `max_temporal_passes`.

## 3. `Content` testy
- Každý soubor v `/data/` projde validací proti schématu (žádný „shipnutý" level
  není rozbitý).
- Round-trip: `deserialize → serialize → deserialize` je stabilní.
- Migrace: starý `schema_version` se správně zmigruje.

## 4. Cross-platform determinismus
CI matice **Linux + Windows**: stejná sestava musí dát **stejný hash** na obou.
Tady se odhalí skryté nedeterminismy (pořadí kolekcí, kultura, floaty). Toto je
pojistka pro žebříčky (ověřované re-simulací na různých strojích).

## 5. `Cli` jako testovací nástroj
`EchoFactory.Cli` není jen pro hráče/moddery, ale i pro CI:

```bash
dotnet run --project src/EchoFactory.Cli -- validate ./data      # schémata
dotnet run --project src/EchoFactory.Cli -- verify  sol.json     # anti-cheat
dotnet run --project src/EchoFactory.Cli -- bench   ./data/levels # výkon
```

`validate` nad celou `/data/` je povinný CI krok (rozbitý obsah neprojde).

## 6. CI pipeline (návrh)

```
on: [push, pull_request]
jobs:
  core:        # Linux
    - dotnet build  (Core, Content, Cli)  -warnaserror
    - dotnet test   (Core.Tests, Content.Tests)
    - cli validate ./data
  determinism: # matrix: ubuntu + windows
    - dotnet test --filter Category=Determinism
  game-build:  # jen že se to zkompiluje (neběží headless)
    - dotnet build EchoFactory.Game
```

`Game` se v CI **builduje, ale nespouští** (potřebuje GPU/okno). Veškerá logika,
která by mohla být v CI, patří do `Core`/`Content` — ne do `Game`.

## 7. Co netestovat automaticky
- Vizuální feel, tweening plynulost, čitelnost — manuální smoke testy.
- Steam integrace — za `IPlatformServices`; v testech `NullPlatformServices`.
  Reálný Steam jen manuálně před release.

## 8. Definition of Done (pro každý milník)
- ✅ Nové chování pokryto unit/snapshot testy.
- ✅ `validate ./data` zelené.
- ✅ Determinismus testy zelené (vč. cross-platform u temporálních věcí).
- ✅ Žádné nové warningy (`Core` má `TreatWarningsAsErrors`).
