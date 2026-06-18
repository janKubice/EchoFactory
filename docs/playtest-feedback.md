# Playtest feedback & backlog

Strukturovaný feedback z hraní (poslední velký playtest: **2026-06-16**) + odvozený
backlog. Toto je **živý seznam úkolů** pro frontend/UX a design. Inženýrský backlog
(engine) je v [ROADMAP.md](../ROADMAP.md); handoff pro pokračování v
[handoff.md](./handoff.md).

> Pozn.: hra se v cloudovém (headless) prostředí **nedá vizuálně spustit**, takže
> tyto věci odhalil hráč lokálně. Frontend změny se tu jen kompilují — vždy
> doplnit o reálný playtest.

**Stav:** 🔴 todo · 🟡 existuje, ale špatné/neúplné · 🟢 hotovo

---

## A. Hlavní menu
- 🔴 **Animované pozadí menu** — pod menu nechat běžet „nesmyslnou" nekonečnou
  továrnu: žádné zdi, jen podlaha, a po ní donekonečna jezdí generátory, cíle,
  pásy, matematické operace, itemy. Čistě dekorativní ambient (může běžet vlastní
  jednoduchá simulace/skript). Velký „cool" efekt.

## B. Build / distribuce
- 🔴 **EXE build** — chce výsledek jako spustitelné `.exe`. (Doplnit publish
  profil: `dotnet publish src/EchoFactory.Game -c Release -r win-x64 --self-contained`
  + případně single-file. Otestovat, zdokumentovat v README.)

## C. Nastavení (Settings)
- 🔴 **Rozlišení / režim okna** — chybí. Aspoň přepínač **fullscreen / windowed**,
  ideálně i volba rozlišení. (Promítnout do `GameSettings` + aplikovat na
  `GraphicsDeviceManager`; pozor na přepočet layoutů scén při změně rozměru —
  scény berou `ScreenW/H` z `SceneManager` v konstruktoru.)

## D. Level editor (`LevelEditorScene.cs`)
Editor je nejvíc nedotažený. Hráč většinu funkcí nenašel (klávesové ovládání není
objevné) → **předělat na klikací UI**.
- 🟡 **Hodnoty > 9** — generátor i cíl umí jen jednociferné hodnoty (vkládá se
  klávesami 0–9, každá = jedna hodnota). Potřeba zadávat libovolná čísla
  (víceciferná, případně záporná). Lepší UI pro zadávání čísel (textové pole /
  +/- stepper / číselník).
- 🟡 **Velikost gridu nejde změnit** — je tam (šipky), ale hráč to nenašel →
  **klikací tlačítka W−/W+ H−/H+** nebo pole.
- 🔴 **Nastavení inventáře** — vůbec nejde (whitelist/limity). Serializace hotová
  (`LevelWriter`/`LevelInventory`), chybí UI.
- 🔴 **Max ticks** — nejde nastavit (teď napevno 60).
- 🔴 **Jméno levelu** — nejde zadat (teď prázdné/auto). Model i serializace
  (`Name`) hotové, chybí UI vstup.
- 🟡 **Test levelu z editoru** — je (`T`), ale neobjevné → dát tlačítko „TEST".
- 🔴 **Klikací paleta uzlů** — místo kláves umožnit **klikat na tlačítka** co
  postavit; umožnit umisťovat i „továrny" (math/splitter/portal/filter), ne jen
  generátor/cíl. (Editor zatím umisťuje jen fixed nodes; stavění linky se dělá až
  ve hře. Zvážit: editor = jen fixed + inventář; nebo plný editor.)

## E. Play / výběr levelů / kampaně
- 🔴 **Scrollování výběru levelů** — při hodně levelech se to nevejde
  (`LevelSelectScene` má 2 sloupce, ale bez scrollu). Přidat scroll (kolečko) a
  **neomezenou kapacitu**.
- 🔴 **Kampaně** — chce strukturu **Play → vyber kampaň → levely v ní**. Stažený
  pack levelů od někoho má spadat pod **jednu pojmenovanou kampaň**. Znamená:
  - levelu přidat pole **`campaign`** (id/jméno kampaně),
  - editor umožní napsat/zvolit jméno kampaně a přiřadit ho levelu,
  - výběr: nejdřív seznam kampaní, pak její levely,
  - **neomezený počet** kampaní i levelů (scroll).

## F. In-game vizuál & UX
- 🟡 **Spodní tlačítka (paleta + HUD)** — i v editoru i ve hře ošklivá; chtějí
  „grid"/vizuální rozdělení ve stylu tlačítek (hezčí styling, ikony, mřížka).
- 🟡 **Vizuál budov** — zkusit mírně vylepšit (uzly).
- 🔴 **Zatočené pásy** — pás by se měl podle natočení **ohýbat** (zatáčka vypadá
  jako zatáčka). Teď je to rovná řada chevronů bez ohledu na vstup/výstup.
  (Potřeba znát vstupní i výstupní směr pásu → kreslit oblouk. Vstupní směr se dá
  odvodit ze sousedů nebo přidat do `BeltNode`/placementu.)
- 🔴 **Splitter — nejasný směr výstupů** — má jen jednu šipku, z té nejde poznat
  oba výstupy (A/B). Vykreslit **oba výstupní směry** zřetelně (a v přehrávání
  ukázat, kam zrovna jde).

