# Task: UI cleanup pass — world backgrounds, hide panels, no scrollwork paper

Owner: Grok Build (background / unattended)
Pool: code first; visual / Imagine only for the short new-asset list below
Input: GitHub Darkgecko777/CaravansOfTheCuttingSands, branch main @ 87dcaaa95b51900ee7734dd64f0304f72f98fca4 (“First complete UI pass: looks poor”). Especially Art_Vision.md, Vision.md, AGENTS.md, Assets/Scripts/FrontDoor/FrontArt.cs, FrontUi.cs, TitleScreen.cs, HouseSelectScreen.cs, Assets/Scripts/Play/PlayPresenter.cs, CargoSoundScreen.cs, WorldClock.cs, Assets/Editor/FrontDoorImport.cs, Assets/Resources/Ui/, Assets/Resources/FrontDoor/, Assets/Resources/Map/. This is a follow-up to Task_Apply_UI_And_Goods_Art.md, which has already been applied.
Deliverable: play and house-select screens lose the scrollwork-corner paper; the world (textured sand, settlement desert, the map) is the background; dark tanned Giant Duzhu Hide is the default 9-slice panel; settlement chrome is slim; Asset Testing still reviews the same kit. Update Art_Vision.md, Vision.md, and AGENTS.md to match. Capture a 1920×1080 still of each surface under Acceptance.
Stop condition: the acceptance checks pass, or a required new asset fails the Art_Vision structural check twice, or a composition / missing-asset / ambiguity stop below is hit (record the fault, skip that piece, continue the rest, and report). Do not regenerate goods icons. Do not add goods, features, or materials.

## Context

The first UI-and-goods pass is in the game, and the result is too busy for the Steam stills (store page due 18 Nov 2026). The main fault is `Assets/Resources/Ui/parchment.png` — the aged sheet with ink scrollwork corners — used as a backdrop and as the default panel. House select puts a parchment wash behind bone-framed cards that are themselves filled with the same paper. Settlement stacks a top bar, a 190-pixel lumber-and-moss `FrontArt.Banner()` rail, and a second vellum name strip over an empty blue camera clear. Travel puts the road list on scrollwork paper and frames the map in paper. There is no focal point.

UI is still runtime uGUI (`Image`, `RawImage`, `Button`, TextMesh Pro) built in C#. No UXML, USS, or UI prefabs. Target is 1920×1080. Fonts stay `FrontDoor/Fonts/BonaNovaSC-Bold.ttf` and `BonaNovaSC-Regular.ttf`. Every physical UI piece must cite a world good. Panels that stretch are 9-sliced. Text is never baked into art. Frost Iron is reserved.

The scrollwork sprite is loaded only through `FrontArt.Parchment()` (`Slice("Ui/parchment", …)`). In play, `PlayPresenter.PaperOn` is the usual wrapper. Grep for `Parchment(`, `PaperOn(`, and `Ui/parchment`, list every use in the report, and replace each one by the rule in Locked 1.

## Do not touch

- Accepted goods icon files, or any regeneration / recrop / reimport of `Assets/Resources/Goods/<GoodId>.png`. Market, wagon, and logbook icon wiring from the previous pass stays; only the panel around those cells changes.
- `WorldClock`, the calendar, day-part names, start date, or `TravelScale.IsDayHour` / `EconomyRates.MorningHour` / `NightHour`.
- Economy JSON/CSV, travel math, kit rates, encounter rolls, audio, mounts, map painting, sand sim internals, title desert composition.
- New goods, new ids, unique per-tab seal devices, leather bookmarks, or a pulsing eye icon.
- Deleting or hiding the title’s ASSET TESTING button, the `CargoSounds` scene, or any of its eight tabs.
- Deleting `Ui/parchment.png` from the repo (it may stay unused on disk). Do not delete the plate files.

## Locked decisions (11 Nov 2026, America/Vancouver)

Earlier locks still stand: Shimmersteel hardware, Hulv Bone + Zar'Kun Sinew frames, shared Scrubcast Resin tab seal plus Unity text (Market may keep the scales device, Gates may keep the gate device), the ash-ivory vellum header scroll, clock format `1 Vekmar, Year 112 · Dawn`, and the accepted goods icons.

