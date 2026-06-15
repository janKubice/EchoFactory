# Slovník pojmů (Glossary)

Sdílená terminologie pro celý projekt. **Pravidlo:** v kódu, JSON klíčích a
názvech souborů používáme výhradně **anglické** termíny ze sloupce „EN /
kód". České termíny slouží jen pro komunikaci v dokumentaci a designu.

| CZ | EN / kód | Význam |
|----|----------|--------|
| Tik | `Tick` (`int`) | Diskrétní jednotka času. Level běží `T = 0 .. MaxTicks`. |
| Mřížka | `Grid` | 2D plocha buněk, na které se staví linka. |
| Buňka | `Cell` | Jedno políčko mřížky na souřadnici `(X, Y)`. |
| Stav mřížky | `GridState` | Kompletní snímek mřížky v jednom tiku. |
| Item / Náklad | `Item` | Pohybující se číselná hodnota (`Value`). |
| Uzel / Stroj | `Node` | Statická entita na mřížce, která zpracovává itemy. |
| Generátor | `Generator` | Uzel spawnující itemy v daných ticích. |
| Cíl | `Sink` (a.k.a. `Target`) | Uzel přijímající požadovanou sekvenci itemů. |
| Pás | `Belt` | Posouvá item o 1 buňku ve směru vektoru. |
| Rozdělovač | `Splitter` | Střídavě rozesílá itemy do dvou výstupů (stavový). |
| Matematický uzel | `GenericMathNode` | Univerzální uzel řízený JSON definicí. |
| Časový portál | `PortalNode` | Posílá item v čase o `TimeOffset`. |
| Sestava / Linka | `Build` | Hráčem umístěné uzly = řešení levelu. |
| Kompilace | `Compile` | Předpočítání celé simulace do `GridState[]`. |
| Průchod | `Pass` | Jeden kompletní běh simulace `T=0..MaxTicks`. |
| Vstřik / Injekce | `Injection` | Vložení itemu z portálu do minulého tiku. |
| Pevný bod | `Fixed point` | Stabilní stav, kdy `Pass(n) == Pass(n-1)`. |
| Paradox | `Paradox` | Chybový stav simulace (viz níže). |
| Stopa | `Footprint` | Počet použitých uzlů (metrika pro žebříček). |
| Přehrávání | `Playback` | Vizuální reprodukce předpočítaných stavů. |
| Tweening | `Tweening` | Plynulá interpolace pozice itemu mezi tiky při renderu. |

## Typy paradoxů

| CZ | EN / kód | Spouštěč |
|----|----------|----------|
| Kolizní paradox | `CollisionParadox` | Dva itemy na stejné buňce ve stejném tiku. |
| Matematický paradox | `MathParadox` | Dělení nulou, přetečení `int`. |
| Časový paradox | `TemporalParadox` | Nestabilní smyčka — nekonverguje za N iterací. |
| Prázdnotný paradox | `VoidParadox` | Item opustil platnou mřížku / systém pásů. |

## Klíčové zkratky

- **POCO** — Plain Old CLR Object (datová třída bez závislostí na frameworku).
- **DOD** — Data-Oriented Design.
- **ADR** — Architecture Decision Record.
- **USP** — Unique Selling Point (časové smyčky).
- **MVP** — Minimum Viable Product.