## G. Konfigurace uzlů — celkově špatná (priorita!)
Hráč nerozumí ovládání uzlů. **Klíčový úkol: klik na uzel → otevře se panel
specifický pro ten uzel** (místo globálních kláves +/-/Tab).
- 🟢 **Klikací config panel** — HOTOVO: klik na umístěný uzel otevře panel s
  tlačítky pro jeho typ (směr / konstanta / porovnání / výstupy / start / init +
  DELETE), vybraná buňka se zvýrazní, Esc zruší. Umisťování = drag prázdných
  buněk (klik na uzel ho už nepřepíše). (`GameplayScene.PanelControls`.)
  ⚠️ neověřené lokálně — proklikat.
- 🟢 **Help overlay + kodex uzlů** — HOTOVO: H / „? HELP" ukáže cíl levelu a
  vysvětlení každého uzlu (z JSON `description`). Pokrývá i math/portál slovně.
- 🟡 **Math (suma) — kde jsou vstupy?** — slovně vysvětleno v kodexu; pořád chybí
  **vizualizace vstupů/portů přímo na gridu** (explicitní vstupní porty).
- 🟡 **Portál — vysvětlení** — kodex ho popisuje; chybí **vizuální** in↔out
  „kometa" na gridu (sekce H).

## H. Design direction: smyčky, portál a „mega komplexní" puzzly  ⭐
Nejdůležitější design feedback. Hráč má jiný (bohatší) mentální model, než co je
teď implementované — **přečíst pozorně**.

**Co hráč čeká / chce:**
- Portál = **vrací v čase / dělá smyčku**, aby mohl mít **víc kopií čísel** a
  muset je zpracovat v cyklu. Konkrétní příklad: *„pojede mi 30× jednička a já
  musím nějak zajistit, abych je všechny sečetl v nějakém loopu."*
- Těch 30 jedniček by **kroužilo ve smyčce**, a podle **podmínky na splitteru** by
  šly buď **zpět do smyčky**, nebo **do cíle**.
- „**Mega komplexní věci** možné s tím dělat."

**Jak to mapuje na engine (důležité rozlišení dvou „smyček"):**
1. **Temporální smyčka (IMPLEMENTOVÁNO)** — `PortalNode` posílá item zpět v
   **čase**; multi-pass kompilátor hledá pevný bod. Jeden item se objeví dřív.
   Vizuálně těžko uchopitelné; `lvl_loop_01` to ukazuje, ale hráč to nepochopil.
2. **Prostorová/procesní smyčka (CHYBÍ jako jasná mechanika)** — itemy **krouží
   po pásové smyčce**, akumulují se a zpracovávají, a **podmíněné směrování** je
   pošle ven nebo na další kolo. To je klasický factory-loop a **přesně to hráč
   popisuje** (30 jedniček v cyklu, sčítat, splitter s podmínkou = router).

**Co z toho plyne (rozhodnuto s hráčem 2026-06-18: „obojí" — nové uzly + tutoriál portálu):**
- 🟢 **Router / podmíněný splitter** — HOTOVO: `RouterNode`/`RouterConfig`
  (`value <op> k` → `OutMatch`, jinak → `OutElse`). Nezahazuje (na rozdíl od
  filtru) → umožňuje „zpět do smyčky vs. do cíle". Tool **7 ROUTER** v paletě.
- 🟢 **Akumulátor / registr** — HOTOVO: `AccumulatorNode`/`AccumulatorConfig` —
  běžící součet (`Initial`), uvolní celkový součet, když `sum <op> k`, pak se
  resetuje. Stav čistý per pass (kompilátor staví uzly znovu). Tool **8 SUM**.
- 🟢 **Prostorové smyčky bez času** — DEMO: `lvl_loop_counter_01` (jeden item
  krouží belt-smyčkou, +1 za kolo, router ho drží ve smyčce dokud nedosáhne cíle).
  Belt-smyčka + router + math = procesní loop bez portálu.
- 🟡 **Portál — lepší vysvětlení/tutoriál** — help overlay (kodex) ho teď
  **slovně** vysvětluje; zbývá **vizualizace in↔out** („kometa" dráhy na gridu)
  + dedikovaný tutoriálový level. Druhá půlka hráčova „obojí".
- 🟡 **Levely kolem akumulačních smyček** — `lvl_tally_01` (sečti stream do N).
  Přidat těžší: kombinace router+akumulátor ve smyčce, sběr N hodnot a podmíněný
  výdej.

> ⚠️ Než se přidá router/akumulátor: probrat s hráčem, jestli chce (a) portál
> coby čistě prostorovou smyčku přejmenovat/vysvětlit, nebo (b) přidat nové uzly
> a portál nechat jako temporální specialitu. Tohle mění herní design — viz
> AskUserQuestion v handoffu.

---

## Doporučené pořadí (návrh)
1. 🟢 **Konfigurace uzlů klikem (panel)** + help overlay/kodex — HOTOVO (G).
   Zbývá vizualizace vstupů/portů a zatočené pásy na gridu.
2. **Editor → klikací UI**: jméno, max ticks, inventář, velikost gridu, číselník,
   test tlačítko, paleta (C, D). ← **další na řadě**
3. **Kampaně + scroll** ve výběru (E) — vč. pole `campaign` v levelu a editoru.
4. **Router/akumulátor + smyčkové levely** (H) — po dohodě s hráčem.
5. **Zatočené pásy + hezčí tlačítka + animované pozadí menu** (A, F).
6. **EXE publish + nastavení rozlišení** (B, C).
