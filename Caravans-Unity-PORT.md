# Port brief — Caravans of the Cutting Sands

Instructions for a Grok Build session opened **inside the Unity project**. This file is the scope. The Godot checkout is the behavior. Do not rebuild the design in prose, and do not treat this file as a second game.

Move this file to the Unity project root and name it `PORT.md` before the first Unity session.

**Godot source (read-only):** `C:\Users\drdis\Documents\Godot Projects\caravans-of-the-cutting-sands`

The Unity session must not edit that tree. If a line in this brief and a function in that tree disagree, the function wins. If a comment and the function disagree, the function wins.

## First message

Paste this as the opening turn in the Unity folder:

> Read `PORT.md` at the project root and follow it. The Godot checkout at `C:\Users\drdis\Documents\Godot Projects\caravans-of-the-cutting-sands` is read-only behavior. Port slice 1 only. Do not edit the Godot tree. Do not start a later slice.

Each later session names the next slice the same way. One slice at a time.

## What this is

Official title: **Caravans of the Cutting Sands**. Owner: Derek. He playtests in the Unity Editor on this machine. Do not describe the work as a demo, a vertical slice, or a proof of concept.

The Godot game is Godot 4.7. Play starts at `scenes/ui/title_screen.tscn`, then house select, then `scenes/map/field_shell.tscn`. A new game opens in Plaza at the chosen house's home, with no top tab open. There is one player wagon.

This port matches that playable path. Unity 6. UI Toolkit for every screen. A 2D project is enough. The reference frame is 1920×1080, stretching to fit the window the way Godot's canvas-items expand mode does.

Use the Unity skills already installed for this machine. Create scenes and assets through the Editor. Plain C# classes replace Godot `RefCounted` books. One run object replaces the `GameState` autoload. C# events replace Godot signals. A coroutine or tween replaces the hop tween. Do not recreate a Godot autoload for every book.

## What to read, and in what order

1. This file, for scope and for the slice in progress.
2. The Godot script named in that slice, and the JSON it loads.
3. `docs/State.md`, for the two shipped playtests and the gated list.
4. `docs/UI_Layout.md` and `docs/UI_Visuals.md`, when the slice draws the frame.
5. `TraderOfTheCuttingSands_Vision.md`, only the section for a lock Derek has asked to build. The current port is not that ask.

`docs/UI_Layout.md` names roles. It does not set the words on the controls. The words in the chrome table below are what this build says.

## Copy these

Copy the files. Leave every `.import`, `.uid`, `.godot`, and `.tscn` behind. Godot scene text is not a Unity scene.

| From the Godot tree | Into the Unity project |
|---|---|
| `data/world/*.json` | `Assets/StreamingAssets/world/` so the files stay loose JSON |
| `Assets/fonts/BonaNovaSC-*.ttf` | a UI Toolkit font asset, Bona Nova |
| `Assets/UI/instrument/` | stand-in plates, referenced by the style that replaces `InstrumentStyle` |
| `Assets/map/*.png` | the chart. The shell uses `Assets/map/cutting_sands_map_advanced.png` |
| `Assets/audio/wind_sound.wav` | the wind bed, when the audio slice is reached |
| Title and menu pictures | only the files `scenes/ui/title_screen.tscn` and `scenes/ui/house_select.tscn` actually reference |

`Samples/` is mood. It is not a component. Do not generate stand-in pictures. A missing ground is the dark ground color from `scripts/ui/instrument_style.gd`.

Load the eight JSON files at new game. Fail in the open if one is missing or fails to parse. Do not rewrite their schema. World facts stay in that JSON.

The files are `commissions.json`, `encounters.json`, `goods.json`, `houses.json`, `map_nodes.json`, `routes.json`, `settlements.json`, `strings.json`.

## Leave these

Do not port, and do not wire them back in:

- `scenes/main/city_hub.tscn`, `scripts/main/city_hub.gd`
- `scenes/map/world_map.tscn`, `scripts/map/world_map.gd`
- `scenes/main/main_game.tscn`

Play is one frame after house select. A feature is another tag on the book, or more work inside a desk that already exists.

