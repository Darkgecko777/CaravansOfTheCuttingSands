# Caravans of the Cutting Sands

This document describes the game as it runs. It is a description for grok.com to read. It does not add features.

The line under the title is "Buy on knowledge. Risk the journey."

The player takes a house and docks at that house's city with 400 coin and an empty wagon. Twenty places share one painted map. Profit is the gap between a full shelf and a thin one. The road spends the day. Food in the wagon ages. Other caravans are already buying the same goods.

## The world

Five cities are house seats. The house is named for the city.

- **Kharûn.** Scrubstone, contracts, quiet leverage. Scrubstone comes out of the ground and becomes coin. The city works the lowest grade of shimmersteel into plain jars.
- **Zamath.** Forest, witching rods, pragmatic deals. A shore city of sentient trees. Neth opens from here across the water, and the crossing adds no hours.
- **Thalor.** Proud, formal, dependent on the west. The shimmersteel mine, and the door for Ishka goods from the dunes north of the map.
- **Veythar.** Isolationist, militant, agrarian. Terraced farms. It works other people's shimmersteel into the highest grade.
- **Ghorath.** Debts, secrets, second chances. An oasis fortress. Dream lotus nectar is contraband wherever it is shown.

Nine villages sit off the seats: Ashar, Moraq, Torvern, Draven, Sorel, Kethra, Neth, Rukh, and Ghûl. Each has a market and a plaza. Six posts sit on the roads: Westmark, Kaleth, Southmark, Highmark, Brineford, and Ridgewatch. Rukh opens from Westmark the way Neth opens from Zamath. Sarn's Rest is a landmark, not a dock.

Shimmersteel is one metal, mined at Thalor. Work changes the finish. It stands in for bronze, silver, and gold, and it is not coin. Forty-one goods travel the map. Water and rations are made at every settlement, including the posts. A grade at each place sets how full those two shelves open and how fast they refill. An abundant grade opens cheaper. A scarce grade opens dearer. Posts mint no unique good. The canteen holds 75 litres. The ration bag holds 30 kilograms. That is three days for one person and one camel, at 25 litres and 10 kilograms a day. A new game starts with both full. Cargo water stays a trade good. Rations do not. The market does not buy or sell them. Fill food still buys one shelf unit into the bag. Ordinary food in the wagon still ages. The kit is what the caravan drinks and eats. Sea water is cargo drawn at Westmark, Neth, Sorel, Zamath, Ghûl, Kaleth, and Brineford. Its buy coin and its sell coin are that place's pure-water price. A sale adds one to the pure-water shelf and does not stock sea water. Those shores do not buy sea water. Ghorath and Rukh buy neither pure water nor sea water. The caravan does not desalt a unit.

Names and place stories live in `Caravans_Goods.md`. Weight, pack size, base price, and daily appetite live in `Docs/Economy_Weight_Price.csv` and `Docs/Economy_Eat_Per_Day.csv`. The playable game reads those two sheets for every good they list. Water, rations, and sea water are not on them. Water and ration price, weight, and pack are read from `data/config/prices.json`. Sea water uses that same pack, and its price is the pure-water shelf. Grades and the sea-water supply are read from `data/config/abundance.json`. The game does not read `data/lines.json`, `data/markets.json`, or `data/markets.md`.

## How a session opens

The title is a desert picture with a bed of blowing sand and a loop of wind. New Game, Continue, Options, and Exit are on the menu. New Game and Exit work. Continue is present and inert. Options stores a tutorial switch. A missing preference reads off. A new game leaves the switch unread.

New Game asks "Choose Your House" under the line "The market does not forgive the uncommitted." All five houses can be confirmed: Kharûn, Zamath, Thalor, Veythar, and Ghorath. Each card carries a letter, K, Z, T, V, or G, and a short flavour. Confirm opens free play at that city. Back returns to the title.

A run lives until play mode ends. Gear on the play screen says "Save is not in this slice."

## A day at a settlement

