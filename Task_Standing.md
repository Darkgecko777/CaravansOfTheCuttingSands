# Task: Kharûn standing — sale profit, first flips, rank readout

Owner: Grok Build
Input: the current checkout, plus Systems' standing design already sitting at the repo root in `_Incoming/standing/standing.json`. That file is standing v2 (schema v2, Kharûn-only, locked by Darkgecko 2026-11-20). Read the code first, then change it. Do not add work beyond this file.

**Wait for prior tasks.** This task expects `Task_Encounters_Batch1` and `Task_Supplies` to be committed first. Those change `CargoUnit` usage (card cargo, kit pours, soft reset), `PlayPresenter` (intro, encounter modal, supply HUD, rescue card), `PlayTests`, `Vision.md`, and `Screenshots/`. Reuse whatever they left in place. If a class, method, or line cited below has moved, follow the behaviour, not the line number. Do not undo the intro card, the 16 encounter cards, the kit / auto-pour / death path, or the 15% merchant spread. Do not retune prices, map-piece list prices, or mount numbers. Leave `ProjectSettings.asset` alone.

The checkout this was written against is the `CaravanSession` / `PlayPresenter` / `prices.json` shape used for the spread, menu, and supplies tasks (standing is still a raw `standingOf` cap of 8 that cuts the tariff). The place is `Westmark` (correct name, west of Kharûn). Every game is House Kharûn; house select is stripped — do not reintroduce it, and do not add standing with any other House. Do not purge "wagon" from existing UI or identifiers (a later sweep; "Your Wagon" stays). In new prose, never call the caravan a wagon, and if a starter bird is named, call it a **thrask**. Do not call the art hand-painted; if a style word is needed, use painterly.

## Why

Standing starts at nothing, and play has no way to raise it. `TariffPercent` subtracts raw `standingOf` steps from a 12% rival cut and stops at 4, with a single `standingMax` of 8. The House room still says "Jobs and standing are not in this slice." and is hidden while `ShowUnfinishedFeatures` is off. A playtest build needs Kharûn-only standing from sale profit and first flips, a flat rival tariff, a visible rank with progress, and a public `GetKharunRank()` hook for the later Tavern / map-piece task.

## How standing and tariffs work today (current code — do not invent a second tariff path)