## Do not build until Derek asks

These are locked in the vision and are not part of any slice below.

- Weather advancing while the player Waits
- House rank, one-way intrigue, personality traits on tokens
- Salvage, a lost pool, ruin extraction
- Stacked goods inside a cargo cell. The rack is 16 cells. One bought unit is one row.
- Skill webs, an agent roster, save, options, house check packages, gifts
- Smuggling, or any hidden channel on a good

Also leave alone: a second wagon, a fleet screen, a second economy, a rumour shop, fog of war on the chart, and any retune of the decision card.

## Words on the chrome

| Say | Where |
|---|---|
| Caravan, Map, Rumours, Character | Top tags, in that order. One open at a time. Each can close. |
| House, Market, Plaza | Bottom desks, in that order. |
| Rumours, and one entry is a rumour | The heard-things tag. Heard things are not marks on the chart. |

House is drawn in cities and in Ghorath. Ghorath has no rules of its own yet. Villages and posts draw Market and Plaza only. All three are dead on the road.

Plaza is the only place that confirms a hop, passes time on purpose, and restocks the canteen and the bag. Start, arrival, and a hop turned back all land on Plaza with no top tag open.

Title shows Continue and Options. Both stay visible and disabled. Title does not open the pause overlay for Options. The round seal on the play frame opens pause.

Engine names may stay in C# until a rename pass: Outyard, Word, wagon, Cargo, and the sight types `SightBook`, `WordBook`, `GoodCopy`. Do not put *assay* or *slip* on chrome, in player copy, or in new identifiers. Do not add an Agents tag.

## The frame

Follow `docs/UI_Layout.md` for composition and `docs/UI_Visuals.md` for the stand-in look. Outside pictures are not in this pass. Until they exist, controls use the plates in `Assets/UI/instrument/` through the port of `InstrumentStyle`.

From back to front: full-bleed ground, top paper banner, book with leather tags, borderless well, bottom paper banner. The well has no plate of its own. The Map tag shows the chart letterboxed in the well. Routes on that plate stay as drawn. Opening a tag during a hop pauses the hop. Closing it resumes.

Each stop will have its own ground, and the road will have its own. Until those pictures exist, a hop with no tag open is still the zoomed chart.

Type is Bona Nova. The place name is the largest type. A title is amber. Body type is bone. Colors come from `InstrumentStyle`: bone, amber, muted, the dark ground, brass.

Top banner, always: patron mark, where you are (while moving, where you are bound), the clock, one purse, how full the wagon is by count and by weight, one sky mark, one round pause seal. While stopped the sky mark is blank. While moving it shows that road's term from Clear, Heat, Wind, Sandstorm. The banner does not invent a condition.

## File map

Read the script when its slice starts. Port the behavior. The C# type names can match the Godot `class_name`.

| Godot | Job |
|---|---|
| `scripts/autoload/game_state.gd` | The run. Purse, rack, canteen, bag, clock, house, place, inventory. |
| `scripts/autoload/world_book.gd` | Loads `data/world/*.json`. |
| `scripts/autoload/cargo_hold.gd`, `scripts/map/cargo_math.gd` | Cells, mass, taps. |
| `scripts/autoload/market_book.gd`, `scripts/map/market_desk.gd`, `wagon_rack.gd`, `wagon_deal.gd`, `deal_style.gd` | The stall and the caravan rack. |
| `scripts/sight/good_copy.gd` | The line the stall prints. |
| `scripts/autoload/caravan_log.gd`, `scripts/map/map_well.gd`, `road_pressure.gd` | Hops, the chart watch, weather on the road. |
| `scripts/autoload/door_book.gd` | Sponsors, standing, the cut, delivery exp. |
| `scripts/autoload/rumour_book.gd`, `scripts/sight/word_book.gd`, `sight_book.gd`, `scripts/map/word_desk.gd` | Rumours, Socialize, expeditions. |
| `scripts/autoload/character_book.gd`, `scripts/map/character_desk.gd` | Face, Ken, Knack, Steel, level, exp. |
| `scripts/autoload/check_roll.gd`, `scripts/map/placeholder_card.gd` | The decision card. d20 plus one check. Odds shown. Do not retune. |
| `scripts/autoload/encounter_book.gd` | At most one authored road card per hop. |
| `scripts/autoload/string_book.gd` | The 19 rival tokens in `strings.json`. |
| `scripts/map/field_shell.gd`, `scenes/map/field_shell.tscn` | The one play frame. Largest file. Port it with its slice, not ahead of the books it calls. |
| `scripts/ui/title_screen.gd`, `house_select.gd`, `pause_menu.gd`, `instrument_style.gd` | Title, the five doors, pause, stand-in plates. |
| `scripts/ui/sand_bed.gd`, `scripts/autoload/audio_manager.gd`, `stream_fade.gd` | Title sand and the wind bed. Last. |
| `scripts/map/zone_style.gd` | Yard styling helper. Read it with the frame slice. |

