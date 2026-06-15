# ADR-0004 — Pevná sada operací místo volného výrazového jazyka

- **Status:** Accepted
- **Datum:** 2026-06-15

## Kontext
`GenericMathNode` je řízen JSONem (`logic.operation`). Otázka: jak daleko pustit
moddery? Dvě cesty:
1. **Pevná registrovaná sada operací** (`add, sub, mul, div, mod, min, max,
   const, gate, compare`).
2. **Obecný výrazový evaluator** (modder píše libovolný výraz/skript).

Obsah přichází i z Workshopu (nedůvěryhodný zdroj).

## Rozhodnutí
**Pevná registrovaná sada operací** s čistým rozšiřovacím bodem v C#. Modder
*skládá* uzly z těchto cihel, nepíše libovolný výraz. Nová operace = malá změna
v Core (registr), ne data.

## Proč
- **Bezpečnost:** libovolný výraz z Workshopu je vektor pro nekonečné smyčky,
  vyčerpání paměti, přetečení — útok na determinismus a stabilitu.
- **Determinismus:** uzavřená sada operací má jasně definovanou, testovatelnou
  sémantiku (vč. chování při div 0 / přetečení → `MathParadox`).
- **Jednoduchost:** žádný parser/sandbox výrazů k udržování.
- **Postačuje:** kombinací uzlů a topologie linky lze vyjádřit bohaté chování i
  bez výrazového jazyka (viz SpaceChem/Zachtronics).

## Důsledky
- ✅ Workshop obsah zůstává „jen data" → bezpečný.
- ✅ Sémantika operací plně pokrytá testy.
- ⚠️ Modder nemůže vytvořit zcela novou *operaci* bez PR do Core. Akceptováno —
  může ale skládat nové *uzly* a levely libovolně.

## Budoucí možnost
Kdyby byla poptávka, lze později přidat **úzký, bezloopový, omezený** výrazový
mini-jazyk (bez cyklů, s tvrdým stropem operací) jako opt-in. Ne pro v1.0.
