# Caravans of the Cutting Sands — Vision

**Status:** Clean rewrite. This chat is the design room. The previous vision is reference only and is not imported by name.
**Engine:** Unity. New project. Not a port of the Godot scenes or scripts.
**Platform:** Steam, using an existing paid slot.
**Name:** Caravans of the Cutting Sands.

This document holds decisions that still describe the game. Anything not written here is unbuilt. Do not invent a second name for a system to fill a gap.

---

## Pitch

You are one merchant with one caravan in a harsh desert. You buy goods that exist because of this world’s plants, animals, and stone, then you carry them to somewhere that wants them. The trip is short in real time and dangerous in fiction. Rations, weather, and the people with you decide whether the cargo arrives. Profit becomes wealth. Finished work for the houses becomes standing. The game ends when that caravan is loaded for a journey off the western edge of the map, toward settlements no one on this atlas can dock.

The feeling to chase is the one from the old Dark Sun book *Dune Trader*: you know what this good is worth somewhere else, you stake coin and reputation on that, and you still have to cross the sand. Tone sits between Dark Sun and *Savage Sword of Conan*. Desperate, practical, competitive. Not a company simulator. You never own a second caravan, a warehouse chain, or a city.

---

## What the player does

1. Read a market. Buy what you believe will pay somewhere else.
2. Leave from the plaza. Travel on the map. Weather is visible and already acting on the route.
3. A leg may pass quietly, or it may open a panel over the map. The panel is a short choice. Your attributes, and anyone riding with you, change the odds. There is no separate combat screen.
4. Arrive, sell, restock water and food, listen for talk, or take a house job.
5. Leave people behind if you want eyes on a market you are not standing in. Bring people with you if you want help when a panel opens.
6. Repeat. Wealth is coin. Standing is the house relationship, paid out as work completed, not as a second currency you spend in a shop.

Failure costs cargo, coin, time, or the trip. You do not die for good. A ruined run pulls you back to a house seat. The cargo on that caravan may simply be gone.

---

## World

One desert map. Settlements do not grow. Populations are not simulated. Water, news, and a road someone will not rob are the real powers.

**House cities** (five). Each is the seat of one merchant house. The resident house always holds that city. Selling there does not change who holds it.

- Kharûn
- Zamath
- Thalor
- Veythar
- Ghorath

**Other settlements** on the current map. Some are villages, some are posts. The split is a rule, not a new name for the map: a village has a fuller market; a post is smaller, hungrier, and does not produce its own trade goods. Both can change which house is currently in charge, based on who has been selling there.

Villages with their own goods: Ashar, Moraq, Torvern, Draven, Sorel, Kethra, Neth, Rukh, Ghûl.

Posts, no origin goods: Westmark, Kaleth, Southmark, Highmark, Brineford, Ridgewatch. Highmark is where Ishka come to buy Draven’s cave fungus. It does not produce Ishka goods. Those move through Thalor.

Sarn’s Rest is a painted landmark, not a dock.

Two sea villages are not a normal road leg. Neth opens from Zamath, and Rukh opens from Westmark, with no travel time. Ghûl is a normal hop east of Southmark.

Location stories are not written in this pass. Each settlement carries the tag: Lore to be added in future update.

Westmark, Southmark, and Highmark keep those names. In this world’s language the ending means march, a border stop. It is not a system term.

Solid lines are the main roads. Dotted lines are the poorer tracks. Both are travel. How much harsher a dotted track is remains open.

**Houses.** Five. They are patronage and a limit, not city offices. You start in one house and may do work for the others. Glove-and-pouch custom is the trust sign between houses: coin from Kharûn does not survive a soak in brine, so a sealed pouch matters. Ghorath is the house that takes pressure the others will not. Safe-conduct for a house caravan is limited, not a blank pass.

You are not the house. The house can employ you, tariff you, and take you back if the sand ruins you. Sabotage of other caravans is not in this game.

---

## Coin and water

**Scrubstone** is the coin, and it is also the feedstock for pulling salt out of water. It is made only in Kharûn. A coin left in strong brine comes apart. Merchants wear gloves and carry house pouches. Kharûn itself is not rich in water. It can keep a street alive. Cheap water in bulk is a trade problem, not a local miracle.

Coin buys kit, travel supplies, and short hires. It does not buy civic rank or a second wagon. House jobs pay a fixed wage in coin.

