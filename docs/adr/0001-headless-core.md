# ADR-0001 — Headless deterministický Core bez závislostí

- **Status:** Accepted
- **Datum:** 2026-06-15

## Kontext
Hra stojí na časových smyčkách a ověřitelných žebříčcích. Obojí vyžaduje, aby
simulace byla 100% deterministická a šla spustit kdekoli — v testech, v CI, na
serveru pro anti-cheat — bez grafiky a bez Steamu.

## Rozhodnutí
Veškerá herní logika žije v `EchoFactory.Core` — čisté C#, **žádné závislosti**
na MonoGame, Steamworks, I/O ani grafice. Prezentace (`Game`) a integrace
(`Steam`) na Core závisí, nikdy naopak.

## Důsledky
- ✅ Core je plně unit-testovatelný, běží v CI na Linuxu za sekundy.
- ✅ Stejný engine kompiluje hru, ověřuje žebříčky i validuje levely (jeden zdroj
  pravdy).
- ✅ Frontend lze vyměnit (MonoGame → cokoli) bez dotyku logiky.
- ⚠️ Vyžaduje disciplínu: žádné `using Microsoft.Xna…` v Core. Hlídá CI a code
  review.

## Alternativy
- *Logika ve frontendu (jako typická MonoGame hra)* — zamítnuto: netestovatelné,
  nedeterministické, nešlo by ověřovat žebříčky re-simulací.
