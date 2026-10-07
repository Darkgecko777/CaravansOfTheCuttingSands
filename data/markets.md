# Market sheet

`markets.json` is the sheet you edit. The game does not read it yet.

`empty_multiplier` and `full_multiplier` are the ends of the price slide. Empty shelves pay more. Full shelves pay less.

A town `makes` the goods produced there. `wants` is that town's appetite for a good. Appetite multiplies the price and the rate the shelf is eaten.

- A good the town makes uses 0.8 until you change it. The shelf stays cheaper and is eaten more slowly than it is made.
- A good the notes already say the town wants uses 1.4 until you change it. Deliveries are eaten quickly and the price sits higher between them.
- A good in neither list has appetite 1. The town does not seek it. Stock changes only when a caravan sells it, or when you add it to the sheet.
- These two numbers are stand-ins. Change them in the JSON.

`needs` records an input and is not used. Worked shimmersteel sits there so a later pass can stop production when the metal has not arrived. Eating a good faster does not do that job.

Dream Lotus Nectar has no row. It is not on a legal board.

Production amounts are not in this file. Water and rations are kit, not rows on this sheet.