The top strip shows where the caravan is, the day and the hour, the coin, the canteen and the ration bag, and a one-line cargo summary: space used of 16, weight used of 256, and up to three goods by name. The kit reads as litres and kilograms against those caps. Gear sits on that strip. The row under it opens the logbook.

The settlement banner names the place and names who holds it. A city is held by its own house. A village or a post starts with no patron. The line shows the holder and the cut, or "No patron · 0%".

The bottom of the dock is the rooms. Market and Plaza are always there. House appears at a city. The house room says "Jobs and standing are not in this slice."

The market has Buy and Sell. A row appears when the shelf has stock, or when the wagon has that good. Rations are the exception: they stay off both pages. Each row shows the name, the count, the coin, and a Buy or Sell press. One press is one unit. A sell press does nothing when that place does not buy the good, and the unit stays aboard. A good the place treats as contraband carries that word on the row. Dream lotus nectar is contraband everywhere it is shown. Sweet Bliss is contraband in a city. Crimson Eyed Melon Juice is contraband at Veythar and Thalor. Eyes of Arkhul are contraband at Kharûn. An empty wagon on the sell page says "The wagon is empty."

The plaza, while the caravan is free, offers one road for each neighbor, "Wait an hour", "Fill water", and "Fill food". Fill water and fill food buy one shelf unit at the local price and pour it into the kit. One unit is 25 litres or 10 kilograms. The pour stops at the cap. A full kit takes nothing from the shelf and spends no coin. The unit does not go on the wagon. Listen answers "Socialize is not in this slice." Look at the map opens the map without leaving. The plaza also states the weather on the road ahead. Heat, wind, and sandstorm lengthen the leg. Cargo loss from weather has no rate. Weather does not change how fast the kit is drunk.

A buy spends coin and takes a unit off the shelf onto the wagon, if the purse can pay, the shelf has stock, and the wagon has space and weight left. A sale pays the local price times the unit's remaining integrity, after any cut, and puts the unit back on the shelf. Selling at a village or a post adds the player's house to the local influence. The house with the most influence holds the place. Each morning that influence falls to three quarters. A rival holder takes a cut of 12 percent. Standing can lower that cut, and the cut does not fall under 4 percent. Standing starts at nothing, and play has no way to raise it. The player's own house, and a place with no patron, take no cut.

## The road

Look at the map, or taking a road, shows the painted map. The player's wagon is a token. Nine rival wagons are smaller tokens, colored by house. Three weather masses move across the map: a sandstorm, a heat, and a wind. A fog of vision hides the rest. The radius is wider at noon, tighter at night, and tightest inside a sandstorm. The map itself cools toward night.

While traveling, the top strip reads "Road to" the destination. Pace is Pause, 1×, 2×, or 4×. The clock follows the distance. The clear Kharûn–Draven road is one day, and that day spends 25 litres and 10 kilograms. Heat and wind stretch a leg by about a sixth. A sandstorm stretches it by two fifths. A longer leg spends more kit only because the clock runs longer. A sea opening, Zamath to Neth or Westmark to Rukh, arrives at once and spends nothing. A dotted gap on a painted road adds no extra time. The leg line names the weather on that road. Waiting an hour spends one twenty-fourth of a day. At empty the kit stays empty.

Arrival docks the caravan. The rooms return.

## The market the world runs

Day 1 begins at hour 6, which is morning. Morning and night are hours 6 and 18. Midnight rolls the day. Waiting an hour, and time on the road, both cross those hours.

Each morning, every place eats from its shelves by the appetite sheet. Stock that hits zero stops being eaten until more arrives. Ordinary food in the wagon loses a tenth of its integrity. Sweet Bliss loses a quarter. A unit near zero leaves the wagon. Water does not age. A unit bought on a morning does not age again that same morning. Sale coin follows the worst unit of that good still aboard.

Each night, a place that produces a good makes more of it, up to a cap. The nightly amount scales with `productionScale` in `data/config/prices.json`, which is 1. Water and rations use that scale on the local grade instead of the shared make. Sea water is made only on the shore list, from the supply in `data/config/abundance.json`. Five workshops craft from stock on hand. The place, the goods, and the input counts are read from `data/config/workshops.json`:

