# Frontend — MonoGame, scény, render, vizuální styl

Frontend (`EchoFactory.Game`) je čistě prezentační: vykreslování, vstup, zvuk,
přehrávání předpočítaných stavů. **Neobsahuje herní logiku** — tu vlastní
`Core`. Renderer dostává `GridState[]` a kreslí je.

---

## 1. Volba enginu: MonoGame (za abstrakcí)

Doporučení: **MonoGame**. Zralý, prověřený shippnutými Steam hrami (Stardew
Valley, Celeste), dobrý content pipeline, velká komunita. Rozhodnutí a srovnání
s Raylib-cs: [ADR-0002](./adr/0002-monogame-over-raylib.md).

Aby nebyl engine „zadrátovaný", veškerý rendering jde přes tenkou abstrakci:

```csharp
public interface IRenderer
{
    void DrawLine(Vec2 a, Vec2 b, Color c, float thickness);
    void DrawCircle(Vec2 center, float radius, Color fill, Color stroke);
    void DrawText(string text, Vec2 pos, Color c, float size);
    // ...
}
```

`MonoGameRenderer : IRenderer`. Scény volají jen `IRenderer`. Výměna enginu
(kdyby) se pak dotkne jediné třídy, ne celé hry.

## 2. Scény (State pattern)

Hlavní `Game.Update/Draw` deleguje na aktivní `IScene`. Scéna má životní cyklus
`Load → Update(dt) → Draw → Unload`. Přepínání přes `SceneManager` (zásobník pro
overlay/pause).

```csharp
public interface IScene
{
    void Load();
    void Update(float dt, InputState input);
    void Draw(IRenderer r);
    void Unload();
}
```

| Scéna | Obsah | Pozn. |
|-------|-------|-------|
| `BootScene` | Init `IPlatformServices`, načtení JSON registrů + assetů, loading bar. | jednorázově |
| `MainMenuScene` | Pokračovat / Hrát / Editor / Workshop / Nastavení / Profil / Titulky. | — |
| `LevelSelectScene` | Mapa/strom levelů, náhled žebříčků pro vybraný level. | — |
| `GameplayScene` | Build / Compile / Playback (sub-stavy) + HUD. | jádro |
| `LevelEditorScene` | Tvorba levelů, omezení inventáře, export JSON. | M6 |
| `SettingsScene` | Obraz / Zvuk / Ovládání / Hra / Jazyk / Přístupnost (overlay). | M4 |
| `ProfileScene` | Postup, statistiky, osobní rekordy. | M8 |
| `WorkshopScene` | Procházení/stahování komunitních balíčků. | M7 |
| `CreditsScene` | Titulky. | M8 |

**Overlaye** (nad aktivní scénou ve stacku): `PauseOverlay`, dialogy, toasty.

> Kompletní popis menu, nastavení, HUD, save systému, onboardingu a přístupnosti
> má vlastní dokument: **[ui-ux.md](./ui-ux.md)**. Volba UI toolkitu:
> [ADR-0005](./adr/0005-ui-toolkit.md).

## 3. GameplayScene — sub-stavy

```
   BUILD ──(Compile)──► [Core na pozadí] ──► PLAYBACK
     ▲                       │ Paradox          │
     │◄──────────────────────┴──────────────────┘
        (uprav a zkus znovu)     (zpět do buildu)
```

- **Build Mode**: paleta uzlů (z registru, omezená `inventory` levelu),
  umisťování na grid, drag/rotace, **undo/redo** (command pattern). Validace
  „kam smím" v reálném čase.
- **Compile**: zavolá `SimulationCompiler` (ideálně na background threadu, aby
  UI neškublo), zobrazí progress, výsledek nebo paradox.
- **Playback Mode**: přehrávač předpočítaných `GridState[]`:
  - Play / Pause / Step / Reset,
  - **Timeline scrubbing** (tažení po časové ose, okamžitý skok na tik T),
  - rychlost přehrávání (0.5× / 1× / 2× / 4×),
  - přepínatelný **debug overlay** „časové stopy" itemu (viz design §6.7).

## 4. Tweening — most mezi diskrétním a plynulým

Simulace je diskrétní (tik). Render je plynulý. Mezi `stateₜ` a `stateₜ₊₁` se
pozice itemu **interpoluje** podle `playbackProgress ∈ [0,1)`:

```csharp
renderPos = Lerp(item.PosAt(T), item.PosAt(T+1), playbackProgress);
```

> Důležité: tweening je **čistě vizuální**, žije ve frontendu a **nikdy** se
> nevrací do Core (pravidlo determinismu #1). Floaty smí být jen tady.

Speciální vizuální události (`VisualEvent` z `GridState`): spawn (fade-in),
merge (sloučení), math (puls), portal-in/out (nasátí/vyplivnutí s pulzujícím
outlinem), collision (záblesk + zastavení na paradoxu).

## 5. Vizuální styl (Mini Metro inspirace)

Žádné textury, skeuomorfismus ani pixel-art. Vektorová čistota a čitelnost.

| Prvek | Provedení |
|-------|-----------|
| **Mřížka** | Jemný nízkokontrastní raster, ne dominantní. |
| **Pásy** | Čisté spojité linky, vysoký kontrast. Směr = animované body podél linky. |
| **Uzly** | Základní geometrické tvary (`shape` z JSON), barevné kódování (`color_hex`), ikona/symbol uprostřed. |
| **Itemy** | Minimalistické bílé kruhy s výrazným bezpatkovým číslem uvnitř. Plynulý pohyb (tweening). |
| **Portály** | Pulzující outline, specifické vizuální propojení in↔out. |
| **Paleta** | Dark mode, neonové akcenty indikující tok čísel. |

**Přístupnost (od začátku):**
- Barva **není** jediný nositel informace — uzel má vždy i tvar/ikonu.
- Paleta přepnutelná na colorblind-safe (paleta je v JSON → snadné).
- Škálovatelná velikost písma itemů.

## 6. Vstup

`InputState` abstrahuje myš/klávesnici (a do budoucna gamepad). Mapování kláves z
nastavení (`/data/locale` neřeší — to je config). Build mode: levý klik umístit,
pravý klik smazat, kolečko rotace, `Ctrl+Z/Y` undo/redo. Playback: mezerník
play/pause, šipky step, tažení po timeline.

## 7. Zvuk

Minimalistický, funkční (Mini Metro styl): jemné tóny na spawn/merge/math,
výraznější na splnění cíle, „dissonance" na paradox. Hlasitost a mute v
nastavení. Audio je čistě frontend, mimo determinismus.

## 8. Co frontend NEsmí

- ❌ Mít herní pravidla (pohyb, matematiku, časový model) — to je v `Core`.
- ❌ Posílat floaty/odvozené hodnoty zpět do simulace.
- ❌ Záviset přímo na Steamworks (jen přes `IPlatformServices`).
