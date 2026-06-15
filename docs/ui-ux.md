# UI / UX — kompletní herní „shell"

EchoFactory není jen engine — je to **plnohodnotná moderní hra**: hlavní menu,
nastavení, ukládání postupu, pauza, HUD, onboarding, oznámení, přístupnost.
Tento dokument popisuje celou prezentační a UX vrstvu. Vizuální styl a technické
základy frontendu jsou v [frontend.md](./frontend.md); tady jde o *obrazovky,
menu, nastavení a tok hráče*.

> Tato vrstva se staví postupně napříč M4–M8 (viz [ROADMAP.md](../ROADMAP.md)) —
> ale je to **první-třídní požadavek**, ne „až zbyde čas".

---

## 1. Principy UX

1. **Klid a čitelnost** (Mini Metro) — UI nesoutěží s hádankou o pozornost.
2. **Nikdy slepá ulička** — z každé obrazovky je jasná cesta zpět/dál; ESC vždy
   funguje konzistentně.
3. **Okamžitá zpětná vazba** — každá akce má vizuální + zvukovou odezvu.
4. **Žádný hardcoded text** — vše přes lokalizační klíče ([content-format.md §5](./content-format.md)).
5. **Přístupnost není dodatek** — barva nikdy jediný nositel informace, vše
   škálovatelné a remapovatelné (viz §9).
6. **Myš i klávesnice rovnocenně**, gamepad jako cíl po 1.0.

## 2. Mapa obrazovek (scény + overlaye)

```
            ┌─────────────┐
            │  BootScene  │  splash, init, loading
            └──────┬──────┘
                   ▼
          ┌──────────────────┐
          │   MainMenuScene  │──► SettingsScene ──► (overlay kdekoli)
          │                  │──► CreditsScene
          │                  │──► ProfileScene (statistiky, postup)
          └───┬───────┬──────┘
              │       │
              ▼       ▼
     LevelSelectScene  WorkshopScene
              │
              ▼
       ┌──────────────┐    ESC    ┌──────────────┐
       │ GameplayScene│ ────────► │ PauseOverlay │
       │  HUD + hra   │ ◄──────── │              │
       └──────┬───────┘           └──────────────┘
              │ (z menu/level selectu)
              ▼
       LevelEditorScene
```

Scény jsou stavy ([frontend.md §2](./frontend.md)); **overlaye** (Pause, Settings,
dialogy, toasty) běží *nad* aktivní scénou ve stacku, takže nastavení jdou otevřít
i uprostřed hry bez ztráty kontextu.

## 3. Hlavní menu (`MainMenuScene`)

- **Pokračovat** (Continue) — skok na poslední rozehraný/další level.
- **Kampaň / Hrát** → `LevelSelectScene`.
- **Editor levelů** → `LevelEditorScene`.
- **Workshop** → `WorkshopScene` (procházení/stahování komunitních balíčků).
- **Nastavení** → `SettingsScene` (overlay).
- **Profil / Statistiky** → `ProfileScene`.
- **Titulky** → `CreditsScene`.
- **Konec**.

