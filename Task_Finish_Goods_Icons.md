# Task: Finish remaining goods icons

Owner: Grok Build (background / unattended)
Pool: visual / Imagine
Input: GitHub Darkgecko777/CaravansOfTheCuttingSands @ d6cb1dc (main, "Nov 10, Start Point"), especially Caravans_Goods.md, Art_Vision.md, AGENTS.md, Vision.md, Assets/Resources/Goods/, Assets/Scripts/Play/WorldIds.cs, EconomyCatalog.cs, CargoSoundScreen.cs
Deliverable: transparent PNG goods pictures for every remaining unprocessed GoodId, shown on the ASSET TESTING Market tab, with accepted or proposed looks written back to Art_Vision.md
Stop condition: the 24 remaining goods pass the checks below and Art_Vision.md is updated; or a good fails the structural check twice in a row (record the fault, skip that good, continue). Do not implement pictures in the play market. Do not add goods. Do not invent perishable/fragile/hardy tags.

## Context

There are 41 locked goods. Every GoodId already has Assets/Resources/Goods/<GoodId>.png. Sixteen are accepted looks in Art_Vision.md. BrineScrub is already a clean transparent PNG but is not written back yet. Twenty-four files are still 1024x1024 RGB paintings on chroma-key green.

The bench loads Resources.Load<Texture2D>("Goods/" + good) into a RawImage. Cell names are Unity text. Play screens must stay unchanged.

## Do not touch

- The 16 accepted goods, unless the existing file fails the structural check: ClackFiendMoltSkin, FrostIron, GiantDuzhuHide, HulvBone, LivingLumber, MoonSoakedZethiBulb, MorRoot, PincerBeastFur, SarnsBaneLiver, ScrubcastResin, Shimmersteel, ShimmersteelCookware, ShimmersteelFineware, ShimmersteelJars, TitansHeartRock, ZarKunSinew
- Play screens, economy JSON/CSV, audio, UI chrome, mounts
- New goods, new ids, or new filename schemes

## Remaining work (24)

ClackFiendLarvae, CrimsonEyedMelonJuice, DreamLotusNectar, DuneMolluskPearls, EyesOfArkhul, FingerFungus, FireSalt, IshkaMusicMakers, KelEelFlesh, KelkrisFeathers, KhorCraftedTools, Numbspindle, PalestoneBricks, Rations, RethsBlood, SeaWater, SunburntBrew, SweetBliss, Sweetrock, TerrorshriekTalons, TigerCactusFlesh, VekKilnedPottery, Water, WaterWitchingRods

Also write the existing BrineScrub look into Art_Vision.md after a structural pass; do not regenerate it.

## Art rules

Read Art_Vision.md and the relevant Caravans_Goods.md entry before each picture.
- Full-size painting, transparent PNG, no background. Green is only a temporary key and must be removed.
- Whole object in frame with a clear even margin. Not a small sprite.
- No text, symbols, or labels in the picture.
- Use this world's materials (shimmersteel, frost iron, living lumber, Hulv bone, Zar'Kun sinew, Giant Duzhu hide, Clack Fiend molt-skin, scrubcast resin). No generic bronze, timber, or polished ivory.
- Keep construction simple: one object or one tight bundle. Every strap, rope, lid, or handle must attach. Coils and rolls must be one continuous form.
- After Imagine, run the Art_Vision structural check silently. On failure, describe the exact problem in one sentence, regenerate with a forced fix, then present.
- Do not blindly chroma-key the current files: some greens are brushy, many have contact shadows, FingerFungus has a cream frame, and green objects will be damaged by a naive key. Prefer a fresh, simpler generate-then-process pass.
- Every good below has a user-locked description (Nov 10). Follow it exactly; do not substitute the current green-screen drafts' forms (lute/flute, hammer-saw-chisel kit, glass lab vial, corked ceramic jar).
- Rations is cooked travel food, not an origin good. It is omitted from the Market bench; inspect the file itself and do not change bench behavior.

## Locked packing vocabulary

- Binding, ties, strings: Zar'Kun Sinew (one continuous cord, suckers visible, ends attached or tucked).
- Sacks, pouches, wraps: Clack Fiend Molt-skin (pale, oily, thin enough that contents almost show).
- Crates, hafts, wooden bodies: Living Lumber (moss-edged planed family, as on the accepted ShimmersteelJars crate).
- Liquid vessels: low-grade Shimmersteel (thick faceted crystal, sand-bronze lip), sealed with a Scrubcast Resin plug pressed into the mouth, drips joined to the lip.
- Off-list trade items may appear as packaging only (e.g. the water bladder). Never glass, cork, hemp, burlap, generic rope, generic timber, or bronze.

## Locked descriptions (24)

