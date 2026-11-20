# UI screenshots

Captured 2026-11-20 at commit 9566b48 (working tree).

| File | Scene | State |
| --- | --- | --- |
| 01_title.png | TitleScreen | Main menu on the living-lumber wood panel. Text-only NEW GAME, OPTIONS, and EXIT. No Continue. Options closed. The sand is blowing. |
| 02_title_options.png | TitleScreen | Options open on the wood panel over the main menu. Master, music, and SFX sliders, fullscreen, resolution, and Show intro again as text rows, and Close. No bone frame. |
| 03_intro_card.png | SampleScene | First-run intro card centred over the Kharûn market. Header, body, tagline, and a text-only BEGIN. Play input is locked. |
| 10_market_empty.png | SampleScene | Market at Kharûn. No cell is selected. The wagon holds the seeded cargo. |
| 11_market_selected_buy.png | SampleScene | Market at Kharûn. A shop cell with stock is selected. |
| 12_market_sort_menu.png | SampleScene | Market at Kharûn. The sort menu is open. |
| 13_market_selected_wagon.png | SampleScene | Market at Kharûn. A wagon cell is selected. |
| 15_market_ridgewatch.png | SampleScene | Market parked at Ridgewatch, so the top bar shows that longer name. The wagon still holds the seeded cargo. |
| 20_plaza_inn.png | SampleScene | Plaza, Inn, at Kharûn. |
| 21_plaza_gates.png | SampleScene | Plaza, Gates, at Kharûn. No road is lit. Neighbour road buttons sit inside the gates panel. |
| 40_logbook.png | SampleScene | Logbook open over the market at Kharûn, between the bars. Seeded buy and sale figures are on the rows. An unseen price is a dash. |
| 41_pause_menu.png | SampleScene | Pause menu open over the Kharûn market. Wood panel, text-only RESUME, OPTIONS, MAIN MENU, and QUIT. Gear is visible on the top bar. |
| 30_travel_map.png | SampleScene | Gates at Kharûn with the Draven road lit. The painted trail and the destination circle are up. Neighbour road buttons stay on the gates panel. |
| 31_road_choice.png | SampleScene | Gates at Kharûn with the Draven road lit. Careful, Steady, and Rush sit on the panel. Embark is live. No card covers the map. |
| 32_departure.png | SampleScene | En route to Draven, paused at the Kharûn dock. The route line is drawn and the wagon has not moved. |
| 33_encounter.png | SampleScene | Encounter card open on the Draven road. The title is On the Road. |
| 34_camp.png | SampleScene | Make camp card open on the Draven road. |
| 35_resume_pace.png | SampleScene | Pace card after camp on the Draven road. Dawn and dusk are offered. The camp has advanced the clock. |

The encounter card, the camp card, and the after-camp pace card are part of play. They are 33_encounter.png, 34_camp.png, and 35_resume_pace.png. Play has no tooltip type and no separate dialog type.

21_plaza_gates.png is the gates with no road lit. 30_travel_map.png is those same gates with the Kharûn to Draven road lit. That gates view is the travel map the player opens, and the neighbour road buttons stay on its panel.

31_road_choice.png is the gates panel with the Draven road lit. Careful, Steady, and Rush are on that panel, and Embark leaves. No card covers the map. The pace card opens after camp, which is 35_resume_pace.png, in the order Careful, Steady, Rush.

House, Tavern, and Stables stay off the layout while ShowUnfinishedFeatures is off, so this capture does not include those rooms. Gear stays on the top bar and opens the pause menu, which is part of play. The title, title Options, and the pause menu use one wood panel and text-only items. Title Options includes Show intro again.

03_intro_card.png is the first-run intro card, one wood panel over the Kharûn market. It shows the header, the body, the tagline, and a text-only BEGIN. Play input is locked. Later New Games skip the card unless Show intro again is on.

15_market_ridgewatch.png is the long dock name. Sarn's Rest is a landmark. House, Tavern, and Stables are off the bar in this capture.

The caravan is House Kharûn. This run buys the first goods Kharûn will sell until four units are aboard, and it records the buy and sale figures the market would show. The clock stays at the new-game hour until the camp card.
