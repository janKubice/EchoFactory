# ADR-0005 — Vlastní lehký UI toolkit (ImGui jen pro dev nástroje)

- **Status:** Accepted (revidovatelné — týká se jen frontendu, ne Core)
- **Datum:** 2026-06-15

## Kontext
EchoFactory má být plnohodnotná moderní hra: hlavní menu, nastavení, HUD, pauza,
dialogy, toasty, Workshop UI ([ui-ux.md](../ui-ux.md)). MonoGame ale žádný UI
framework nemá. Potřebujeme přístup, který ladí s čistým vektorovým stylem
(Mini Metro) a zvládne plnou škálu obrazovek.

Kandidáti:
1. **Vlastní lehký UI** (retained/immediate) postavený nad `IRenderer`.
2. **Hotová knihovna** (Myra, GeonBit.UI) — retained UI pro MonoGame.
3. **ImGui.NET** — výborné pro nástroje, ale „dev" vzhled nevhodný pro hru.

## Rozhodnutí
**Vlastní lehký UI toolkit** pro herní UI (tlačítka, panely, slidery, taby,
dialogy, toasty, fokus/navigace). **ImGui.NET** povolen **jen pro vývojářské a
debug nástroje** (editor internals, profilery), nikdy ne ve finálním herním UI.

## Proč
- **Vizuální jednota** — UI je součást estetiky hry; hotové knihovny tlačí vlastní
  look, který by se s vektorovým minimalismem pral.
- **Lehkost a kontrola** — potřebujeme jen malou sadu widgetů; plný UI framework
  je víc závislostí a stylovacího boje než užitku.
- **Konzistence se stylem** — vše kreslíme přes `IRenderer` (linky, kruhy, text),
  takže UI a herní plocha sdílejí jeden renderer a paletu.
- **Gamepad/fokus** — vlastní fokus systém připraví ovládání bez myši (cíl po 1.0).

## Důsledky
- ✅ UI dokonale ladí s herním stylem a paletou (vč. colorblind variant z JSON).
- ✅ Minimální závislosti; renderer-agnostické (drží ADR-0002 dveře otevřené).
- ⚠️ Musíme napsat základní widgety sami (jednorázová investice v M4).
- ⚠️ ImGui smí do buildu jen pod dev-flagem, aby neunikl do release.

## Alternativy
- *Myra / GeonBit.UI* — rychlejší start, ale stylová nekonzistence a závislost.
- *Jen ImGui* — zamítnuto pro herní UI (vzhled), ano pro nástroje.
