# Session

The user posts a build instruction written on grok.com. Build that instruction. Check it against the game in code before changing code. Do not add work beyond the instruction. Do not keep a task list.

When the build changes how the game runs, update `Vision.md` so it still describes the game as it runs. `Vision.md` is a description for grok.com to read. It does not add features.

`Caravans_Goods.md`, `Name_List.md`, `Naming.md`, `data/markets.md`, the JSON sheets, and the economy sheets under `Docs/` are game data.

Do not use chrome, mint, plate, mark, or stall for systems, screens, or currency. Settlement names that already contain mark stay.

Tunable numbers live in `data/config/`.

When a build needs new or revised music, ambience, or sound effects, spawn one subagent for that audio and keep the rest of the build here. Start it before the other work. Skip the subagent when the instruction needs no new sound, or when an existing clip only needs to be wired in.

The subagent does not see this conversation. Put the sound in its prompt: the user's wording, what to make, length, and the landing rule below. It calls the audio-studio tools `generate_audio`, `edit_audio`, and `process_audio`. Those tools write only to `C:\Stability\audio-studio-mcp\outputs\`. Music and ambience stay instrumental. Sound effects use `audio_type` `sfx` with `sound_source`, `sound_action`, `sound_material`, and `sound_space`. Generation writes a 44.1 kHz wav and a json sidecar in that outputs folder. `process_audio` then writes the usable clip as wav in that same folder and leaves the generated wav and sidecar there. The subagent copies only that processed wav into `Assets/Resources/Audio/`. A requested sound effect ends as that one wav. The game loads it with `Resources.Load`. Call `unload_models` when the set is done. One subagent owns the set. Variations of one cue go in one `generate_audio` call. Do not run a second audio subagent at the same time. The local model holds several gigabytes.

Before wiring sound into the game, confirm the wav is in `Assets/Resources/Audio/`. If the copy is missing, copy the processed wav there. Wire that file.
