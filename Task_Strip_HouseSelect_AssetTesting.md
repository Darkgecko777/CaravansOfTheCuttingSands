# Task: Remove House Select and the Asset Testing Scene

**Run after Task_UI_Declutter.md is finished and committed.**

## 1. Start every new game in Kharûn
- Remove the house select screen entirely. **New Game** goes straight from the title screen to the starting state at Kharûn.
- Hard-wire the starting house, settlement, coin, cargo and wagon to whatever the Kharûn start uses today, so the result matches what a player saw when they picked Kharûn.
- Strip out the code for the other house starts: house-choice data, alternate starting settlements and loadouts, the UI and its presenter code, and any save fields that exist only to record the chosen house.
- Keep the Kharûn house's identity anywhere it shows up in play (logbook, banners, dialogue).
- Delete house-select-only art from the build if nothing else references it. Leave the source art in the repo's art folders alone.
- Old saves: if a save stores a house choice, load it as Kharûn or ignore the field. Don't let it crash.

## 2. Remove the Asset Testing scene
- Delete the Asset Testing scene and remove it from Build Settings.
- Delete scripts, prefabs and menu items used only by that scene. Check references first. Anything shared with the main game stays.
- The new pipeline replaces it: the `Caravans/Capture UI Screenshots` tool and committed screenshots.

## 3. Clean up
- Fix compile errors and warnings this causes. No missing-script or missing-reference warnings when the scene loads.
- Update Vision.md (one fixed start in Kharûn, no house choice) and remove Asset Testing mentions from the docs.
- Remove the house select shot from the screenshot capture tool, re-run the capture, and commit the refreshed `Screenshots/` folder.

## Done when
- New Game drops the player straight into Kharûn.
- No house-select or Asset Testing code, scenes or menu entries remain.
- The project compiles clean and the new screenshots are committed.

## Final step: clear the task files
- When everything above is finished, delete **every** `Task_*.md` file from the repo root (this one, plus any finished or old ones still there, like Task_UI_Declutter.md, Task_UI_Screenshots.md and Task_UI_Cleanup_Pass.md).
- Don't move them somewhere else in the repo. Just delete them, and include the deletions in the final commit.