- Tunables live in `data/config/prices.json`, loaded by `GameConfig.Read` with `JsonUtility` from `data/config/` (`Assets/Scripts/Play/GameConfig.cs`, `EnsureLoaded`). Required keys are checked by name in the file text and currently include `"tariffBase"`, `"tariffFloor"`, `"standingMax"`. There is no YAML parser. Use flat `[Serializable]` classes and arrays only (no dictionaries, nulls, or polymorphism). Missing fields default to 0, `""`, or false. `GameConfig.Reset` clears `loaded` and forgets catalogs; keep every forget-hook intro / encounters / supplies added, and add this task's the same way.
- `PriceFile` (`GameConfig.cs`, the `tariffBase` / `tariffFloor` / `standingMax` fields) and `EconomyRates.TariffBase`, `TariffFloor`, `StandingMax` wrap those keys. `RivalCutFallsFromTwelveToFour` in `PlayTests.cs` locks a 12 → 4 cut through `SetStanding` — that test is now wrong and must be updated or replaced (§4, §8).
- Session state is `readonly int[] standingOf` in `CaravanSession` (around line 78), one slot per `WorldIds.HouseCount`. `SetStanding(HouseId house, int steps)` (around 794) clamps 0..`StandingMax` and writes `standingOf` directly. There is no points field, no flip-seen flags, and no highest-rank floor. `ResetCaravan` zeros `standingOf`.
- `TariffPercent(PlaceId place)` (around 337): 0 if `House` is null, or if `PatronOf(place)` is null, or if the holder is the player's House; otherwise `TariffBase - standingOf[holder]`, floored at `TariffFloor`. The `standingOf` term and the floor both go.
- `PatronOf` reads the `patron[]` array. Cities get a fixed seat House in `ResetCaravan` via `EconomyCatalog.CityHouse` and never change (`RefreshPatron`, around 1254, returns early for cities; ties keep the current patron). Villages and posts start with no patron and pick one up from influence. `EconomyCatalog.KindOf` (around 95) is the source for place kinds: five cities, nine villages (Draven, Ashar, Moraq, Torvern, Sorel, Kethra, Neth, Rukh, Ghûl), six posts (Westmark, Kaleth, Southmark, Highmark, Brineford, Ridgewatch).
- Cargo is `List<CargoUnit> { Good, Integrity, DecayedOnDay }` (around 60). There is no cost field. `TryBuy` (around 427) charges `BuyCost` (= `BuyPaid` = ask + tariff, around 1642), then `stacks.Add`s a unit at integrity 1. `TryFill` / `TryFillWater` / `TryFillFood` (around 457–487) buy a shelf unit straight into the kit and **create no `CargoUnit`**. `SellUnit` (around 520) pays `Net` (gross after spread, minus the tariff cut), then may add influence and call `RefreshPatron`. `WorstStack` (around 1603) picks the sold unit. `NoteVisitBuy` / `TakeVisitBuy` (around 1662–1672) are the existing per-good same-visit resale counters (`SeaWater` aliases to `Water`); `SellUnit` already skips influence when `TakeVisitBuy` is true. `ApplyIntegrityHit` and `DecayCargo` (around 1230) change integrity only.
- Influence is village- and post-only (`!EconomyCatalog.IsCity(Dock)`), added from player `SellUnit` and from NPC sales. `TickMorning` decays influence by `influenceKeep` (0.75) and will already refresh patrons — those ticks must not award flip points. `MerchantSpread()` is the spread hook; standing must still not change the spread in this task.
- The House tab is `PlayPresenter.BuildHousePage` (around 661), stub text "Jobs and standing are not in this slice." `HideUnfinished()` still sends `Page.House` back to Market while `ShowUnfinishedFeatures` is off, so that tab is not a visible readout.
- `InternalsVisibleTo("Caravans.Play.EditModeTests")` is already on `CaravanSession.cs`. Add small internal or test hooks rather than driving the whole market UI.
- Agents, mounts, and map-piece buying do not exist in code. Do not invent them. If supplies has landed, a soft reset already keeps "standing"; extend that to Kharûn points, rank, and the first-flip marks (`kharunFlipSeen`).

## Locked design (2026-11-20, standing v2)

Recorded in `_Incoming/standing/standing.json` and `systems/standing/notes.md`. Implement these, and keep the numbers in `prices.json`, not in literals sprinkled through code.