Water and rations are their own market goods. You fill them at the plaza before a trip, at the local price. Some trade goods can be broken into rations at a bad rate. Which goods, and the daily eat rate, are not locked.

---

## Goods

Display names are locked. Weight, pack size, and base price are not. One listed good is one bought unit. Water and rations are not on this list.

**Kharûn** — Shimmersteel Jars, Tiger Cactus Flesh (edible), Khor Crafted Tools, Scrubcast Resin.

**Ghorath** — Dream Lotus Nectar (illegal to move openly), Crimson Eyed Melon Juice, Numbspindle, Mor Root.

**Veythar** — Moon Soaked Zethi-Bulb (edible), Titan’s Heart-rock, Shimmersteel Fineware, Vek-kilned Pottery. Veythar works shimmersteel. It does not mine it.

**Thalor** — Shimmersteel (raw, from the mine), Shimmersteel Cookware, Giant Duzhu Hide, Ishka Crafted Music Makers.

**Zamath** — Living Lumber, Water Witching Rods, Sweet Bliss Pomegranates, Brine Scrub.

**Ashar** — Palestone Bricks, Terrorshriek Talons.

**Moraq** — Dune Mollusk Pearls, Sunburnt Brew.

**Torvern** — Kelkris Feathers, Pincer Beast Fur.

**Draven** — Frost Iron, Finger Fungus (edible).

**Sorel** — Fire Salt (edible), Sweetrock (edible). Salt-flat goods. Not the same thing as Zamath’s Brine Scrub.

**Kethra** — Clack Fiend Larvae (edible), Clack Fiend Molt-skin.

**Neth** — Eyes of Arkhul, Reth’s Blood.

**Rukh** — Sarn’s Bane Liver, Kel Eel Flesh (edible).

**Ghûl** — Zar’Kun Sinew, Hulv Bone.

Shimmersteel grades, low to high: Kharûn jars, Thalor cookware, Veythar fineware. Thalor raw sits under all three.

No origin goods are written yet for Sarn’s Rest, Westmark, Kaleth, Southmark, Highmark, Brineford, or Ridgewatch. Posts may simply not produce origin goods. That list is not closed.

Smuggling, starting with Dream Lotus Nectar, is unwritten.

---

## Markets

Shelves are live. A settlement makes its goods up to a cap and eats some each day. Price follows how full the local store is, for buyers inside a distance band. Your purchase shrinks that store. Your sale fills the store where you sell.

These losses are on purpose. Nothing is owed back into a regional pool.

- Weather and a failed road or expedition check may destroy cargo or coin.
- Goods handed to a house as a job leave the world. They do not restock the market.
- Expedition loot that will not fit on the caravan is gone.

The log book remembers the best buy and the best sale you have on record, and where. It is not shared. Three feeds, and they stay distinct. Seen: you were in the market, stamped with the hour. Reported: someone you posted there saw it, fresh while they remain, stale if you pull them. Heard: socializing, or a rider on a rival caravan. Heard prices can be old or wrong, and the log says they were heard.

Other merchants use the same markets. Two seats a house. You hold one seat in your house, so nine caravans walk besides yours. They move small lots. Their sales in a village or post can shift which house is present there. Sales in a city do not. The extra seats from the old four-a-house roster do not walk.

---

## Standing, tariffs, jobs

**Standing** is separate from coin. It is tracked per house.

- Home house: up to 10.
- Each other house: up to 8.
- It rises by finishing that house’s jobs.
- A rival house stops offering jobs at 8. The home house keeps offering wage and experience after 10.

**Tariffs** apply in villages and posts only. Houses gain presence when their people, including you, sell there. The score is units sold times local value, higher when that good is scarce, lower when the store is full. It decays each dawn. The house with the highest presence is the sponsor. The market shows that house, your standing with them, and the cut. The cut applies to buys and sales. Standing lowers it. If your home house is the sponsor, there is no cut. That includes your home city. A rival sponsor is never free. Cities do not contest sponsorship.

**Jobs** are posted on the house board in that house’s city. A job is one good, one amount, deliver to the house, goods removed, fixed wage, standing, and experience. Home-house jobs pay better and remain at standing 10.

**Rooms.** Standing hangs objects in a room at your home seat. Five room shells, one per house, dressed as that city. You only open the room at home. Objects earned from other houses still appear there. Fifty objects exist in data (five houses, ten steps). One save can earn forty-two (home ten, four rivals at eight). A low step is a small curiosity. A high step is furniture. Objects do not change tariffs, market access, or safe-conduct. Steam achievements mirror objects you have actually earned. This reward track is decided in shape. Art can wait.

