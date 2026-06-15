# Formát obsahu (Data-Driven Design)

Veškerý herní obsah je JSON. Tento dokument je referenční schéma pro **uzly**,
**levely** a **uložené pozice**. Pravidla pro moddery jsou závazná — loader je
vynucuje a validuje.

---

## 1. Principy

- **Obsah jsou data, ne kód.** JSON nikdy neobsahuje spustitelné instrukce —
  jen deklarace. To dělá Workshop obsah bezpečným (viz [meta-services.md](./meta-services.md)).
- **Fail loud.** Loader validuje proti schématu a hlásí konkrétní chyby (soubor,
  cesta v JSON, co je špatně). Žádné tiché defaulty u povinných polí.
- **Verzování.** Každý dokument nese `schema_version`. Loader umí migrace.
- **Invariant culture.** Parsování čísel vždy `InvariantCulture` (žádné `,` vs `.`).
- **Stabilní `id`.** `id` je trvalý identifikátor (pro reference, Git diff,
  žebříčky). Nepřejmenovávat — přejmenování = nový obsah.

Schémata (`/data/schema/*.schema.json`) jsou zdroj pravdy a používají se v CI
(`EchoFactory.Cli validate`).

## 2. Definice uzlu (`/data/nodes/*.json`)

Engine při startu načte složku `/data/nodes/`, naparsuje definice do **registru**
a podle `type` instancuje příslušnou C# implementaci (`GenericMathNode`,
`BeltNode`, `PortalNode`, …). Nová varianta matematického uzlu = nový JSON, žádný
kód.

```json
{
  "schema_version": 1,
  "id": "node_math_add",
  "name": "Adder",
  "type": "math",
  "category": "Math",
  "inputs": 2,
  "outputs": 1,
  "logic": {
    "operation": "add",
    "operands": ["in_1", "in_2"]
  },
  "tick_cost": 1,
  "visual": {
    "shape": "circle",
    "color_hex": "#2ECC71",
    "icon": "+"
  }
}
```

### Pole

| Pole | Typ | Povinné | Popis |
|------|-----|:---:|-------|
| `schema_version` | int | ✓ | Verze schématu uzlu. |
| `id` | string | ✓ | Globálně unikátní, stabilní (`node_*`). |
| `name` | string | ✓ | Zobrazované jméno (lokalizovatelný klíč). |
| `type` | enum | ✓ | Která C# implementace: `math`, `belt`, `splitter`, `generator`, `sink`, `portal`. |
| `category` | string | ✓ | Skupina v paletě uzlů (UI). |
| `inputs` / `outputs` | int | ✓ | Počet vstupních/výstupních portů. |
| `logic` | object | dle `type` | Konfigurace chování (viz níže). |
| `tick_cost` | int | ✓ | Kolik tiků trvá zpracování (default 1). |
| `visual` | object | ✓ | `shape`, `color_hex`, `icon` (viz [frontend.md](./frontend.md)). |

### `logic` pro `type: "math"`

`operation` je z **pevné registrované sady** (ne libovolný výraz — viz
[ADR-0004](./adr/0004-fixed-operation-set-vs-expression-dsl.md)):

| `operation` | Sémantika | Chyba |
|-------------|-----------|-------|
| `add` `sub` `mul` | aritmetika | přetečení → `MathParadox` |
| `div` `mod` | celočíselné dělení / zbytek | dělitel 0 → `MathParadox` |
| `min` `max` | extrém vstupů | — |
| `const` | konstanta (`"value": N`) | — |
| `gate` | propustí vstup jen při podmínce | — |
| `compare` | `eq/lt/gt` → 0/1 | — |

`operands` referencují porty (`in_1`, `in_2`, …) nebo konstanty.

### `logic` pro ostatní `type`

- `belt`: `{ "direction": "right" }`
- `splitter`: `{ "outputs": ["left", "right"], "start": "left" }`
- `generator`: viz level (rozvrh je vlastnost instance v levelu, ne typu)
- `portal`: `{ "time_offset": 10 }` (kladné = do minulosti)
- `sink`: cílová sekvence je vlastnost levelu, ne typu

> **Princip:** *typ* uzlu definuje chování, *instance v levelu* definuje
> konkrétní parametry (pozice, rozvrh generátoru, cílová sekvence sinku).

## 3. Definice levelu (`/data/levels/*.json`)

Jeden level = jeden JSON. Usnadňuje verzování (Git), ladění a komunitní sdílení.

