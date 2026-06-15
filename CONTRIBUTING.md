# Přispívání / konvence

Než přidáš kód, přečti [docs/architecture.md](./docs/architecture.md) —
zvlášť **Pravidla determinismu**. Jejich porušení je bug priority P0.

## Jazyk
- **Kód, identifikátory, JSON klíče, názvy souborů, commit messages → anglicky.**
- **Designová dokumentace a komunikace → česky** (technické termíny anglicky, viz
  [glossary](./docs/glossary.md)).
- **User-facing texty → nikdy natvrdo**; vždy přes lokalizační klíč
  (`/data/locale`).

## Git workflow
- Vývoj na feature větvích, slučování přes PR.
- Commit messages: imperativ, stručně a věcně (`Add belt propose/commit step`).
  Žádné odkazy na nástroje/modely v commitu.
- Nepushovat do hlavní větve bez review.

## Kódové konvence (C#)
- `.NET 8+`, `nullable enable`. `Core`: `TreatWarningsAsErrors`.
- `Core` **nesmí** referencovat MonoGame / Steamworks / I/O / grafiku.
- Preferuj `struct` pro malé hodnotové typy (`GridPoint`, `Item`, `ItemId`),
  `class` pro entity se životním cyklem.
- Žádný floating point v `Core`. Žádné `Guid` v simulaci (viz
  [ADR-0003](./docs/adr/0003-deterministic-item-ids.md)).
- Iterace kolekcí jen přes deterministické seřazení (helper `DeterministicOrder`).
- `System.Text.Json` source-generated; parsování `InvariantCulture`.

## Definition of Done
Viz [testing.md → §8](./docs/testing.md). Stručně: testy zelené, `validate ./data`
zelené, determinismus testy zelené, žádné nové warningy.

## Rozhodnutí (ADR)
Architektonicky významná rozhodnutí se zapisují jako ADR do
[docs/adr/](./docs/adr/). Formát: kontext → rozhodnutí → důsledky → alternativy.

## Přidávání obsahu
- Nový **uzel** = JSON v `/data/nodes/` (pokud používá existující `type`), jinak +
  C# implementace `type` v `Core`.
- Nový **level** = JSON v `/data/levels/` + referenční řešení pro CI/anti-cheat.
- Vždy spustit `EchoFactory.Cli validate ./data` před commitem.
