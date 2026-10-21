# TASK — Encounter system (placeholder table + shared card)

**Intent**  
Implement the encounter check and shared decision card so a road can fire a placeholder event. The table is 80 numbered IDs with blank effects. Flavour, outcomes, and full option text fill in a later lore pass.

**How it sits in the game**  
Travel already advances in hours with pace, weather stretch, and a travel window. This system adds one check per 12-hour portion of travel. It does not replace weather’s faceless leak or stretch. It does not introduce combat, a bandit sim, or a lost-pool. Socialize and expedition chains will reuse the same card later; this pass only needs the card and the road check.

**Chance and pool**  
- Base chance a check fires on a 12-hour portion: 50%.  
- Slow pace: –15% (35%). Fast pace: +15% (65%). Other modifiers (skills, cargo, day/night, weather) may be stubbed or omitted this pass.  
- If the check fires: select evenly at random among the tag categories that match the current route, then select evenly at random among the rows in the chosen category.  
- Only rows whose tags match the route enter the pool. Day/night is a boolean on the ID and filters the pool further.  
- Weather categories (Clear / Heat / Wind / Sandstorm) are eligible only when that weather is the resolved term on the leg.  
- Rival-houses rows are eligible only if the player has crossed paths with an NPC of that house on the current leg.  
- Ancient-mysteries rows are never drawn on ordinary road checks (expedition-only).  
- Floor: after modifiers some chance remains. No 0% road.

**Route tags**  
Tags gate the pool. Wilderness-desert, bandits, and catchall apply to every route. City tags apply to every route that touches that city. Wildlife-sea and wilderness-mountain apply only to the routes listed in `artifacts/Route_Tags.md`. Derek completes any remaining city routes in that file. Weather and rival-houses are runtime/conditional, not stored on the route.

**This pass delivers**  
- Shared decision card: title, short placeholder copy, Continue (or 2–3 stub options). No live DCs.  
- 80 placeholder IDs exactly as listed in `artifacts/Encounter_IDs.md`. Effects column stays blank.  
- Route-tag lookup that builds the eligible pool from the current route + day/night + resolved weather + crossed-path flag.  
- One check per 12-hour portion while the wagon is on the road. Uneventful majority.  
- Abstract hit stub only if an effect field is later filled; for now the card may simply Continue with no delta.

**Out of scope**  
Full event effects or flavour copy. Agent modifiers on the card. Pace/weather weighting beyond the 15% pace numbers above. Socialize table or expedition chains beyond card reuse. Attribute sheet, house gifts, or live odds.

**Handshake**  
Vision section “Travel, Weather, and Road Events” and “Events (distinct system)” are the source. Tunable chance numbers live in `data/config/` once the system exists. Placeholder IDs and route tags live in the two artifacts named above.