Doplňky moderní hry: animované pozadí (jemný „běžící" demo-grid), verze buildu v
rohu, indikátor přihlášení ke Steamu, notifikace „nový obsah ve Workshopu".

## 4. Nastavení (`SettingsScene`)

Záložkové, okamžitě aplikované, s „Obnovit výchozí" a potvrzením u rizikových
změn (rozlišení). Persistuje se do `settings.json` (viz §7).

| Záložka | Položky |
|---------|---------|
| **Obraz** | Rozlišení, režim okna (fullscreen / borderless / windowed), V-Sync, limit FPS, **UI scale**, jas/kontrast. |
| **Zvuk** | Master / Hudba / SFX / UI hlasitost, mute při ztrátě fokusu. |
| **Ovládání** | **Remapování kláves** (vše), citlivost, invert tažení timeline, myš vs. trackpad. |
| **Hra** | Výchozí rychlost přehrávání, auto-compile on/off, potvrzovací dialogy, mřížka on/off, tooltipy. |
| **Jazyk** | Výběr locale (cs/en/…), fallback `en`. |
| **Přístupnost** | Colorblind paleta (Deuteranopia/Protanopia/Tritanopia), velikost písma, **reduced motion**, high-contrast, hold↔toggle, dyslexia-friendly font. |

## 5. Herní HUD (`GameplayScene`)

HUD má dva režimy (Build / Playback) sdílející rámec. Vše s tooltipy a klávesovými
zkratkami.

### Build mode
- **Paleta uzlů** — dlaždice z registru, filtrované `inventory` levelu; zašedlé/se
  zámkem, když limit vyčerpán nebo uzel zakázán.
- **Počítadlo inventáře** — kolik z čeho zbývá (limity z levelu).
- **Footprint counter** — aktuální počet uzlů (žebříček „Smallest Footprint").
- **Undo / Redo** — viditelná tlačítka + `Ctrl+Z/Y` (command pattern, [design.md §6.9](./design.md)).
- **Compile** — velké primární tlačítko; běží na pozadí s progressem.
- **Info o levelu** — cíl (požadovaná sekvence sinku), `par` hodnoty (3★).

### Playback mode
- **Transport**: Play / Pause / Step / Reset.
- **Timeline scrubber** — tažení po tikoch, skok na T; značky událostí (spawn,
  delivery, paradox).
- **Tick counter** — aktuální / max (žebříček „Fewest Ticks").
- **Rychlost** — 0.5× / 1× / 2× / 4×.
- **Debug overlay „časové stopy"** — přepínatelná trajektorie itemu ([design.md §6.7](./design.md)).
- **Panel paradoxu** — když nastane: druh, tik, zvýraznění buňky, lidská hláška +
  „skok na problém".

### Výsledková karta (po splnění)
Hvězdičky (dle `par`), dosažené `ticks`/`footprint`, porovnání s osobním rekordem
a žebříčkem, tlačítka „Další level" / „Zkusit optimalizovat" / „Sdílet řešení".

## 6. Pauza (`PauseOverlay`)
ESC kdykoli ve hře: **Pokračovat**, Restartovat level, Nastavení, Jak hrát
(nápověda), Zpět do výběru levelů, Do hlavního menu. Hra je beztak předpočítaná,
takže „pauza" je triviální (nezastavuje se žádná živá simulace).

## 7. Ukládání a profily

Díky determinismu je **řešení jen sestava** ([content-format.md §4](./content-format.md)) —
savy jsou malé a re-simulací se dopočítá zbytek.

- **Profil hráče**: postup kampaní, hvězdičky, odemčené levely, osobní rekordy
  (`ticks`/`footprint`) per level, statistiky.
- **Uložená řešení**: per level (i více variant — „rychlé" vs „malé").
- **Auto-save**: po každém úspěšném compile/řešení; ruční sloty volitelné.
- **Nastavení**: `settings.json` (mimo herní savy).
- **Steam Cloud**: profil + savy + nastavení synchronizované ([meta-services.md](./meta-services.md)).
- **Verzování & migrace**: každý save nese `schema_version` + `level_hash`
  (detekce „save pro jinou verzi levelu"). Migrace stejně jako u obsahu.

## 8. Onboarding a nápověda

Časové smyčky jsou nejtěžší koncept — UX je musí učit postupně.

- **Tutoriálové levely** zavádějící mechaniky jednu po druhé (pás → math →
  splitter → portál).
- **Kontextové nápovědy** (dismissable), tooltipy na všem.
- **„Jak hrát" / Codex** — prohlížitelná encyklopedie uzlů (generovaná z JSON
  definic: tvar, barva, popis, příklad).
- **Vizualizace smyček** — debug overlay + animace „nasátí/vyplivnutí" portálu.
- **Nevnucované** — zkušený hráč může onboarding přeskočit.

## 9. Zpětná vazba a „šťáva"

- **Toast notifikace** (roh): level splněn, nový rekord/žebříčkové umístění,
  stažen Workshop balíček, odemčen achievement.
- **Přechody scén** — plynulé fade/slide, ne tvrdé cuty.
- **Mikroanimace** — hover, klik, umístění uzlu, doručení itemu.
- **Zvukové signály** — jemné tóny (spawn/merge/math), výrazný na splnění,
  disonance na paradox ([frontend.md §7](./frontend.md)).
- **Potvrzovací dialogy** u nevratných akcí (smazat řešení/level, opustit editor
  bez uložení).

## 10. Vstup a platformy

- **Primárně myš + klávesnice**: levý klik umístit, pravý smazat, kolečko rotace,
  `Ctrl+Z/Y`, mezerník play/pause, šipky step, tažení timeline.
- **Plné remapování** kláves (Nastavení → Ovládání).
- **Gamepad** — cíl po 1.0 (UI navržené tak, aby šlo ovládat i fokusem/D-padem).
- **Responzivní layout** — UI scale + kotvení prvků pro různá rozlišení.

## 11. UI toolkit

MonoGame nemá UI framework. Volíme **vlastní lehký retained/immediate UI** ladící
s vektorovým stylem (tlačítka, panely, slidery, taby, dialogy, toasty, fokus
systém). Pro **vývojářské/debug nástroje** je povolen ImGui.NET (mimo finální
herní UI). Rozhodnutí a alternativy: [ADR-0005](./adr/0005-ui-toolkit.md).

## 12. Přístupnost (souhrn)

První-třídní, ne dodatek: colorblind palety (paleta je v JSON → snadné),
škálování písma a UI, reduced motion, high-contrast, plné remapování, hold↔toggle,
dyslexia-friendly font, čitelné kontrasty. Detail rozesetý v §4 a [frontend.md §5](./frontend.md).