1. Standing exists **only with House Kharûn**, capped at rank 10, 100 points per rank. Rank = `min(10, floor(points / 100))`. Points only rise; no card or system takes Kharûn points away. A reached rank never drops.
2. **Sales, anywhere:** `profit = net coin received (after tariff) − that unit's Basis`. If `profit > 0`, `points += profit / (standingCoinPerPointBase + standingCoinPerPointStep × current rank)` — 18 + 8 × rank. Points are a **float**; do not truncate each sale. A sale in a city, in an unheld town, or in a Kharûn-held town all count the same. There is no "holder" credit and no rival standing.
3. **No sale points for:** a loss or break-even sale; a same-visit resale (`TakeVisitBuy`); a unit with Basis −1 (card or find); Rations; units poured into the kit (not a sale).
4. **Flip bonus: +15 points** on the **first player-caused flip** of each of the 15 holdable places (the nine villages and six posts listed above). Trigger it inside `SellUnit` after `RefreshPatron(Dock)`: the patron **changed to House Kharûn** and `kharunFlipSeen[place]` is false → add `standingFlipBonus` and set the flag. Flips caused by Kharûn NPC caravans or by the daily decay tick do not count and must **not** set the flag (the player's later first flip of that place still pays). Cities never flip. A flip-back costs nothing; a re-flip pays nothing. Store one bool per place, kept on soft reset and on save.
5. Tariff stays `tariffBase` 12. **Remove** `tariffFloor` and `standingMax`. `TariffPercent` = 0 if the holder is House Kharûn or there is no holder; otherwise a flat 12%. Rank does not cut the tariff. Flipping a place to Kharûn already removes its tariff (existing code), so a first flip pays twice: no tariff there while you hold it, plus the one-time bonus.
6. Map pieces are **not** in this task. Expose `GetKharunRank()` and a Kharûn points getter (name can vary, e.g. `KharunPoints`) for the later Tavern task. That task will gate the six pieces at Kharûn's Tavern by rank (1, 3, 5, 7, 9, 10). Do not sell pieces, bank leads, or open a Tavern here.
7. Out of scope: wagon wording sweep, expeditions, Socialize, agent hiring, the thrask → duzhu → pincer ladder, map-piece buying, shrinking `MerchantSpread` with standing, stripping leftover house-select UI (if a prior task has not already), and Steam achievement wiring.

## 1. Install the data

`_Incoming/standing/standing.json` is the designer record (thresholds, flip list, card mapping, pacing notes). **Do not load that file at runtime.** It has nested maps, notes, and mixed-type pacing arrays that `JsonUtility` will not parse cleanly. Read it for the rules, then put the live tunables in `data/config/prices.json`, and delete `_Incoming/standing/` (and the empty `_Incoming` folder if nothing else remains). If the incoming folder is already gone and `prices.json` already has the new keys, use those.

In `prices.json`, `PriceFile`, the `EnsureLoaded` required-key list, and `EconomyRates`:

- Keep `tariffBase` 12.
- **Remove** `tariffFloor` and `standingMax`. Replace every `EconomyRates.TariffFloor` and `EconomyRates.StandingMax` call site.
- Add `standingCap` 10, `standingPointsPerRank` 100, `standingCoinPerPointBase` 18, `standingCoinPerPointStep` 8, and `standingFlipBonus` 15.
- Required-key checks should look for those new names (and `"tariffBase"`), and must no longer require `"tariffFloor"` or `"standingMax"`.

You do **not** need to run `systems/standing/tools/standing_sim.py`. The formula and flip bonus are already in the JSON. Do not retune the coin-per-point numbers, the flip bonus, or mount prices to chase a 12-hour run.

## 2. Cost basis

Add `int Basis` to the existing per-unit `CargoUnit`. Cargo is already one entry per unit, so stacks bought at different prices need no extra structure: each unit keeps what it cost, and the sold unit (`WorstStack` or a specific stack) uses its own Basis.

- `TryBuy`: set `Basis` to the coin actually paid (`BuyPaid` / `BuyCost` at that dock: ask + tariff).
- `TryFill`: a fill today creates **no** `CargoUnit` — it pours the shelf unit into the kit — so there is no Basis to store and the fill awards no standing. Do not invent a cargo unit for a fill. If a later path ever puts a filled unit onto the packs, that unit's Basis is the coin paid.
- **Card or find goods: Basis = −1** (unpriced). They sell for coin as normal but give no standing. Every code path that adds a `CargoUnit` other than `TryBuy` (encounter `cargo[]` grants, test hooks, finds, and so on) must set −1 unless it is a paid buy. Zero basis is wrong: it would turn a found unit into a standing windfall.
- `ApplyIntegrityHit` and `DecayCargo` must not change Basis. A damaged unit simply makes less profit, or none.
- Kit pours (supplies auto-pour, or any other removal into the skins) destroy the unit without a sale, so they earn no points.
- Soft-reset cargo loss: lost units take their Basis with them; kept units keep theirs. There is nothing else to do.
- Old saves, and any unit that loads without a Basis field, load as −1. New units from `TryBuy` always have a paid Basis.

## 3. Points, flips, ranks, and persistence

Replace the per-House `standingOf` array as a source of truth. Keep one float `kharunPoints` (or equivalent) and a `bool` first-flip mark per place (`kharunFlipSeen`). Public rank is `GetKharunRank() = min(standingCap, floor(points / standingPointsPerRank))`. Rank is derived from points; points never fall, so a reached rank never drops.

On each **player** sale in `SellUnit` (and `TrySell` / `TrySellStack`), in this order:

1. Read the sold unit's `Basis` and the `Net` already computed for that unit, and read `PatronOf(Dock)` **before** any influence is added.
2. Remove the unit, pay `Coin`, then run the existing `TakeVisitBuy` / influence / `RefreshPatron` block exactly once — do not decrement the visit counter twice. Cities still add no influence and never change patron.
3. Award sale standing only if all of these hold: `Basis >= 0`; `profit = net - Basis` is `> 0`; `TakeVisitBuy` was **not** true for this sale; the good is not Rations; this is a sale, not a kit pour. Then `points += profit / (base + step × GetKharunRank())` using the rank from **before** this sale's points (and before any flip bonus below). A sale in an unheld town still counts.
4. Then the flip check: if this sale actually ran `RefreshPatron` (not a city, not a same-visit resale) and the patron **is now Kharûn and was not Kharûn before**, and `kharunFlipSeen[Dock]` is false, add `standingFlipBonus` and set the flag. A profitable first-flip sale may award **both** the profit points and the +15 in the same `SellUnit`.
5. If the rank rises, fire the rank-up toast (§7). Points may go a little past 1,000 on the last sale or flip; the displayed rank still caps at 10, and the progress readout at rank 10 has no "next" bar.

NPC `BuyLoad` / `SellCargo` / `UnitProfit` must **not** award sale points or the flip bonus, and must not set `kharunFlipSeen`. Morning influence decay must not either.

Card standing effects are flat Kharûn points, not run through the coin divisor, and may only add (schema 0..+25). Do not write a path that subtracts standing. A helper such as `AddKharunPoints(float delta)` ignores non-positive deltas, then adds and fires the toast if the rank rises. `SetStanding` (or a `SetKharunPoints` / rank setter for tests) may set points for EditMode tests but must not change the tariff and must not toast.

New game / `ResetCaravan`: points are 0, rank is 0, and every `kharunFlipSeen` flag is false. A soft reset (if supplies has landed) **keeps** points, rank, and the first-flip marks — do not zero them in `ApplySoftReset`. Influence, shelves, and patrons stay as that task already specified.

**Save / load.** Persist each unit's `Basis`, Kharûn `points` (rank may be derived on load), and the `kharunFlipSeen` marks through whatever session save / snapshot already exists (search for it: Continue, `PlayerPrefs`, a session DTO, `JsonUtility.ToJson`, and so on). Missing `Basis` on old cargo loads as −1; missing points and flip marks load as 0 / false. If Continue is still hidden and there is **no** session save path yet, do not invent a full save system in this task; keep the fields on the session (and through a soft reset) so a later save task can pick them up, and mention that in `Vision.md`.

## 4. Tariff

`TariffPercent` becomes: 0 if `House` is null, or the holder is null, or the holder is House Kharûn; otherwise `tariffBase` (12). Drop the `standingOf` term and the floor. Leave `CutCoin`, `BuyPaid`, `Net`, and the 15% spread alone.

Update or replace `RivalCutFallsFromTwelveToFour`. Standing rank must no longer change the cut. The locked behaviour is: a rival-held town (e.g. Draven after `AddPresence` for Zamath) is **always 12%**, including at Kharûn rank 10; Kharûn itself, a Kharûn-held village or post, and a town with no patron are **0%**. Morning influence decay (`× 0.75`) can stay asserted in that test or a sibling.

## 5. Map-piece hook (do not build map pieces)

Add a public `int GetKharunRank()` on `CaravanSession` and a public points getter. Leave a clear `TODO` next to the API: a later task will sell the six pieces at Kharûn's Tavern, gated by ranks 1 / 3 / 5 / 7 / 9 / 10, with leads discounting the price (rank gates; it does not discount). Do not implement the Tavern, list prices, leads, or the Westmark gate.

## 6. Card standing effects

Wire a Kharûn standing-points effect on the encounter resolver (flat points through `AddKharunPoints`). The allowed range is 0 to +25; there are no standing losses. Batch 1 has no standing outcomes; do not invent any on those 16 cards, and do not edit CZ-03 or CT-01 (those are map-piece leads, not standing).

If, at build time, these cards exist in the loaded encounter data (batch 2 may still be absent), apply Systems' v2 mapping and do not leave the old standing losses:

- CK-01 "The Factor's Questions": a1 House Kharûn **+10**.
- CK-01 b1, CZ-01 "The Rod Dips" b2, and CG-02 "The Nectar Runner" b2: **remove the standing loss**. Replace it with a placeholder **10-coin fine** (`Coin` drops by 10, floored at 0). Systems' notes left the replacement TBD (−1 h delay or a small coin fine, no number); this task uses 10 coin so the outcomes still do something without taking standing or inventing a delay. Do not also add a delay.
- CK-01 b2: **no effect** (flavour only). Strip the old standing loss and add nothing.

If those ids are not in the card file, leave the helper in place and do **not** invent the cards or copy batch-2 prose into batch 1.

## 7. Display and rank-up toast

Show the Kharûn rank, and progress toward the next rank, on a visible dock panel in normal play, without turning the hidden House tab back on.

Put a compact cream / ink line on the **market / town panel** (for example under the market frame header or above the shop columns), `UiType.Small` or similar, using `EconomyCatalog.HouseName` so Kharûn keeps its circumflex. A clear shape is `House Kharûn  ·  Rank 5  ·  40 / 100` (points inside the current rank, over 100). At rank 0, show `Rank 0` and progress toward 1. At rank 10, show `Rank 10` and no next-rank fraction. Keep this **off the top bar** — `15_market_ridgewatch.png` is specifically the long dock name, and the supply readout (if supplies has landed) already uses that cluster. Do not rely on the House tab: `HideUnfinished` still hides it, and jobs are still absent. You may replace the House stub with the same rank line plus a short "Jobs are not in this slice." only if that tab is already visible; it is not the required readout.

When the rank rises, show a short on-screen toast, not a modal, with the exact placeholder text:

`Your standing with House Kharûn has risen to rank N.`

`N` is the new rank. Reuse the supplies notice hook if one exists; otherwise a short fade line on the market or dock is enough. It must not cover the map, must not use the wood pause panel, and must clear in `ShotClearOverlays()` (and whatever intro / encounter / rescue overlay-clearing earlier tasks added). No toast on test setters, and no toast on a new game.

## 8. EditMode tests

Add tests in `Assets/Tests/EditMode/PlayTests.cs` (or a sibling in that assembly). Use the existing helpers (`Leave`, `Ride`, `ShotDockAt`, `SetShelf`, `AddPresence`, `InternalsVisibleTo`). Add small hooks to read Kharûn points, rank, a unit's Basis, and `kharunFlipSeen`, and to grant a card/find unit at Basis −1.

Required:

- **Basis on a buy.** `TryBuy` stores `Basis == BuyCost` for that unit at that dock. A later sale of that unit uses that Basis, even if the local price has moved, and even if another unit of the same good was bought at a different price (`WorstStack` still picks the unit; that unit's own Basis is used).
- **Fills and pours award nothing.** `TryFillWater` / `TryFillFood` do not change standing. Removing a Water or Rations unit into the kit (if the pour path exists) does not change standing.
- **Profit awards points.** A profitable sale **anywhere** (Kharûn, a rival city, an unheld village, a held village) raises Kharûn points by `profit / (18 + 8 * rankBefore)`. A 1,800-coin profit at rank 0 reaches rank 1 (100 points), with no flips. Rival Houses never gain points.
- **Exclusions.** No sale points when: profit ≤ 0; `TakeVisitBuy` is true; Basis is −1 (the unit still sells for coin); the unit was poured away rather than sold. Control: buy in Kharûn, travel to a village, sell there, and Kharûn points rise.
- **First-flip bonus.** In an unheld village (Draven is the easy one) and in a post (Westmark), a player sale that makes House Kharûn the patron awards **+15** once, sets `kharunFlipSeen`, and may also add sale-profit points in the same call. A second player flip of the same place, after influence has drifted back, awards 0 and leaves the rank in place. An NPC-caused flip (or a hook that adds Kharûn influence and refreshes the patron the way an NPC or the morning tick would) awards 0 and does **not** set the flag; a later player flip of that place still pays. A city sale never sets a flip mark. A same-visit resale does not flip and does not pay the bonus.
- **Cap and never-drop.** Rank cannot pass 10. There is no standing-loss path; do not add a test that subtracts Kharûn points. `GetKharunRank()` matches the cap.
- **Tariff.** Rival-held is 12% at rank 0 and at rank 10. Kharûn, a Kharûn-held place, and an unheld place are 0%. Update or replace `RivalCutFallsFromTwelveToFour` so it no longer expects standing to cut the tariff or read `standingMax` / `TariffFloor`.
- **Save shape, if a path exists.** A bought unit's Basis, non-zero Kharûn points, and at least one first-flip mark survive a save-and-load. An old unit without Basis loads as −1 and awards no standing. If there is no save path, skip this case and cover the in-memory fields plus the soft-reset keep instead.
- **Soft reset, if supplies has landed.** After a desert death and Continue, points, rank, and first-flip marks are still there; cargo (and its Basis) is gone.

Update any existing test this behaviour breaks (`SetStanding`, `TariffPercent`, `StandingMax`, `TariffFloor`, buy-then-sell coin tests — those must still not print coin). If a test fails, fix the production code or the stale assertion, not the invariant.

Run **all** EditMode tests (PlayTests and FrontDoorTests) and leave them green.

## 9. Re-capture screenshots

This is a UI task (dock rank readout and rank-up toast). After the behaviour works, run **Caravans/Capture UI Screenshots** in `Assets/Editor/UiScreenshots.cs` and commit the refreshed `Screenshots/` set, including the regenerated `Screenshots/index.md`.

Update existing market state lines (`market-empty`, `market-buy`, `market-sort`, `market-wagon`, `ridgewatch`, and any supplies HUD shot on the market) so they mention the new Kharûn rank line and its progress fraction, and confirm it is readable and does not clip the shop, the detail column, or the Ridgewatch name on the top bar.

Add hooks and shots, and hide the toast in `ShotClearOverlays()` (and in whatever intro / encounter / rescue overlay-clearing earlier tasks added) so it does not sit on later frames:

- `ShotOf("standing-ranks", "42_standing_ranks.png", "SampleScene", "Market at Kharûn. The town panel shows House Kharûn, a forced mid rank (e.g. Rank 5), and progress to the next rank (e.g. 40 / 100). Cream / ink text, circumflex on Kharûn, no toast covering the grid.", Kind.Play, Kind.Play)`
- `ShotOf("standing-rankup", "43_standing_rankup.png", "SampleScene", "Rank-up toast over the market or dock: 'Your standing with House Kharûn has risen to rank 1.' Short toast, not a modal, hide-and-bone play chrome, not the wood pause panel.", Kind.Play, Kind.Play)`

Force-show the toast even if the live session would not have opened it (same spirit as `Shot("intro")` / `Shot("encounter")`). Confirm in the shots: rank and progress visible, toast text fully readable, controls unclipped.

Update the `Notes` string that `WriteIndex()` appends: the market / town panel now shows standing with House Kharûn and progress to the next rank, and a short toast fires when the rank rises.

## 10. Docs

The build changes how the game runs, so update `Vision.md` so it still describes the game as it runs. Patch, do not revert, whatever Intro, Encounters, and Supplies already wrote.

In particular, replace the living-market sentences that say standing starts at nothing and play has no way to raise it, and the `prices.json` line that names a standing cap of 8 and a 4% tariff floor. Say: the merchant is House Kharûn; standing exists only with House Kharûn, rises from profit on each sale (net after tariff minus that unit's cost) and from a one-time +15 on the first player-caused flip of each village or post (15 places), never from losses, same-visit resales, kit pours, rations, or card and find goods; 100 points per rank at 18 coin of profit per point, rising by 8 per rank already held; the cap is rank 10 and a reached rank never drops; a rival holder takes a flat 12% cut, and House Kharûn and a place with no patron take none (rank does not cut the tariff); the market / town panel shows the Kharûn rank and progress to the next, and a short toast fires on a rank-up. Mention `GetKharunRank` only if you need to say the later Tavern pieces will use it as a gate; do not describe map pieces or a Tavern as if they already play. The House tab may still hide jobs. Do not invent a bank, expeditions, or agents for hire. Do not introduce camel, wagon-as-vehicle, Eastmark, or hand-painted in the new sentences. If a starter bird is named, call it a thrask.

## 11. Finish

- Fix any compile errors.
- Run the EditMode tests and leave them green.
- Commit the code, the `prices.json` tunables, the tests, `Vision.md`, and the screenshot set with a clear message.
- Final step: delete every `Task_*.md` from the repo root (including this one and any old finished ones) and commit the deletions.

## Done when

- A profitable sale anywhere awards float Kharûn points using `profit / (18 + 8 × rank)` and 100 points per rank. There is no rival standing.
- The first player-caused flip of each of the 15 villages and posts awards +15 once, stored in `kharunFlipSeen`. NPC and decay flips do not pay and do not set the flag. Re-flips pay nothing, and ranks never drop.
- Basis is the coin paid on `TryBuy`, −1 on card/find goods, unchanged by damage, and carried on the unit until it is sold or lost.
- Losses, break-even sales, same-visit resales, kit pours, Rations, and unpriced units award no sale points.
- Rank caps at 10. `TariffPercent` is 12 in a rival-held town and 0 in a Kharûn-held or unheld town, with no rank term. `tariffFloor` and `standingMax` are gone.
- `GetKharunRank()` and a points getter are public. Map pieces are not built.
- CK-01 a1 is Kharûn +10, the three old standing losses become a 10-coin fine or (for CK-01 b2) no effect, only if those cards are in the data; otherwise the helper is ready and no cards were invented.
- Points, rank, first-flip marks, and Basis survive the session save path if one exists, and survive a soft reset.
- The market / town panel shows the Kharûn rank and progress to the next rank, and a rank-up toast uses the approved line.
- All EditMode tests pass. Screenshots and `Vision.md` match the as-built standing rules.

## Stop / don't

- Do not implement map-piece buying, leads, the Westmark gate, end screens, or achievements.
- Do not retune `merchantSpread`, kit sizes, daily consumption, road lengths, or map-piece list prices, and do not invent the thrask / duzhu / pincer buy flow in this task.
- Do not shrink the merchant spread with standing, and do not change how influence itself is awarded (village- and post-only, no same-visit resale) except to detect a first player flip after `RefreshPatron`.
- Do not load `standing.json` as a runtime config, and do not keep `tariffFloor` or `standingMax` beside the new keys.
- Do not invent batch-2 cards, do not rewrite approved card prose, and do not add a standing-loss effect.
- Do not turn the House tab back on only to show rank, and do not put the rank on the top bar.
- Do not reintroduce house select or standing with rival Houses.
- Do not sweep "wagon" out of the UI or code, and do not add wagon / wheel / axle / cart / camel wording.
- Do not rename Westmark, and do not edit `ProjectSettings.asset`.