`project.godot` autoloads only `GameState`, `AudioManager`, and `PauseMenu`. The other books are `class_name` types those three and the shell call.

## Numbers to preserve

Confirm each one in the script before coding it. The table is a checklist, not a second source.

| Rule | Value | Where |
|---|---|---|
| Purse at new game | 500 | `GameState.STARTING_SCRUBSTONE` |
| Rack | 16 cells, 256 mass | `STARTING_CAPACITY`, `STARTING_MASS` |
| New rack | empty | no letter-catalog seed |
| Canteen and bag | 128 L, 16 portions | `CANTEEN_LITRES`, `BAG_PORTIONS` |
| A normal start | 64 L, 8 portions | `NORMAL_LITRES`, `NORMAL_PORTIONS` |
| Drink, road and Wait | 8 L and 1 portion a watch | `LITRES_PER_WATCH`, `PORTIONS_PER_WATCH` |
| Water lot tap | 10 L into the canteen, 2 L wasted | `TAP_IN`, `TAP_WASTE` |
| Edible tap | 1 ration portion, 25 integrity lost | `cargo_hold.gd` |
| Clock | 8 watches, 3 hours each | Dawn, Morning, Heat, Afternoon, Dusk, First night, Deep night, Predawn |
| Market open | watches 1–5 | `MARKET_LAST_WATCH` |
| Wait | 1 watch, half day, full day | Plaza only |
| Standing cut | 12, 11, 10, 9, 8, 7, 6, 5, 4, then further rungs in the array | `DoorBook.CUT_BY_STANDING`. Own house as sponsor charges nothing. Villages and posts only. |
| Own house standing at start | 1. Every other house 0. | `DoorBook.seed` |
| Delivery | 1 exp | `DoorBook.DELIVERY_EXP` |
| Expedition | exp equal to its stars | rumour / expedition script |
| Missive | 2 of `frost_iron` | `MISSIVE_GOOD`, `MISSIVE_QTY` |
| Kharûn checks | Face 1, Ken 0, Knack 1, Steel 0. Level 1, exp 0. | `houses.json` plus `CharacterBook.apply_start` |
| Level-up | a manual +1 | `character_desk.gd` |
| Card odds | shown as faces in 20, clamped so the die need stays between 2 and 20 | `check_roll.gd` |
| Catalog id | snake case of the locked name. Stall prints the `name` field. | `goods.json` |
| Water and Rations | those ids and names stay | `goods.json` |

Region-pair weather is frozen at New Game. Heat or Wind adds one day. Sandstorm adds two. Arrival leaks water. There is no coin tithe. Road-card weights live in `encounter_book.gd`: Clear 25, Heat or Wind 30, Sandstorm 35, across the authored rows. Never a 0% road. Bandits tithe. They do not murder the mark.

Cities and villages mint rations and water. Posts do not. Sarn is painted only: no mint, no dock. Warehouses produce and consume at Dawn. Live prices exist only at the stall where the wagon stands. The stall ledger remembers the lowest buy and the highest sale, and the town of each.

Kharûn's first start is Vorek's missive. The Draven edge is Heat with no leak. One scripted Knack card fires on the way out. Read that path in `field_shell.gd` and `game_state.gd` rather than reconstructing it from this paragraph.