---

## People on the caravan

You upgrade the caravan. Better beasts change how you travel. Hired people change checks. They never hold the purse and they do not run their own trade.

The starter beast is a duzhu: about the size of a large camel, eight-legged, reptile, the ordinary merchant mount. Giant duzhu hide is an Ishka good exported through Thalor, not a mount you buy there. Pincer beasts, giant furred crabs, are the next pack tier. That tier is named and unbuilt. Kelkris are sand-burrowing feathered rodents that live off pincer-beast leavings. They are a Torvern good, not a mount.

You, and each hired person, advance on a skill tree in the Total War sense: a small set of choices, not a second character sheet. Experience is spent into that tree. What a person is good at is chosen, not rolled.

Each person is on one task:

- **Posted in a settlement.** They report that market’s stock without the caravan being there. This is how a route changes. Reports are prices and fullness, not rumours.
- **Riding with you.** They mitigate road risk. A guard helps when the panel is a threat. A factor helps when the panel is talk. A wrangler helps when the panel is the sand, the beasts, or a broken wagon.
- **Riding a rival caravan.** They write heard prices as that caravan docks. This is the main reason to care which rival is on the road.
- **Scouting an expedition.** They go on the ticket’s chain instead of sitting the road. The chain is still a skeleton.

Posts and riders share one cap. A soft cap rises as you spend experience in the tree. The hard cap is six, so six specialized people cannot light the whole map. The player does not take a cap slot by driving their own caravan.

Finding a person for the knowledge checks may stay on you until a scout seat exists.

---

## Attributes

Four checks. No second skill list. Names below came from the prior writeup and are the working set until you rename them.

| Check | Used for |
|---|---|
| Face | Talk. Once-a-day socializing. Rumour panels. Later, arguing a tariff. |
| Ken | What you already understand. Identifying a find. How useful a left-behind report is. |
| Knack | What you invent on the road. Adapting the wagon, a jury-rig, turning a wreck into something you can use. |
| Steel | Threat. Bandits, beasts, an armed standoff. Options on the panel. No hit points. |

Experience is one pool. It comes from jobs, expedition results, and later from road panels. On a level you place it into the four checks yourself.

Starting house is a package: small bonuses and penalties on the four checks, plus one gift that is that house’s method, not a bloodline. All five houses are valid starts. The wagon starts at that house’s city. The packages are not written. Do not stub them. Gifts that disrupt other caravans are out.

Until you ask for numbers, any check is a placeholder panel: title, short text, continue, optional simple result. Do not invent difficulties to unblock travel.

---

## Ending

The game has a designed stop. It is not the room, not a standing cap, and not the player noticing that upgrades have run out.

A trader once left the western edge, reached settlements that are not on this map, and came back. The stories disagree about the last known stop. Neth, Kethra, Westmark, and Sarn’s Rest are all named. The off-map places are talked about and show up as mislabeled goods. They are never dockable. The player does not see them.

Play is how you become someone who can follow that departure. Expeditions are the source of the unique pieces the western edge requires, and they also pay a real amount of coin. Ordinary trade pays for beasts, hands, water, and the load. House standing can fund the departure, tax it, or forbid it. The last prep is this caravan, filled and hired out. Not a second wagon.

When the western edge accepts the load, the game ends. There is no return trip and no off-map act. Credits roll on departure.

The piece list is not written yet. The rule is written: the pieces come from expeditions, they are not bought on a market, and a half-ready caravan cannot leave.

## Rumours and short expeditions

A rumour is a ticket, not a price hint. It names a settlement, a rating from 1 to 5, an expiry, and maybe a verified flag. You keep tickets on a list. They are not pins on the map.

Expeditions do two jobs. They pay coin, enough that a successful chain is worth more than a quiet trade leg. They also drop the unique pieces required for the western departure. Those pieces are not market stock and are not house-job rewards. A ticket that can drop a piece should be recognizable as such before you spend it, even if the story around it is unreliable.

Verification that raises the rating before you go is unwritten.

You spend a ticket at that settlement’s plaza. The expedition is a short chain of panels. The caravan does not animate a second trip. When it ends, the clock jumps by the time spent. Payoff is loot and experience from the rating. The ticket is used. Loot that will not fit is lost.

