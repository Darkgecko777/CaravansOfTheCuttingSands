# Session

The user posts a build instruction written on grok.com. Build that instruction. Check it against the game in code before changing code. Do not add work beyond the instruction. Do not keep a task list.

When the build changes how the game runs, update `Vision.md` so it still describes the game as it runs. `Vision.md` is a description for grok.com to read. It does not add features.

`Caravans_Goods.md`, `Name_List.md`, `Naming.md`, `data/markets.md`, the JSON sheets, and the economy sheets under `Docs/` are game data.

Do not use chrome, mint, plate, mark, or stall for systems, screens, or currency. Settlement names that already contain mark stay.

Tunable numbers live in `data/config/`.

Asset work stays on the asset-testing bench until the user says otherwise. Work is one set and one item. Read `Art_Vision.md` before making a picture. Pictures are made with Imagine. After a picture is generated, run the structural check below before it is shown. Sound is made with the audio subagent below, and that subagent starts before the other work when a new or revised clip is needed. Only an accepted file is copied into the game. A goods picture lands at `Assets/Resources/Goods/<GoodId>.png`. A mount picture lands at `Assets/Resources/Mounts/<Name>`. A mount call lands at `Assets/Resources/Audio/mount-<name>.wav`. The ASSET TESTING screen is where the file is checked. The scene name stays CargoSounds. An accepted look is written back into `Art_Vision.md`. When the bench's behavior changes, update `Vision.md`. The play market does not gain item art unless the instruction says so.

After generating a picture, do not present it yet. Perform a silent structural check for physical continuity and topology.

- Connectivity: every strap, rope, cable, harness, or handle must attach to something. No floating rings, loose ends, or disconnected hardware.
- Continuity: rolls, coils, tubes, belts, and ropes must be single continuous forms, a true spiral or a true length. Not stacked concentric rings, separate segments, or broken loops.
- Contact and support: objects must rest on or attach to a surface. No unexplained floating parts.
- Completeness: the intended object is whole and coherent. No missing section where continuity is expected.
- Geometry: no impossible intersections, melted edges where parts should meet cleanly, or obvious perspective breaks on the main object.

If any of these fail, describe the exact problem in one sentence, regenerate with a corrected prompt that forces the fix, and only then present the result. A roll that has broken into separate rings is corrected with a prompt such as "continuous single spiral of leather, ends properly tucked, no separate rings." If the picture passes, present it normally.

When a build needs new or revised music, ambience, or sound effects, spawn one subagent for that audio and keep the rest of the build here. Start it before the other work. Skip the subagent when the instruction needs no new sound, or when an existing clip only needs to be wired in.

The subagent does not see this conversation. Put the sound in its prompt: the user's wording, what to make, length, and the landing rule below. It calls the audio-studio tools `generate_audio`, `edit_audio`, and `process_audio`. Those tools write only to `C:\Stability\audio-studio-mcp\outputs\`. Music and ambience stay instrumental. Sound effects use `audio_type` `sfx` with `sound_source`, `sound_action`, `sound_material`, and `sound_space`. Generation writes a 44.1 kHz wav and a json sidecar in that outputs folder. `process_audio` then writes the usable clip as wav in that same folder and leaves the generated wav and sidecar there. The subagent copies only that processed wav into `Assets/Resources/Audio/`. A requested sound effect ends as that one wav. The game loads it with `Resources.Load`. Call `unload_models` when the set is done. One subagent owns the set. Variations of one cue go in one `generate_audio` call. Do not run a second audio subagent at the same time. The local model holds several gigabytes.

Before wiring sound into the game, confirm the wav is in `Assets/Resources/Audio/`. If the copy is missing, copy the processed wav there. Wire that file.