```json
{
  "schema_version": 1,
  "id": "lvl_tutorial_01",
  "name": "First Steps",
  "author": "core",
  "description_key": "lvl.tutorial_01.desc",
  "grid": { "width": 8, "height": 6 },
  "max_ticks": 150,
  "max_temporal_passes": 5,
  "strict_timing": false,

  "fixed_nodes": [
    {
      "id": "gen_a",
      "type": "generator",
      "position": { "x": 0, "y": 2 },
      "schedule": [
        { "tick": 0, "value": 1 },
        { "tick": 5, "value": 2 },
        { "tick": 10, "value": 3 }
      ]
    },
    {
      "id": "sink_a",
      "type": "sink",
      "position": { "x": 7, "y": 2 },
      "expected": [
        { "value": 1 }, { "value": 2 }, { "value": 3 }
      ]
    }
  ],

  "inventory": {
    "mode": "whitelist",
    "allowed": ["node_belt", "node_math_add"],
    "limits": { "node_math_add": 3 }
  },

  "blocked_cells": [ { "x": 3, "y": 0 } ],

  "par": { "ticks": 20, "footprint": 7 }
}
```

### Pole

| Pole | Popis |
|------|-------|
| `grid` | Rozměry plochy. |
| `max_ticks` | Délka simulace (horní mez `T`). |
| `max_temporal_passes` | Cap iterací pevného bodu (viz engine doc). |
| `strict_timing` | Sink vyžaduje přesné tiky, nebo jen pořadí. |
| `fixed_nodes` | Předumístěné generátory/sinky/překážky (hráč nemůže měnit). |
| `inventory` | `whitelist`/`blacklist` dostupných uzlů + `limits` na počty. |
| `blocked_cells` | Nepoužitelné buňky. |
| `par` | Referenční hodnoty pro 3-hvězdičkové hodnocení / žebříček. |

## 4. Uložená pozice / řešení (`save` / `solution`)

Díky determinismu je řešení **jen sestava** — ne nahrávka. Re-simulace dopočítá
vše ostatní. Proto jsou save i žebříčkové „proof" záznamy malé.

```json
{
  "schema_version": 1,
  "level_id": "lvl_tutorial_01",
  "level_hash": "sha256:…",
  "placed_nodes": [
    { "node": "node_belt",     "position": { "x": 1, "y": 2 }, "direction": "right" },
    { "node": "node_math_add", "position": { "x": 4, "y": 2 }, "direction": "right" },
    { "node": "node_math_mul", "position": { "x": 5, "y": 2 }, "direction": "right", "constant": 3 },
    { "node": "node_splitter", "position": { "x": 6, "y": 2 }, "output_a": "up", "output_b": "down", "start_with_a": true },
    { "node": "node_portal",   "position": { "x": 7, "y": 2 }, "direction": "down", "time_offset": 2 }
  ]
}
```

- `node` = `id` definice z registru (`/data/nodes/`); jeho `type` určuje, jaká
  pole placement čte.
- `level_hash` = hash definice levelu, proti kterému bylo řešeno (detekce, že se
  level od té doby změnil → save se označí jako „pro jinou verzi").

Konfigurace placementu (ploché, dle `type` uzlu):

| Pole | Pro typ | Význam |
|------|---------|--------|
| `direction` | belt, math, portal | Směr pásu / výstupu uzlu / výstupu portálu. |
| `constant` | math (volitelné) | Je-li uvedeno → **unární** uzel: `op(item, constant)` (např. `mul` + `3`). |
| `output_a`, `output_b`, `start_with_a` | splitter | Dva výstupní směry a počáteční strana. |
| `time_offset` | portal | Δ tiků: `>0` do minulosti (smyčka), `<0` do budoucnosti. |

## 5. Lokalizace (`/data/locale/*.json`)

```json
{ "schema_version": 1, "locale": "cs", "strings": {
  "menu.play": "Hrát",
  "lvl.tutorial_01.desc": "Doprav čísla 1, 2, 3 do cíle."
}}
```

User-facing texty se referencují klíčem (`description_key`, `name` přes klíč),
nikdy se nepíšou natvrdo v kódu ani v levelech. Fallback locale: `en`.

## 6. Načítání, priorita a modding

Pořadí načítání (pozdější přepisuje dřívější se stejným `id`):

```
1. /data/                     (core obsah, dodaný se hrou)
2. Steam Workshop subscribed   (stažené balíčky)
3. /mods/  (lokální)           (vývoj modů)
```

- Konflikt `id` → **varování** v logu + použije se poslední (s jasnou hláškou).
- **Validace při načtení**: každý balíček projde schématem. Nevalidní balíček se
  *přeskočí s chybou*, nezhroutí hru.
- Workshop balíček má manifest (`pack.json`) s `id`, `name`, `version`,
  `content_types` (jaké levely/uzly přidává). Detail v
  [meta-services.md](./meta-services.md).

## 7. Verzování a migrace

- Změna formátu = inkrement `schema_version` + migrační funkce
  `Migrate(vN → vN+1)` v `Content`.
- Loader migruje staré dokumenty za běhu (in-memory), originál nemění.
- Pravidlo: **nikdy neměnit význam existujícího pole** — jen přidávat nová
  (volitelná) nebo zavést novou verzi s migrací. Chrání to staré savy i mody.
