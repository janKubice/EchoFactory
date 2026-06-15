# Meta-služby — Steam, žebříčky, Workshop, anti-cheat

Integrace se Steamem (`EchoFactory.Steam`) je izolovaná za rozhraním
`IPlatformServices`. Hra musí jít spustit **bez Steamu** (vývoj, testy, CI) —
pak se použije `NullPlatformServices` (no-op + lokální žebříčky).

---

## 1. Abstrakce platformy

```csharp
public interface IPlatformServices
{
    bool IsAvailable { get; }
    ILeaderboardService Leaderboards { get; }
    IWorkshopService    Workshop { get; }
    IUserService        User { get; }      // id, jméno, achievementy
}
```

- `SteamPlatformServices` — Steamworks.NET.
- `NullPlatformServices` — fallback; žebříčky jen lokální (`/data/scores.json`).

`Core` o tomto rozhraní **neví** — meta-služby konzumuje jen frontend.

## 2. Žebříčky (Leaderboards)

Ke každému levelu dvě **nezávislé** tabulky (různé styly hraní):

1. **Fewest Ticks** — nejnižší `Stats.FinalTick`. Odměňuje paralelní zpracování
   a efektivní časové smyčky.
2. **Smallest Footprint** — nejnižší `Stats.Footprint` (počet uzlů). Odměňuje
   minimalistická řešení.

### Anti-cheat = determinismus (klíčová výhoda)

Protože simulace je deterministická a řešení = malá sestava, žebříček
**nedůvěřuje tvrzené metrice**. Záznam obsahuje:

```json
{
  "level_id": "lvl_tutorial_01",
  "level_hash": "sha256:…",
  "claimed": { "ticks": 14, "footprint": 6 },
  "solution": { "placed_nodes": [ /* ... */ ] }
}
```

Ověření (na klientovi před odesláním + ideálně server-side re-simulací):

```
1. Načti level podle level_id; ověř level_hash (shoda verze).
2. Re-simuluj solution.placed_nodes přes EchoFactory.Core.
3. Outcome musí být Solved.
4. Stats.FinalTick == claimed.ticks  AND  Stats.Footprint == claimed.footprint.
5. Teprve pak zapiš skóre.
```

`EchoFactory.Cli verify <solution.json>` dělá přesně tohle — použitelné i jako
serverless validační krok. Cross-platform determinismus (viz
[testing.md](./testing.md)) zaručuje, že verdikt je všude stejný.

> Pozn.: Steam leaderboard API samo o sobě „proof" neukládá; proto se solution
> blob ukládá vedle (Steam UGC / detail skóre) a periodicky se re-validuje. Plný
> server-side anti-cheat je post-launch (viz [ROADMAP.md → M8](../ROADMAP.md)).

## 3. Steam Workshop

Uživatelské **levely** (čistý JSON) a **mody uzlů** (JSON definice) se nahrávají
přímo do Workshopu. Hra integruje UI pro procházení, stahování, hodnocení.

### Bezpečnost: jen data, žádný kód

Workshop obsah je **výhradně JSON** — deklarativní data, žádné spustitelné
instrukce. Nelze jím spustit cizí kód. Zbývající rizika a obrana:

| Riziko | Obrana |
|--------|--------|
| Nevalidní/poškozený JSON | Validace proti schématu při importu; nevalidní balíček se přeskočí s chybou. |
| Konflikt `id` s core obsahem | Detekce + varování; core má prioritu nebo jasné řešení konfliktu. |
| „Zlomyslný" uzel (nekonečná smyčka, přetečení) | Pevná sada operací ([ADR-0004](./adr/0004-fixed-operation-set-vs-expression-dsl.md)) + cap tiků/passů + paradox místo pádu. |
| Nehratelný/podvodný level | Před publikací editor spustí `validate` + ověří řešitelnost (autor musí dodat referenční řešení). |

### Manifest balíčku (`pack.json`)

```json
{
  "schema_version": 1,
  "id": "pack_author_coolnodes",
  "name": "Cool Nodes Pack",
  "author": "steam:7656…",
  "version": "1.2.0",
  "content_types": ["nodes", "levels"],
  "requires_game_version": ">=0.4.0"
}
```

### Workflow

```
Editor ─► validate + ověř řešitelnost ─► upload (IWorkshopService.Publish)
Hráč ─► browse/subscribe ─► auto-download ─► validate při startu ─► registr
```

Pořadí načítání (core < workshop < lokální mody) je v
[content-format.md → §6](./content-format.md).

## 4. Achievementy (lehké, post-MVP)

Např. „Vyřeš level se stabilní časovou smyčkou", „Bootstrap paradox poprvé",
„Footprint ≤ par na 10 levelech". Definované datově, vyhodnocené z
`SimulationResult`. Nízká priorita, až po základní smyčce.

## 5. Fallback bez Steamu

- Žebříčky: lokální soubor, jen osobní nejlepší skóre.
- Workshop: vypnutý; lokální `/mods/` stále funguje.
- Žádná funkce hry (kampaň, editor) na Steamu **nezávisí** — Steam je nadstavba,
  ne podmínka.