1. **Global background rule.** Remove the scrollwork-corner paper from every play and front-door screen. The world is the background: textured sand, settlement desert art, or the travel map. Plain parchment (no ink scroll corners) is used only for actual documents — the logbook now, letters later.
2. **Default panel.** One dark tanned Giant Duzhu Hide 9-slice texture replaces the scrollwork paper as the kit panel. Asset Testing’s UI Kit tab shows this hide as “Panel,” not the scrollwork sheet.
3. **House select.** Textured sand is the backdrop (drop the parchment `Wash` in `HouseSelectScreen.Build`). Keep the Choose Your House vellum banner, the CONFIRM menu button, and the small resin Back seal. Inside each existing `FrontArt.BoneFrame()`, replace the parchment `Fill` with the dark hide, lightly tinted in that house’s colour (`PlayPresenter.HouseColor`: Kharûn blue `0.55, 0.72, 0.84`; Zamath green `0.45, 0.62, 0.38`; Thalor red `0.72, 0.28, 0.22`; Veythar purple `0.55, 0.38, 0.68`; Ghorath amber `0.78, 0.52, 0.22`). Keep the hide grain readable. On dark hide, card lettering must flip from the current dark `CardInk` to a light ink (the play `Ink` / `FrontUi.Gold` / `TitleGold` range). Selected state is a slightly brighter hide tint, not a parchment wash and not a return to `PlateHot`.
4. **Settlement.** Slim top and bottom bars on a plain dark strip (the existing `Bar` colour is fine; not hide, not paper). Date, coin, water, and the other live Unity labels stay. One smaller, centred Shimmersteel rail with the place name (and patron line) on a hanging vellum field; no living lumber, no moss. Drop the lumber-and-moss rail and the duplicate vellum name strip — they are one unit, not a stack. Shrink `BannerHeight` (today 190) so chrome no longer eats a third of the screen; a centred rail around 720–960 wide and roughly 90–110 tall is the target. The settlement’s desert art fills the background, replacing the empty blue clear.
5. **Travel.** The map (`Map/upscale_map`) fills the travel area with a thin Shimmersteel edge (rod or slim rail; no paper frame, no parchment mat). The road list sits on the dark hide panel. Road and pace buttons stay as they are.
6. **Market.** Keep the bench’s dark goods cells, icons, demand rims, and Unity names. Place the shop and wagon on the dark hide panel. Buy / Sell / Sort unchanged in function; their flyout uses hide, not paper.

## New assets (Imagine) versus code-only wiring

Do not invent materials outside dark tanned Giant Duzhu Hide, ash-ivory vellum, plain parchment, Shimmersteel, Hulv Bone, Zar'Kun Sinew, and Scrubcast Resin. After Imagine, run the Art_Vision structural check silently. On failure, describe the fault in one sentence, regenerate with a forced fix, then present. Two failures: skip that asset, do not ship scrollwork paper as a stand-in, and report.

**Must**

1. **Dark hide panel.** A dark tanned Giant Duzhu Hide sheet, even grain, no text, no brands, no scroll corners, even enough edges to 9-slice as a tall panel and as a wide bar. Add `Assets/Resources/Ui/duzhu-hide-panel.png` (or similar) and a `FrontArt.HidePanel()` accessor. Do not overwrite `parchment.png` or `hide-header.png`. Add the new file to `FrontDoorImport.FixIfImportedEarly` (Ui PNGs already import as Default, no mips, readable, clamp, `alphaIsTransparency`, max 2048).
2. **Textured sand backdrop.** A wide or tileable sand ground for house select: visible grain, no ornaments, no baked text, no paper. Prefer a new `Ui/` or `FrontDoor/` file shown full-bleed behind the banner and cards. `TitleScreen.BuildDesert()` already uses `FrontDoor/title_screen.png`; reuse that plate for house select only if it is a clean desert with no baked lettering and it reads as textured sand. If you reuse it, say so and skip a second sand plate.
3. **Settlement desert ground.** There is no per-settlement location art in `Assets/Resources/` (folders are Audio, FrontDoor, Goods, Map, Mounts, Ui). Do not use `Map/upscale_map.png` as the settlement ground. `FrontDoor/title_screen.png` is the only existing desert plate; inspect it. If it has no baked text it may be a temporary settlement ground, but a dedicated settlement desert (dunes, no typography, no UI chrome) is preferred so every town does not look like the title card. If the file is missing or unusable, stop that piece and report rather than leaving the blue clear.
4. **Plain parchment for documents.** The current `Ui/parchment.png` is the scrollwork-corner sheet and must not appear in play. Recast it, or add `Ui/parchment-plain.png` and retarget `FrontArt.Parchment()` to the plain sheet (aged cream, fiber and deckle, no ink corners, no medallions, no text). The logbook uses this file. Leave the scrollwork PNG on disk unused if you add a new name.
5. **Banner rail without lumber.** Recast `Ui/banner.png` (and retarget `FrontArt.Banner()` borders if the shape changes) so the rail is Shimmersteel only: faceted bar, sand-bronze knobs, open centre for the hanging vellum, no living lumber, no moss, no baked text. `Ui/shimmersteel-rod.png` may be used as the thin bar if it already reads correctly at the smaller size. If the recast still contains lumber, moss, or lettering, stop and report rather than shipping it. Update the FrontArt comment that still describes a lumber lip.

**Not required:** a new bone frame, new seals, new vellum header, new goods icons, unique tab devices, per-settlement variants, or a recast of the title menu frame.

## Batching (screenshot-critical first)

Code that swaps `PaperOn` / `Parchment()` to `HidePanel()` can start on surfaces that do not wait for the desert plates. Run Imagine for hide, sand, plain parchment, and the rail recast in parallel. Settlement stills wait on the desert ground and the rail recast; house-select stills wait on the sand backdrop and the hide panel.

