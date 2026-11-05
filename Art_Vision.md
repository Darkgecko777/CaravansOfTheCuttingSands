# Artistic vision

This is the visual reference for pictures. It is refined when an asset is accepted. It does not describe how the game runs, and it does not add features.

After a picture is generated, do not present it yet. Perform a silent structural check for physical continuity and topology.

- Connectivity: every strap, rope, cable, harness, or handle must attach to something. No floating rings, loose ends, or disconnected hardware.
- Continuity: rolls, coils, tubes, belts, and ropes must be single continuous forms, a true spiral or a true length. Not stacked concentric rings, separate segments, or broken loops.
- Contact and support: objects must rest on or attach to a surface. No unexplained floating parts.
- Completeness: the intended object is whole and coherent. No missing section where continuity is expected.
- Geometry: no impossible intersections, melted edges where parts should meet cleanly, or obvious perspective breaks on the main object.

If any of these fail, describe the exact problem in one sentence, regenerate with a corrected prompt that forces the fix, and only then present the result. A roll that has broken into separate rings is corrected with a prompt such as "continuous single spiral of leather, ends properly tucked, no separate rings." If the picture passes, present it normally. A picture that fails this check is not an accepted look.

A goods picture is a full-size painting with no background. The delivered file is a transparent PNG. The object is painted on a flat chroma-key green field only so that green can be removed. The whole object sits inside the frame with a clear margin on every side, so the cell grid contains it. Unity scales the picture into the cell. Do not deliver a small sprite.

## UI paper

The paper target is the mockup `Npz0d.jpg` in the project root. It is a reference for the sheet. It is not a screen to copy, and the words on it are not game text.

The sheet is aged cream parchment: visible fiber, foxing, faint cracks, and a torn deckled edge. The corner ornament is flat dark-brown ink. Between the corners the edge is one torn strip, even enough to stretch, with no medallion along a side. The fill is the paper itself. Text is never painted on the sheet.

The title menu frame stays gilded metal. This paper is the information panel, the location banner, and the top bar. Location tabs are a separate piece and are not part of the sheet. A location tab is a scrubcast resin seal, painted in dark amber with brush grain and grit, one drip joined to the bottom edge and ending in a bead. The resin takes a signet the way hot wax does. A circular border is pressed into the face, the field inside sits lower, and the device is one soft bold stamp. Hover and selected keep that same seal. On a screen the seal is the size of one tab in a bottom line of six.

The market seal is `Assets/Resources/Ui/resin-seal.png`. Hover is `Assets/Resources/Ui/resin-seal-hover.png`. Selected is `Assets/Resources/Ui/resin-seal-selected.png`. The market device is a thick upright, a level beam, and two pans joined to the beam.

The gate device is two thick uprights joined by a beam across the top, with one opening between the uprights. The picture is `Assets/Resources/Ui/resin-seal-gate.png`. Hover is `Assets/Resources/Ui/resin-seal-gate-hover.png`. The pressed line is `Assets/Resources/Ui/resin-seal-gate-selected.png`. It is on the Seals tab with the market seal.

The accepted sheet is `Assets/Resources/Ui/parchment.png`. It is the file on the UI Kit tab, sliced as a tall panel and as a short wide bar. The corner ink is inside the slice border. The torn edge between the corners stretches.

The accepted road frame is `Assets/Resources/Ui/bone-frame.png`. It is four Hulv bone beams, one continuous Zar'Kun sinew lashing at each corner, honeycomb at the cut ends. The center is empty. UI Kit shows it sliced at two sizes. Card shows it with the title and the body as text.

A physical UI piece uses a material from this world. Bone is Hulv bone: porous ivory beams. Wood is living lumber: faceted grain, bark, moss, a grain-eye, dark sap. Metal hardware is shimmersteel, sand-bronze crystal with grit in the crevices, or frost iron, dark iron under a pale rime, when the piece is cold. Cord is Zar'Kun sinew. Do not paint bronze, generic timber, or polished ivory.

## Starter mount

The duzhu is the starter mount, an eight-legged reptile. The giant is the oversized animal the hide comes from. These prompts are the mount a rider uses.

The bare picture is `Assets/Resources/Mounts/Duzhu.jpg`. It is the file on the Mounts page. The hide is small scales of even size and even spacing across the body, legs, and tail, with a row of larger plates along the spine. Each bronze ring sits where leather straps meet, and those straps run into the girth. The near side shows four legs. The second leg of each pair is not separated in this picture.

### Bare

Stylized painterly 2D illustration, side view, soft even lighting, plain muted sand background, no scenery.
A single large eight-legged reptile, standing in three-quarter side view, large enough to carry a rider and cargo.
Proportions: body length roughly twice a human's height, low and broad, eight sturdy legs (four pairs) evenly spaced along the torso, short thick neck, heavy tail for balance.
Materials: matte scaled hide in muted sand and ochre tones, subtle bronze harness rings at the shoulders and mid-body.
Load points: simple harness only, no saddle, no rider, no cargo.
Clear silhouette, matte painted texture, no wagon, no scenery, no text.

### Travel

The same animal, with gear and baggage and a saddle, packed for several days of desert travel. No rider.

Add gear and baggage plus a saddle (no rider) appropriate for several days desert travel.

## Market goods

### Giant Duzhu Hide

