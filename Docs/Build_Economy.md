# Build task — economy

For Grok Build on github.com/Darkgecko777/CaravansOfTheCuttingSands. Design room rules are in the vision. Do not import an older Godot writeup.

## Read first

- `Docs/Caravans_of_the_Cutting_Sands_Vision.md`. Price bend, spoilage, tariffs, and the nine walking caravans are written there.
- `Docs/Economy_Eat_Per_Day.csv` — dawn eat, one unit per day. A formula cell of `=1/3` is one music maker every third day. Nectar rows are an off-board sink, never a legal shelf.
- `Docs/Economy_Weight_Price.csv` — weight, pack, base price. A skimmed proposal. Use it as data. Expect it to change after a playtest.
- `Docs/Caravans_Goods.md` — names. Do not add origin goods.

## Keep

- `Assets/Resources/Map/upscale_map.png` and the Wonderdraft map. Do not generate a replacement atlas.
- `Assets/Scripts/JarsRun/` as the slice to extend. Do not fork a second game beside it.
- The jars-run teaching trip: Shimmersteel Jars from Kharûn to Draven, Finger Fungus back. The horned goat-lizard card. Vekra of Kharûn.
- Buy and Sell as two sub-tabs. One price for both. Tariff is a cut on the commit, not a second price.

## Change

Replace `ThrowawayEconomy.cs` as the source of truth. The 1.6-to-0.7 line is retired.

Price. One local price. No distance term. Ordinary shelf is three days of that dock's eat, multiplier 1. Empty is ×2. Six days is ×1/2. Those are caps.

A dock with no eat uses pack cap as its full shelf until a target is asked for. Zero stock is none on the board. Remember the ceiling price.

Dawn tick. Origin makes refill. A make that spends an input does not run if the input shelf is empty. Named spends: Kharûn 2 bars for jars, Thalor 1 bar and 1 heart-rock for cookware, Veythar 1 bar for fineware, Zamath 1 lumber for rods, Ghûl 1 spindle for the bone take. Eats come from the workbook. Anything else on a shelf sits until bought, sold, or spoiled.

Nine NPC caravans on the map, same roads, same weather. Two seats a house, player holds one. On arrival they buy and sell at once. Night closing is not in this build, so arrival is open. They do not draw road cards, smuggle, eat, drink, or spoil. Small shared cargo limit. They pick the load and dock with the highest profit per hour that fits. Profit is sale minus buy minus tariff. Presence is not in the score. A village or post sale still adds to that house's slice after the commit.

Presence is a pie. Each dawn every slice loses a quarter. Biggest slice is the patron. The player pays that house and no other. Base cut is a tenth. Standing halves it and no further. Home patron is zero, including the home city. A rival patron is never free. Cities do not contest.

Spoilage is a timestamp on player cargo. Ordinary edibles last four dawns. Sweet Bliss lasts two, then it is not sellable. Do not draw the spoiled face. Metals, jars, resin, hide, bone, wick, and worked shimmersteel do not age.

Banned boards. Nectar never legal. Melon juice hidden in Veythar and Thalor. Sweet Bliss hidden in city kitchens. Eyes of Arkhul hidden in Kharûn. A smuggle check can commit a hidden good at the dear price. NPCs do not take that check.

Weight and pack from the workbook limit what a wagon holds. Do not invent a second unit.

## Do not build

- Night closing.
- Wagon variety. One cargo limit for the nine.
- Room art, skill-tree nodes, expedition chains, location stories.
- A spoiled Sweet Bliss object.
- Sabotage, or NPC road cards.
- A second map.

## How to tell it worked

Start the jars run. Buy jars in Kharûn under the base bend. Wait or travel across a dawn. Kharûn jars climb back toward the ordinary shelf if the bars are there. Draven jars fall if no one delivered. A full Draven shelf pays less than an empty one, and an empty shelf remembers double base with nothing on the board.

Dock a village under a rival patron and see a tenth cut. Home city takes none. After sales and a few dawns the patron can change, because the pie lost a quarter at dawn.

An edible older than four dawns is gone the next morning. Sweet Bliss is gone after two. An NPC caravan is on a road, docks, and the shelf changes without the player trading. Its sale in a post moves a slice. It does not smuggle.