Length follows the rating: about one panel at 1, a few at 5. Failure still continues. Weather there may forbid some panels.

**Socialize**, at the plaza, once between dawns. Costs one watch. A Face check. You choose the success: a new ticket, or one more star on a ticket you hold (cap 5). A miss wastes the watch. Improving a ticket does not refresh its expiry. A new ticket must live long enough to reach its settlement on a clean run plus one delay. Local or nearby tickets may start higher. Weather on the path can shorten the useful life. A road panel may still hand you a ticket. Stars are how much it pays, not whether a market tip is true.

---

## Road cards

A road card opens over the travel map. It is a short encounter, not a second scene. Most legs are quiet. When a card fires, the tween pauses.

The card is also a lore door: a beast, a bandit, a supernatural thing on the road. Text for those comes later. Tag unused cards: Lore to be added in future update.

Each card offers two ways through, and the two ways use two different checks. The roll is a twenty-sided die plus the modifier from that check, and from a hired person who matches it. Odds are visible before the choice. There is no separate combat screen and no hit-point track.

Results may spend rations, water, time, or coin. The tutorial card spends coin only. Weather has already charged time and may already have ruined a faceless share of cargo. The card does not bill that again.

The tutorial leg, Kharûn to Draven, fires one fixed card at halfway. A giant horned goat-lizard blocks the route. Steel fights it. Ken feeds it. Both are locked successes, and each costs a little coin. The starting purse must cover that cost and still buy the jars. Later cards are not locked.

Expeditions are a series of these cards with no travel. They use their own screen: a static background for the place, then one card after another. They are not played on the route tween.

## Weather and the road

Weather is its own system. Masses move on the map. Clear, Heat, Wind, Sandstorm. The same mass reads the same on every route it covers.

When you commit to a leg, and again if a panel fires:

- Heat and Wind add time. A sandstorm adds more.
- A chance to lose some cargo, with no story attached.
- Which panels are legal. A sandstorm does not offer a polite conversation. A quiet leg through a storm is allowed. The storm already cost time and maybe cargo.

Waiting in the plaza lets the weather move. A sandstorm is the reason to wait. Weather does not rewrite market prices.

**Road panels** are separate. At most one per leg. Most legs are quiet. Currencies on a panel: time, a share of cargo, a share of coin, rarely a wagon condition. Losses are deletion, not a salvage ledger. Do not charge the same loss twice if weather already took a faceless share.

Bandits, beasts, salvage, finds, rescue, and talk are flavours of panels, not extra systems.

There is no perfectly safe road. Ruin is rare: the trip aborts to a house seat, and the cargo may be gone.

---

## Clock and screens

Time on the map is continuous. The caravan tweens along the route. The light follows the hour, so day and night are visible. Pause and speed are required. A tutorial callout freezes time.

The books do not tick every minute. Dawn is the pulse: markets make goods, markets eat goods, job dates and rumour dates are checked. Spoilage is not part of that pulse. A perishable good stores the hour it was loaded and the hour it turns. Sweet Bliss is the first of these: fresh, then toxic. A later caravan upgrade can add hours or a cool chest. Rumours and jobs store a deadline, not a watch cost.

Night closing the market is not in the first build. Dawn is the refill. Arrival does not freeze a leftover fraction of an hour. The top bar can name the hour. A three-hour watch may be a display word. It is not the simulation step.

Most of the game is spent docked. A location has its own screen. The map is not behind it. The clock is stopped on that screen unless the player chooses to wait.

**Top bar.** Where you are, the day and hour, coin, cargo. A gear for options.

**Banner.** Each settlement has a picture under the working panel, and its name. The banner is the flavour. It is not a scene you walk. Art can be tagged until a banner exists.

**Bottom tabs.** House, only in the five cities. Market. Plaza. Taking the road leaves this screen for the map. House is jobs and standing. Plaza is wait, fill water and food, listen, and start an expedition if a ticket names this place.

**Market.** About two-thirds of the screen. Two sub-tabs, Buy and Sell, not one mixed grid. Buy shows the settlement’s stock: icon, name, count, price. Sell shows the caravan: icon, name, count, what it will fetch here. A selected stack moves by an arrow or a commit button. Drag may do the same on a pointer. It is not the only control. Rearranging your own racks is free and does not spend coin. Buy and sell commit.