A bulky rolled hide from the giant, the oversized animal. The picture is `Assets/Resources/Goods/GiantDuzhuHide.png`. It is the file on the Giant Duzhu Hide cell. The leather is matte sand with ochre patches. Larger flat plates lie along the top of the roll. The open end is one continuous sheet wound in a spiral, the inner end tucked at the center. No animal, no harness.

### Shimmersteel

Raw cargo is two stacked bars. The picture is `Assets/Resources/Goods/Shimmersteel.png`. It is the file on the Shimmersteel cell. Sand and grit sit in the crevices. Broken faces are translucent crystal with a hard metal edge, sand-bronze, dull where the grit covers them.

A plain vessel of the same metal is a thick pot, cut into facets, with one lip. That pot is the vessel in `Assets/Resources/Goods/ScrubcastResin.png`, on the Scrubcast Resin cell. The resin in that pot is dark amber, and the drips stay joined to the lip.

Work refines the same metal. The jars are the low grade: thick, sand-bronze, grit dulling the crystal. Cookware sits between that and the high grade. Fineware is the high grade: the metal reads as gold, and each piece carries a different precious crystal.

### Living Lumber

One thick rough-carved plank. The picture is `Assets/Resources/Goods/LivingLumber.png`. It is the file on the Living Lumber cell. The long faces are chopped into irregular facets, with wood grain running the length and scraps of bark and moss along one edge. One cut end holds a grain-eye, and dark sap drips from that cut.

### Zar'Kun Sinew

The rope is one pale grey cord in a loose overlapping coil. The picture is `Assets/Resources/Goods/ZarKunSinew.png`. It is the file on the Zar'Kun Sinew cell. The near part of the cord crosses in front of the part behind it. Small round suckers run along the cord. The free end is a wrapped plug.

### Hulv Bone

Four porous ivory beams, lashed with one pale grey cord. The picture is `Assets/Resources/Goods/HulvBone.png`. It is the file on the Hulv Bone cell. Small round suckers run along the cord. The free end is a wrapped plug hanging below the bundle.

### Mor Root

Kindling is a stack of black lengths, each resting on the ones below. The picture is `Assets/Resources/Goods/MorRoot.png`. It is the file on the Mor Root cell. The ends are open cuts. Red and purple splotches mark the skin, and fine rootlets run along the lengths.

### Clack Fiend Molt

Sack cloth is one broad folded sheet of pale oily gossamer. The picture is `Assets/Resources/Goods/ClackFiendMoltSkin.png`. It is the file on the Clack Fiend Molt-skin cell. Where the sheet folds, the under layer shows through.

### Frost Iron

One small bar under a thick pale rime of ice crystals. The picture is `Assets/Resources/Goods/FrostIron.png`. It is the file on the Frost Iron cell. Dark iron shows through along the side.

### Titan's Heart-rock

One dark heat-scarred rock. The picture is `Assets/Resources/Goods/TitansHeartRock.png`. It is the file on the Titan's Heart-rock cell. Red seams run through the faces, with scorch along them.

### Shimmersteel Jars

A crate of thick faceted pots. The picture is `Assets/Resources/Goods/ShimmersteelJars.png`. It is the file on the Shimmersteel Jars cell. The faces are crystal with a sand-bronze lip, and grit sits in the crevices. The crate boards are planed timber with moss along the edges.

### Shimmersteel Cookware

One lidded pot. The picture is `Assets/Resources/Goods/ShimmersteelCookware.png`. It is the file on the Shimmersteel Cookware cell. The body and the lid are pale honey crystal. The rim, the knob, and both handles are warm bronze. Each handle meets the body at a bracket, and the lid sits in the rim.

### Shimmersteel Fineware

A ruby cup, an emerald bowl, and a sapphire dish. The picture is `Assets/Resources/Goods/ShimmersteelFineware.png`. It is the file on the Shimmersteel Fineware cell. Gold sits on the rim and the foot of each piece. A few chips mark the inner rim of the emerald bowl.

### Moon Soaked Zethi-Bulb

A gathered sack of pale oily gossamer. The picture is `Assets/Resources/Goods/MoonSoakedZethiBulb.png`. It is the file on the Moon Soaked Zethi-Bulb cell. The tubers show through the cloth. The neck is tied with a pale cord. The suckers sit on a braided strap beside that cord, and the free end is a wrapped plug.

### Sarn's Bane Liver

A slightly warped living cask. The picture is `Assets/Resources/Goods/SarnsBaneLiver.png`. It is the file on the Sarn's Bane Liver cell. Moss covers the bark, with short pale filaments in the moss. Dark veins branch through the wood of the head and join the oil at the bung.

## Pincer beast

The pincer beast is the larger mount, a giant hairy docile land crab. It carries more than the duzhu. Kelkris eat its molt. There is no pack system yet.

The mount picture is `Assets/Resources/Mounts/PincerBeast.jpg`. It is the file on the Mounts page. A howdah, a small cloth shelter, sits on the saddle. Its poles and straps run into the shell. The claws are held low. Coarse hair grows from the legs and the rim of the shell. No rider.

### Pincer Beast Fur

The cargo is a stack of separate pelts, bulky more than dense. The picture is `Assets/Resources/Goods/PincerBeastFur.png`. It is the file on the Pincer Beast Fur cell. The pieces differ in size and outline, so the pile is lopsided. Each piece rests on the one below it. The hair is dense and dusty tan.
