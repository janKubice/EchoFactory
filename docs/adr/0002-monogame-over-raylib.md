# ADR-0002 — MonoGame jako frontend (za rendering abstrakcí)

- **Status:** Accepted (revidovatelné — Core na tom nezávisí)
- **Datum:** 2026-06-15

## Kontext
Potřebujeme hardwarově akcelerované 2D vykreslování (vektorový styl), vstup a
zvuk. Bez „heavyweight" enginu (Unity/Godot) kvůli kontrole a determinismu.
Kandidáti: **MonoGame** vs **Raylib-cs**.

## Rozhodnutí
**MonoGame**, ale veškerý rendering schovaný za `IRenderer`, aby šel engine
vyměnit s minimem práce.

## Proč
| | MonoGame | Raylib-cs |
|---|---|---|
| Zralost / track record | Stardew Valley, Celeste (shippnuté na Steamu) | mladší binding |
| Komunita / zdroje | velká | menší |
| Content pipeline | ano | jednodušší/manuální |
| Steam patterny | hojně zdokumentované | méně |
| Jednoduchost API | vyšší vstupní práh | velmi nízký |

Pro Steam hru s Workshopem a delší životností převažuje zralost a ekosystém
MonoGame. Raylib by byl rychlejší na prototyp, ale méně prověřený pro release.

## Důsledky
- ✅ Stabilní základ pro shippnutí na Steam.
- ✅ Abstrakce `IRenderer` drží dveře otevřené (i pro pozdější prototyp v Raylibu).
- ⚠️ Strmější začátek než Raylib — akceptováno.

## Pozn.
Rozhodnutí je **levně revidovatelné**, protože `Core` (a tím 90 % hodnoty
projektu) na volbě enginu nezávisí.
