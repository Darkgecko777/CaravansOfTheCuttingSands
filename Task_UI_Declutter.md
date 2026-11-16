# Task: UI declutter pass (from the Screenshots/ review)

Owner: Grok Build
Input: main @ a4e38e4 ("Screenshot captures"). Read `Screenshots/` + `Screenshots/index.md` first, then PlayPresenter.cs, FrontArt.cs, FrontUi.cs, TitleScreen.cs, HouseSelectScreen.cs, ApprovedArt.cs, Art_Vision.md.
Supersedes the remaining open items of Task_UI_Cleanup_Pass.md where they conflict.
Target: 1920x1080. Steam store stills are due 18 Nov 2026, so the Market, Gates/travel map, Departure and House Select screens matter most.

## The problem (seen in the captures)
1. **Bone frame overload.** Thick Hulv-bone borders are on the top bar, the bottom tab bar and every panel. On the market (10-15) the three panels sit side by side, each with its own fat frame, so the frames eat about a third of the screen and the borders touch.
2. **Frames inside frames.** The pace card, encounter card and camp card (31, 33, 34, 35) are a bone-framed card inside a second pale stick frame (`BoneOn` at PlayPresenter ~848 plus the card's own sheet). The settings popup (60) is a bone card floating over bone panels.
3. **Text too small and low contrast.** Market cell names, the labels under the round seal buttons, the plaza menu lines (thin yellow on dark), the top-bar date, coin and cargo (26 pt) and the road-list text are hard to read at 1080p.
4. **Empty giant frames.** Tavern, Stables (22, 23), House (50) and the title Options panel (02) are huge frames around one line or nothing.

## What already works (don't touch)
Title screen composition, house-select layout and cards, goods icons and cells, the travel map art, the departure view (settlement sprite, spotlight, glowing trail), the logbook as a parchment document (it is a document by the earlier rule, so it stays parchment), the economy and travel logic.

## Rules
1. **One frame level.** A given rectangle on screen has at most ONE decorative border. Children of a framed panel get no border of their own. Use thin dividers (1-3 px umber line, or a slim sinew cord sprite) to split areas inside a panel.
2. **Thin frames.** Where the bone frame stays, cut its rendered border thickness to roughly 40% of today's (set `Image.pixelsPerUnitMultiplier` on sliced images, or a slimmer 9-slice). The bone should read as an edge, not a wall. Bone frames only on: the main market panel, the gates/travel panel, modal cards, and house-select cards.
3. **Bars.** Top HUD bar and bottom tab bar become slim plain dark hide strips (`HideBarOn` / the approved hide bar, or `DarkBar`) with no bone, about 72-80 px tall (TopBarHeight 128 and TabHeight 118 today). Keep every live label and seal.
4. **Modal cards.** A modal card is one bone-framed dark-hide sheet over a dimmed backdrop (black at 50-60% alpha, full-screen, blocks raycasts). Remove the outer pale stick frame entirely. This applies to the pace card, encounter, camp, after-camp pace, settings and title Options.
5. **Size to content.** Panels with little content shrink to fit, with a sensible minimum (about 520x260). Tavern, Stables and House show a centred, compact card with their text, not a full-height frame. The title Options card holds just its controls plus Close.
6. **Type scale.** Minimum text is 22 pt at 1080p, anywhere. Body and menu lines are 26-28 pt, HUD values 30-32 pt and headers 36-40 pt. Text on dark hide is cream (about #EADCC0), and gold is for headers and selected states only. Add a 1-2 px dark outline or soft shadow to text that sits on busy art. Centralise these as constants (e.g. `UiType.Body`, `UiType.Small`) and use them everywhere instead of literals.
7. **Market layout.** One bone-framed panel holds three columns (shop grid | detail | wagon grid), separated by thin dividers and not three frames. Keep cell size and icons. Detail column: the good's icon large at the top, its name, price and demand in readable type, Buy and Sell seals under it, and the meter at the bottom. The sort flyout is a plain dark-hide dropdown with no frame. Cell names drop below icons only if they fit at 22 pt; otherwise show the name in the detail column and on hover, not under every cell.
8. **Gates / travel.** The map gets the maximum area. The road list panel on the left keeps its bone frame thin, and its lines are readable seal-plus-text rows. Pace seals sit in one row under the list. The map gets a thin Shimmersteel rod edge, not bone.
9. **Spacing.** Use a consistent 16/24/32 px spacing scale. Leave at least 24 px between any two framed elements, so frames never touch.

## Done when
- Re-run `Caravans/Capture UI Screenshots` and commit the refreshed `Screenshots/`.
- No captured screen shows a border inside a border.
- No text under 22 pt.
- The market reads as one panel.
- Tavern, Stables, House and Options are compact cards.
- Update Vision.md (as-built) and Art_Vision.md with the one-frame-level rule and the type scale.
- Report: the list of changed helpers and constants, plus anything you could not do and why.

## Stop / don't
- No new art assets in this pass except an optional slimmer bone 9-slice if pixelsPerUnitMultiplier can't do it cleanly. No changes to goods icons, map art, economy, travel or encounter logic.
- Keep the ASSET TESTING button and the CargoSounds scene.
