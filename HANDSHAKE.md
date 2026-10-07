# Handshake

Read this file for progress. Then open only the file named for the question you have. This is not a second design. The design is `Caravans_of_the_Cutting_Sands_Vision.md`. Goods names and bulk are in `Caravans_Goods.md`.

If this file and another document disagree, the other document wins for anything it already locks. Decisions listed under "Decided since the vision" are the newer word where they differ from the vision. If a chat and a file disagree, the file wins.

## Playable now

Press Play on SampleScene. The lesson starts at Kharûn. Buy Shimmersteel Jars, take the one open road to Draven, choose a pace, resolve the goat-lizard card, sell the jars, buy Finger Fungus, ride home, sell the fungus, then confirm Kharûn. Gear skips the lesson and leaves the starting caravan at house choice.

Prices, the eight-hour leg, and the weather on that road are throwaways in `Assets/Scripts/JarsRun/ThrowawayEconomy.cs`. The traced road is `Assets/Scripts/JarsRun/RoadPath.cs`. The screen is `Assets/Scripts/JarsRun/JarsRunPresenter.cs`. Rules are `Assets/Scripts/JarsRun/CaravanSession.cs`. No other road can be travelled. Cargo loss stays at zero.

## Decided since the vision

- Fair-weather travel time comes from painted length. The traced Kharûn–Draven road is one day. Another road takes that day times its own painted length divided by that road's length. Roads that are not traced have no time. Upgrades will multiply the time later. They do not change the measured length.
- The market is open from Dawn through Dusk. It is shut for First night, Deep night, and Predawn. Dawn still refills shelves. The lesson leaves while the market is open and arrives while it is shut, and the callout teaches Wait. This is not in the game yet.
- A dotted track raises the chance of the one road card. It does not add travel time. Until cards exist beyond the lesson, that chance is only a note. The teaching card is unchanged.
- Tariff in a village or post, when another house holds it: 12% at standing 0, one point lower each step, 4% at standing 8. Your own house charges nothing when it holds the place. Standing with another house stops at 8.
- Water and food are kit, separate from cargo. Cargo water and edible goods can be poured into that kit at a bad rate. Numbers are stand-ins until tested.
- Each of the four checks will start from 1 to 3. The package for each house is not written.
- Prices will be read from `data/markets.json`. The game does not read that file yet. The log book remains three feeds: seen, reported, and heard.
- Player sentences will be read from `data/lines.json`. The game does not read that file yet.

## Who edits what

Grok.com edits `data/markets.json`, `data/lines.json`, and, when a design lock changes, the vision. Add a sentence as one new object. Do not paste a rewritten file. Do not invent a sentence in C#.

This mode edits C# under `Assets/Scripts` when asked. It does not invent sentences or appetite numbers.

Do not read `Library/`, Unity scene files, or `Caravans-Unity-PORT.md`. Do not use these words for systems, screens, currency, or build tasks: chrome, mint, plate, mark, stall. Settlement names that already contain mark stay.

## Sentence shape

`data/lines.json` holds a list. One approved line looks like this:

```json
{ "id": "card.goat.title", "text": "The road" }
```

The id stays. The text is the sentence the player reads. A line is added only after it is accepted.

## Market shape

`data/markets.md` says what the fields mean. Edit numbers in `data/markets.json`.