- Kharûn turns shimmersteel into jars.
- Thalor turns shimmersteel and Titan's Heart-rock into cookware.
- Veythar turns shimmersteel into fineware.
- Zamath turns living lumber into water witching rods.
- Ghûl turns numbspindle into hulv bone.

A shelf's coin comes from how many days of local appetite the stock covers. Those bands are read from `data/config/prices.json`. An empty shelf asks double the base price. Three days of cover asks the base. Six days asks half. The figure on a buy row is that price plus any cut. The figure on a sell row is the integrity-adjusted price after the cut. Opening the market records those figures. It does not invent a price for a good that is absent from the shelf and from the wagon.

Nine rival caravans share the map: one of Kharûn and two of each other house. They buy a profitable load, travel at most two hops, and sell on arrival. A sale at a village or a post can move who holds the place. During a new game they trade everywhere. Their tokens are on the map. The player has no screen for their cargo.

Starting stock is thinner the more hops a place sits from a producer. A place six or more hops out starts empty of that good. Caps sit at six days of local appetite, or at a night's make, whichever is larger. Water and rations do not use that hop gradient. Their opening cover and their nightly make come from the local grade in `data/config/abundance.json`. A is six days and one and a half times the local appetite. B is four days and 1.15. C is three days and 0.85. D is one day and 0.4. The shelf price is still the cover curve. Sea water starts only on the shores that draw it. Elsewhere it starts empty, and a sale never adds to a sea-water pile.

## Tunable numbers

Play reads `data/config/` from disk when a session needs a rate. The files hold the numbers the running game already uses.

`consumption.json` holds the morning integrity loss: a tenth for ordinary food, a quarter for Sweet Bliss. It holds the daily ration appetite of 1, which the eat sheet does not list. It also holds the kit: 25 litres and 10 kilograms per day, a canteen of 75 litres, a ration bag of 30 kilograms, and a shelf unit of 25 litres or 10 kilograms.

`prices.json` holds the shelf bands, the nightly production scale, the rival cut of 12 percent with a floor of 4, the standing cap of 8, and the morning keep of three quarters of village and post influence. It also holds the water and ration price, weight, and pack.

`weather.json` holds the leg stretches. Heat and wind multiply a leg by 1.15. A sandstorm multiplies it by 1.4. A clear leg stays at 1, and that 1 is not a field in the file. The same file holds the vision radii: 0.34 by day, 0.18 at night, and 0.14 inside a sandstorm. Cargo loss is not in the file. The road does not apply one.

`travel.json` holds the starting 400 coin, the wagon limits of 16 space and 256 weight, and the clock. Morning is hour 6, night is hour 18, and a day is 24 hours. A clear day at 1× takes 36 real seconds. A sea opening still adds no hours on the player's clock. Rivals treat that opening as one hour when they score a trip, from `seaHourFloor`. The same file holds the 8 coin the lesson card holds back.

`rivals.json` holds the two-hop limit and the seed 1701 that places the nine rival caravans.

`workshops.json` holds the five recipes named above. It names places and goods by id, such as Kharun and ShimmersteelJars.

`abundance.json` holds the four grades and one row per place, plus the sea-water supply: shores start with 4, make 2 a night, and cap at 12. The grade letters are the locked table. A later tune changes the day counts and the make rates in that file.

There is no file yet for upgrade costs or check odds. The goat-lizard card has no odds. Both answers spend the held coin and clear the road.

## The logbook

A button under the date and the coin opens a near-full-screen chart of every good, in catalog order, including water, rations, and sea water. Each row keeps the lowest buy and the highest sale the market screen has shown for that good, and the settlement of each figure. An empty cell is a price not yet seen. A later cheaper buy replaces the low. A later higher sale replaces the high. Talk, rumours, and leaving someone to watch a market do not write a row.

## The jars lesson

The lesson remains in the session. A new game does not enter it. The canteen and the ration bag start full there too, and that road drinks them the same way. Skipping the lesson refills both when it resets the wagon.