## Slices

Finish one. Derek plays it in the Editor. Then start the next. Do not scaffold later slices "so the hooks exist."

### 1. Project and JSON

Unity 6, 2D, UI Toolkit. Copy the JSON and the fonts. Load all eight files on play. A temporary label may show the house count and the good count.

Done when Play prints a successful load, `frost_iron` resolves, and Water and Rations still use those names. A missing file stops the load in the open.

### 2. Title and house

Title, then five doors from `houses.json`: Kharûn, Zamath, Thalor, Veythar, Ghorath. Continue and Options visible and disabled. New Game applies that house's checks. Kharûn is 1/0/1/0 at level 1 with 0 exp. Other houses use the checks in JSON, or zeros when the block is absent.

Done when New Game, House Kharûn, reaches an empty play scene seated at Kharûn's home.

### 3. The frame

The banners, the four tags, the three desks, and the stand-in plates. Wire the top-banner facts to the run. Map shows the chart, letterboxed, with no fog of war. House hides where the Godot shell hides it.

Done when the tags open and close one at a time, Plaza is the pressed desk, and the top row reads Caravan, Map, Rumours, Character.

### 4. Clock, Plaza, travel

Wait. Market hours. Adjacent hops. The travel watch and the arrival caption. Pause the hop while a tag is open. Weather term on the road. The frozen region-pair adds. The arrival leak.

Done when a hop from the home seat to a neighbor leaves from Plaza, arrives on Plaza with no tag open, and the canteen and the bag have paid the watches.

### 5. Market and the rack

Buy and sell at the stall you stand in. The cut. Restock. Taps. Empty starting rack. Cell and mass limits. Dawn stock when a hop crosses Dawn. The ledger.

Done when the goods-catalog playtest below passes, and a village sponsored by the player's house shows cut 0% while a rival sponsor still shows a cut.

### 6. Rumours, Character, the card, the missive

The rumour list. Socialize. Expeditions. The decision card, used by Socialize, expeditions, and the road. Character tag with a manual +1. Vorek's missive from the Kharûn start through delivery. The 19 tokens moving on the clock. The Map tab keeps the committed stamp. The travel watch slides the current watch.

Done when the plaza-rules playtest below passes, including 1 exp on delivery.

### 7. Pause and the wind

The round seal opens the pause overlay from the play frame. Title Options still does not. Port the wind bed the way `audio_manager.gd` and `sand_bed.gd` actually behave.

Done when pause opens and closes over the frame, and the title screen is unchanged.

## Playtests

Run these in the Editor. The Godot wording is the expected result.

**Catalog.** New Game, House Kharûn. Open Market. The four origin rows read Shimmersteel Jars, Tiger Cactus Flesh, Khor Crafted Tools, Scrubcast Resin. No `Kharûn A`. Water and Rations are still named Water and Rations. Vorek asks for two Frost Iron. At Draven, before the buy, Plaza says Frost Iron is not aboard. Open Market at Ghorath. Dream Lotus Nectar is listed, and its line says it is not legal plate. Buy still works. Open Market at Draven. Frost Iron and Finger Fungus. Finger Fungus offers Tap into the bag. At Veythar, buy Titan's Heart-rock until the rack stops. Weight fills before the 16 cells (2 cells, 56 weight). Hulv Bone at Ghûl is 4 cells and 72 weight.

**Plaza.** New Game, House Kharûn. Plaza is the pressed seal, and the top tag reads Caravan. Vorek names Market, Caravan, Plaza, then House. Restock and the Draven road are on Plaza. Draven opens on Plaza. Buy two Frost Iron. The road home opens on Plaza. Deliver. The wage is net of the advance, standing moves, and exp is 1. A village sponsored by House Kharûn shows cut 0%. A village sponsored by another house still shows a cut. Ghorath still shows House.

## Working rules

- Smallest change that finishes the slice. Match the Godot behavior where the player can see or count it.
- Do not commit unless Derek asks.
- Do not add a design doc, a second brief, or a README inside the Unity project.
- After a slice, name what Derek can click. Do not claim a playtest passed unless the Editor showed it.
