# Task: UI screenshot capture tool

## Why
Caravans Bot (the art reviewer) can't run Unity. It reads the repo through GitHub. To critique and fix the UI (9-slice borders, cards nested in cards, spacing), it needs current screenshots of every screen, committed to the repo, refreshed with one click whenever the UI changes.

## Build
Add an editor-only capture tool, `Assets/Editor/UiScreenshots.cs`, plus any small runtime hooks it needs (keep them out of shipping behaviour; `#if UNITY_EDITOR` or a debug-only static entry point is fine).

1. **Menu item** `Caravans/Capture UI Screenshots`. It enters Play mode from TitleScreen if needed, then walks through every screen state below automatically, with no clicking.
2. **Fixed resolution.** Capture at 1920x1080. Force the Game view to that size, or render the camera and canvas to a 1920x1080 RenderTexture so the result doesn't depend on the editor window. Overlay canvases must be included.
3. **Settle before each shot.** Wait 2-3 frames (or until layout and fades finish) after each state change, then `ScreenCapture.CaptureScreenshot`. Don't capture mid-storm-fade or mid-tween.
4. **Output** to `Screenshots/` at the repo root (not under Assets, so Unity doesn't import them). Use zero-padded, stable, descriptive names that overwrite each run, e.g. `01_title.png`, `02_title_options.png`, `03_house_select.png`, `04_house_selected.png`, `10_market_empty.png`, `11_market_selected_buy.png`, `12_market_sort_menu.png`, `20_plaza_inn.png` ... `30_travel_map.png`, `31_road_choice.png`, `32_departure.png`, `40_logbook.png`, `50_house.png`, `60_options.png`.
5. **Seeded state.** Use a fixed test save or seed so shots are deterministic: a mid-game caravan at Kharûn with some cargo in the wagon, some coin, and several logbook entries, so lists and grids aren't empty. Add one capture with a long place name (e.g. Ridgewatch or Sarn's Rest) to check that labels fit.
6. **Manifest.** Write `Screenshots/index.md` listing each file, the scene and state it shows, and the capture date and commit hash.
7. **Exit Play mode** when finished and log `Captured N screenshots to Screenshots/`.

## Screen states to cover
- **TitleScreen:** main and options open.
- **HouseSelect:** nothing selected and one house selected.
- **Play (`PlayPresenter`):**
  - Market (empty selection, a shop good selected, a wagon good selected, sort menu open)
  - Plaza, one shot each for Inn, Gates, Tavern, Stables
  - House
  - Travel map with neighbour road buttons
  - Road choice / pace picker
  - Departure / en route with the route line drawn
  - Logbook open
  - Options open
  - The top HUD bar is visible in all of these.
- **CargoSounds test scene:** one shot per page (Market, FrontDoor, Map, Mounts, UiKit, Layers, Card, Seals). These are reference sheets, not shipping UI, so prefix them `90_`.
- Add any screen or popup that exists in code but isn't listed here (encounters, dialogs, tooltips) and note it in index.md.

## Done when
- One menu click produces the full set in `Screenshots/` with no manual steps and no errors in the console.
- Running it twice gives the same file names, so diffs in git show just the UI changes.
- Update Vision.md (as-built) with a line about the tool. Don't change any UI visuals in this task; this is capture only.
- The user commits `Screenshots/` along with the code.
