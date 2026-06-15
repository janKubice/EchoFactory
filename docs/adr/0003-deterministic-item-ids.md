# ADR-0003 — Deterministické ID itemů místo `Guid`

- **Status:** Accepted
- **Datum:** 2026-06-15

## Kontext
Původní návrh měl `Item.Id` jako `Guid`. Časové smyčky se řeší porovnáváním
*injection setů* mezi průchody (passy) — hledáním pevného bodu `F(I*) = I*`.
Žebříčky se ověřují re-simulací téže sestavy. Obojí vyžaduje, aby **identita
itemu byla napříč běhy stabilní**.

`Guid.NewGuid()` je náhodné → každá kompilace dá jiná ID → dva passy nelze
porovnat (nikdy „rovnost") a re-simulace nedá shodný výsledek. To **rozbíjí
jádro hry**.

## Rozhodnutí
Zavést `ItemId` (a obdobně `NodeId`) jako **deterministicky odvozenou** identitu:

```
ItemId = hash(sourceNodeId, spawnTick, sequenceWithinTick)
```

Alternativně monotónní čítač resetovaný na začátku každé kompilace (deterministicky
v pevném pořadí spawnů). Žádné `Guid` v simulaci.

## Důsledky
- ✅ Injection sety lze porovnávat a hashovat → pevný bod funguje.
- ✅ Re-simulace dá bitově shodný výsledek → ověřitelné žebříčky a snapshot testy.
- ✅ Identita itemu je čitelná i pro ladění (víš, odkud item pochází).
- ⚠️ Pozor na kolize hashů — použít dostatečně široký typ (`ulong`) a dobře
  zvolené komponenty; v testech ověřit unikátnost v rámci levelu.

## Alternativy
- *Guid* — zamítnuto (viz výše).
- *Náhodné, ale seedované* — funkční, ale provenance-based ID je čitelnější a
  stejně deterministické.
