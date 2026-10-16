# TASK: Travel window and pace

The caravan has a travel window of a fixed number of watches (starting example: 4). The window is shown on the top strip and depletes with any action that advances the clock.

When one watch remains, further clock-advancing actions are blocked until the player camps or rests. On the road this appears as a forced-camp card. In town the road buttons stay grey until a rest action is taken.

Pace is chosen at every start (leaving town, breaking camp, or resuming after a forced rest). Rush covers more ground inside the window but multiplies kit drain. Steady or Careful covers less ground and drinks less. Day travel and night travel are explicit choices at the same moment.

Certain stretches raise encounter weight for the mismatched half of the day. Blanket weather stretch and heat still apply. Camp restores the full window (zone or sandstorm penalties may still apply). Town rest restores it faster.

The window does not break the once-per-day Socialize lock. Window size, pace multipliers, and recovery rates live in `data/config/`.

Update `Vision.md` so the running description shows the window, the forced rest, the pace choice at each start, and the day/night option.