If it is running, Vekra of Kharûn, the clerk, walks the player through one circuit. Buy Shimmersteel Jars at Kharûn. Take the road to Draven. Choose a pace. At the halfway point a giant horned goat-lizard blocks the road. Steel fights it. Ken feeds it. Each is certain and costs 8 coin, held back from the starting 400 until the card is done. The road then opens. At Draven, sell the jars, buy Finger Fungus, return to Kharûn, and sell the fungus. The wagon keeps that kit, and the player chooses any house and docks there. Skipping the lesson from Gear resets the wagon and opens that same house choice. Free travel of the Kharûn–Draven road does not stop for the goat-lizard. The title tutorial switch does not start this lesson.

## Current features

**Front door**

- Title with blowing sand, wind, and the line "Buy on knowledge. Risk the journey."
- New Game, an inert Continue, Options, and Exit.
- A saved tutorial switch that a new game leaves unread.
- House select for Kharûn, Zamath, Thalor, Veythar, and Ghorath, each confirmable.
- A confirmed house opens free play at that city with 400 coin, an empty wagon, a full canteen, and a full ration bag.

**At a settlement**

- Top strip for place, day and hour, coin, canteen litres, ration kilograms, cargo space, cargo weight, and up to three goods.
- Gear, which states that save is absent from this build.
- Logbook button on the row under that strip.
- Settlement banner with the place and the patron line.
- Market, Plaza, and, at a city, House.
- Buy and Sell pages. One press moves one unit. Rows are shelf stock or wagon cargo, with a contraband label where the place treats the good that way. Rations stay off both pages. A place that does not buy a good takes no sale.
- Plaza roads to each neighbor, wait an hour, fill water, fill food, listen, and look at the map. Fill pours one shelf unit into the kit, 25 litres or 10 kilograms, and stops at the cap.
- Listen states that socializing is absent. The house room states that jobs and standing are absent.
- Wagon limits of 16 space and 256 weight.

**On the road**

- Painted map with the player's token, nine rival tokens colored by house, and three moving weather masses.
- Vision fog that tightens at night and inside a sandstorm, and a map that cools after noon.
- Pause and paces of 1×, 2×, and 4×. The clock follows the road.
- A clear Kharûn–Draven leg of one day, which spends one day of water and rations. Heat and wind lengthen a leg. A sandstorm lengthens it further. The daily kit rate stays the same.
- Sea openings from Zamath to Neth and from Westmark to Rukh that add no hours.
- A leg line that names the weather. Weather does not destroy cargo.

**The living market**

- Prices from local appetite and stock: double when empty, the base at three days of cover, half at six.
- A rival patron's cut of 12 percent, never under 4. No cut for the player's own house or for a place with no patron.
- Morning appetite that consumes shelf stock, and a morning fade of village and post influence.
- Nightly production, scaled from `data/config/prices.json`, and five crafting workshops whose recipes are read from `data/config/workshops.json`.
- Cargo aging: ordinary food loses a tenth each morning, Sweet Bliss a quarter, and a spent unit leaves the wagon. Water stays whole. Sale coin follows integrity.
- Nine rival caravans that buy, travel up to two hops, sell, and can shift who holds a village or a post.
- Forty-one goods, twenty places, and five houses. Weight, pack, base price, and appetite come from the economy sheets. Water and rations come from `data/config/prices.json`. Grades and sea-water supply come from `data/config/abundance.json`. Sea water is priced and received as pure water.

**The logbook**

- A scrolling chart of every good.
- Lowest buy and highest sale actually shown on the market screen, each with its settlement.
- An empty cell for a price not yet seen.

**In the session, off the path of a new game**

- The jars lesson, the clerk Vekra, the pace prompt, and the goat-lizard card with Steel and Ken.
- Skip the lesson, from Gear, while that lesson is the running step.
- A second house choice after the fungus sale or after a skip, which keeps the wagon when the lesson was finished and resets it when the lesson was skipped.