**Phase 1 — Kit swap and slim bars.** Add `FrontArt.HidePanel()`. Grep and list every `Parchment` / `PaperOn` / `Ui/parchment` use. Retarget `PaperOn` (or split it) so play panels use hide and only documents use `Parchment()`. Slim `BuildTopBar` / `BuildViewBar` / the dock tab strip to plain dark strips; if the two top strips are redundant, combine them. Flip labels on hide panels to light ink. Do not put hide or paper in the title desert.

**Phase 2 — House select (first-visit still).** Sand backdrop; keep the vellum title; hide fills in the bone frames, tinted per Locked 3; keep CONFIRM and the resin Back seal. Drop `PaintSky`’s empty look once the sand is in. Flavour line stays Unity text, light enough to read on sand.

**Phase 3 — Settlement chrome and ground (primary store still).** Slim bars. One smaller centred `FrontArt.Banner()` + vellum field + Unity place name and `session.PatronLine()`. No lumber, no second name strip. Desert plate full-bleed behind the dock body; sand-coloured camera clear as a fallback. Tab strip is a slim dark bar; seals follow the earlier lock (Market scales, Gates gate device, other tabs shared resin + Unity text).

**Phase 4 — Travel.** Map fills the travel area; thin Shimmersteel edge on `mapFrame`. Road list (`MapRoads`) on hide. Pace, Embark, playback, and road buttons unchanged. Rival discs, destination ring, and route trail stay.

**Phase 5 — Market and wagon.** Shop, wagon, and detail strip on hide. Keep the dark cells, icons, name bands, demand rims, empty-slot fallback, and Buy / Sell / Sort. Sort menu is a small hide flyout.

**Phase 6 — Remaining play surfaces.** Logbook overlay: plain parchment (document), same calendar header and row icons. Plaza / Inn / Tavern / Stables / House / Gear stubs and options: hide or bone, not scrollwork; same stub copy. Road cards keep the bone frame; the inner fill is hide (or empty), not paper. Close and action buttons stay resin or `ButtonPanel`.

**Phase 7 — Asset Testing and docs.** Keep all eight tabs. UI Kit: hide is the Panel sample; top-bar sample is a slim dark strip with `1 Vekmar, Year 112 · Dawn`; bone frame samples stay; vellum header stays. Layers shows the recast rail (no lumber) with vellum. A plain-parchment sample may sit beside the hide panel, labelled as the document sheet. Then write the docs under Acceptance.

## Guardrails

- No baked text, symbols, or labels in any picture.
- No bronze, generic timber, polished ivory, frost iron, or off-list materials in new or recast UI art. No living lumber or moss on the settlement banner.
- Do not introduce the word “watch.”
- Do not change start-of-run hour, dawn/dusk wait targets, or day/night kit math.
- Do not regenerate goods icons. A missing icon is still a fallback, not a stop.
- Runtime uGUI only; no new prefabs or UXML.
- Scrollwork paper must not remain visible as a “temporary” panel or backdrop.

## Acceptance

- No scrollwork-corner paper is visible on title, house select, settlement (market, plaza, house), travel, road cards, logbook, options, or any other play surface.
- House select sits on textured sand; cards are bone frames with dark hide fills, lightly house-tinted; lettering reads on the hide; CONFIRM and the resin Back seal remain; the Choose Your House vellum banner remains.
- Settlement has slim dark top and bottom bars, one smaller centred Shimmersteel-and-vellum banner with Unity text and no lumber or moss, and desert art behind the chrome rather than a blue clear.
- Travel: the map fills the area with a thin Shimmersteel edge; the road list is on dark hide; buttons unchanged.
- Market and wagon cells still show goods pictures (or a clean name fallback) on the dark hide panel.
- Logbook is plain parchment, not scrollwork and not hide, and still shows the calendar date and row icons.
- Title still has ASSET TESTING. The bench still reviews Market, Front door, Map, Mounts, UI Kit, Layers, Card, and Seals. UI Kit shows the hide panel; Layers shows the recast rail. Back still returns to the title.
- Art_Vision.md no longer calls the scrollwork sheet the information panel and top bar, and no longer describes a lumber-and-moss lip on the location banner. It records: dark hide as the default panel; the world as the background; plain parchment for documents only; the Shimmersteel rail with hanging vellum and no lumber. Vision.md and AGENTS.md match.
- One 1920×1080 still each: house select, settlement market, travel, and the Asset Testing UI Kit (and Layers if the rail changed). Note any leftover scrollwork if a piece was skipped.

## Stop and report

Stop the current piece, leave a written fault, and continue the rest if: a must-have asset is missing or fails the structural check twice; `banner.png` still has lumber, moss, or baked text; `title_screen.png` cannot be reused and no settlement desert was generated; a change would require a new material or a new good; or you cannot tell whether a `Parchment()` call is a real document. If it is a document, switch it to the plain sheet and continue — do not leave scrollwork visible.