| GoodId | Icon description |
|---|---|
| Water | One large swollen beast bladder full of fresh water, neck tied shut with Zar'Kun Sinew and a sinew carrying sling. Raw bulk cargo, not a worn canteen waterskin. |
| SeaWater | The same sinew-tied beast bladder, with salt crust on the skin and a pale brine stain at the neck. |
| Rations | A Clack Fiend Molt-skin sack tied with sinew, hard bread, a pressed cake and a dense dried-stew block visible through it. No origin goods. |
| TigerCactusFlesh | A modest, dense, meat-like cut of reddish plant flesh, half-wrapped in molt-skin and tied with one sinew cord. Not leafy, not a whole cactus, no tiger stripes. |
| KhorCraftedTools | A sinew-bound bundle of a heavy pick-adze, a broad scraping blade and a short pry-bar, each with a dull gritty low-grade shimmersteel head on a Living Lumber haft. Practical, road-worn. |
| DreamLotusNectar | One small thick faceted shimmersteel vial of nectar, resin-plugged, loosely wrapped in a scrap of molt-skin and tied with sinew for smuggling. |
| CrimsonEyedMelonJuice | One squat, round shimmersteel flask, deep crimson juice visible through the crystal faces, resin-plugged. |
| Numbspindle | A small, light bundle of pale dried plant spindles bound with one sinew cord. Not a jar, not Mor Root kindling. |
| VekKilnedPottery | A moss-edged Living Lumber crate of kiln-dark clay vessels (tall jug, bowl, lidded pot) with fine incised geometric patterns. Clearly ceramic. No symbols or titans. |
| IshkaMusicMakers | A sinew-bound awkward mixed bundle: a Giant Duzhu Hide hand drum, a sinew-strung Living Lumber instrument body, and a short Living Lumber pipe. |
| WaterWitchingRods | One light single Living Lumber rod, faceted grain and a scrap of moss, cut clean at the grip. Not forked, not a beam. |
| SweetBliss | A handful of fresh, whole pomegranates in a loose molt-skin gather tied with sinew. Not split, spoiled or juiced. |
| PalestoneBricks | A small stack of pale bricks with glassy edges, lightly bound with one sinew cord. Stone-looking but light. |
| TerrorshriekTalons | A few oversized hooked talons, partly folded in a pale molt-skin wrap tied with sinew. Sharp and dangerous, not ornamental. |
| DuneMolluskPearls | A small sinew-tied molt-skin pouch, irregular matte sand-toned desert pearls showing through. Not round sea-oyster jewellery. |
| SunburntBrew | One tall, narrow shimmersteel bottle-jar of dark amber, smoky liquor, resin-plugged. |
| KelkrisFeathers | A nearly weightless sinew-tied bundle of irregular, mixed-length, soft rodent feathers. Not a tidy bird-quill fan. |
| FingerFungus | A light cluster of pale, hand-like cave blooms, fingers readable, loosely gathered with sinew. No frame, no background. |
| FireSalt | A dense rust-red flecked edible salt cake, half-wrapped in molt-skin and tied with sinew. |
| Sweetrock | A sinew-tied molt-skin pouch of pale honey-white salt crystals showing through. |
| ClackFiendLarvae | A moss-edged Living Lumber crate lined with molt-skin, holding large chitinous insect eggs. Edible cargo, not a nest scene. |
| EyesOfArkhul | A small molt-skin pouch, slightly open, a few irregular organ-smooth gizzard gems showing. Not cut jewellery, no eyes, no cult symbols. |
| RethsBlood | One small thick faceted shimmersteel vial of dark, dense gland ink, resin-plugged, unwrapped. No symbols. |
| KelEelFlesh | Cured sides of brine meat, sinew-bound, resting in an open moss-edged Living Lumber crate. Not a ration sack, not whole fresh eels. |

## Import and placement

- Overwrite Assets/Resources/Goods/<GoodId>.png using the exact PascalCase enum name.
- Update the matching .meta so it does not keep a stale 1024x1024 sprite rect on a differently sized PNG. Prefer the Default / no-mips / clamp / alphaIsTransparency import used by GiantDuzhuHide and PincerBeastFur, since the bench reads a Texture2D.
- FrontDoorImport.cs does not cover Goods; set import settings on each asset.

## Batching

Work in batches of 4-6: Water, SeaWater, Rations; TigerCactusFlesh, KhorCraftedTools; DreamLotusNectar, CrimsonEyedMelonJuice, Numbspindle; VekKilnedPottery; IshkaMusicMakers; WaterWitchingRods, SweetBliss; then the remaining village goods.

## Acceptance

For each file: transparent PNG; even margin; no baked text; matches the written brief; structural check passed; loads at Goods/<GoodId> on the Asset Testing Market tab (Rations: file-only check). Write each finished look into Art_Vision.md, plus the jar-seal and off-list-packaging rules. Report finished goods and skipped goods with their faults.