**Map.** The Wonderdraft map is supplied in the Unity folder. Routes are drawn by hand over it. It is the whole screen while travelling, and a panel while docked. Every settlement is already on it. Fog is active vision, not discovery: the map is shaded where the player has no eyes. Vision is a radius around the caravan, so on the road you see the road you are on. A posted person adds a radius where they stand. A rider on a rival adds a radius on that caravan. The radius is fixed. Weather and time of day change the caravan’s own radius only, as a cosmetic. Night and a sandstorm see less. Clear day sees farther. A posted person’s radius does not change with the caravan’s weather. When a town enters vision, or a posted person keeps it in vision, the log book updates from that market. Nothing else is drawn on the map. Seeing the destination early does not change the game. Decisions happen after docking. A report can describe what you cannot see from here, such as a sandstorm on a route. An encounter opens over the travel map. Opening character, rumours, or the caravan on the road pauses travel.

**Player pages.** Character, including the skill tree. Rumours. Map. Caravan, which is cargo and the log book when you are not in the market. The log book is the seen, reported, and heard prices.

Tutorial callouts point at these controls in order on the jars run. A new control in that stretch gets a callout, or it stays dark until the tutorial is over.

## Out of this game

- A fleet, or a warehouse company.
- Combat as its own mode, or an auto-battler on the road.
- Settlement growth.
- Sabotage or murder as player actions.
- A ledger that conserves every lost good into a hidden regional pile.
- A second caravan.

---

## Tutorial

The first session is locked to Kharûn. House choice is not on the new-game screen. It opens after the tutorial is finished, or if the player skips the tutorial from options. Skipping drops them at house choice with the same starting caravan the tutorial would have ended on, not at a finished delivery.

The walkthrough is a mask over most of the screen and a small callout aimed at one control. The callout says what that control does, then waits for the player to use it. Total War: Warhammer 3’s faction introductions are the reference: one control, one sentence, the rest dimmed. It is not a scroll of tips and not a second narrator in the market.

Buttons and features will still be added. Each new control that appears in the first session gets a callout entry, or an explicit note that it stays dark until after the tutorial. Do not leave a new button live and unexplained in the masked stretch.

The teaching trip is one delivery, two legs. Take Shimmersteel Jars from Kharûn to Draven. Draven already imports those jars. Bring Finger Fungus back. Fungus is Draven’s cave good, edible, and not shimmersteel. Raw shimmersteel is not the tutorial good. It was a placeholder that got left in place after the goods were named.

Coin, water, and how poor the start feels are not locked. The tutorial only has to make the jars affordable and the return trip survivable. The baseline purse is a later pass.

## What this pass does not write


Expedition scenes, location stories, and the piece list for the western edge are skeletons. A starting build tags them: Lore to be added in future update. Do not draft adventure text, settlement histories, or flavour paragraphs to fill the tag. Goods names and the travel exceptions above are data, not lore.

## First slice, not yet chosen

The trip is the game. A reasonable first proof is one route between two settlements, weather moving, one encounter panel, buy and sell, coin changes. The full map can be clickable around that. Attribute numbers, house gifts, and the authored event list wait until asked.

---

## Open

- Confirm or rename Face, Ken, Knack, Steel.
- Dotted track versus main road: extra water, harsher panels, or only a path.
- Tariff percents, dawn decay, and the scarcity band.
- Job list and the experience curve.
- Expedition length and loot by rating.
- Socialize odds. Verification of a ticket.
- Smuggling.
- The unique pieces the western edge requires: how many, which expeditions can drop them, and what “the edge accepts the load” checks besides those pieces.
- Weather loss rates, and which panels each weather allows.
- How far a posted person can see, and what they actually report about stock.
- Skill-tree nodes for the player and for hired people. Hard cap is six. Soft cap curve is not locked.
- Room art: five shells and fifty objects. Data can wait.
- Pack size, weight, and base price for every good.
- Daily food and water use.
- Pace and fatigue.
- Save and options.
- Which map settlements are villages, which are posts, and which produce no origin goods.

---

## Words that do not belong in this project

Do not use these for systems, screens, currency, or build tasks: chrome, mint, plate, mark, stall.

Market is the buy-and-sell screen. Stock is the goods on offer and how full the local store is. Plaza is where you wait, fill water and food, listen, and leave.

Settlement names that already contain mark stay. Coin is struck in Kharûn. Shelves are filled. The map is the map. A screen is a screen.
