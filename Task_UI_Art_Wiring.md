# Task: Wire approved UI art (market kit + HUD)

Status: Approved (2026-11-11), ready
Owner: Grok Build
Source of truth: Google Drive folder `Caravans of the Cutting Sands / Art / Approved`
https://drive.google.com/drive/folders/1CxUslzhzK92rsCVmnYwO5fEp0cHslnch
Spec: `manifest.md` in that folder (ID `1_lEh15ql9iI_CtvHS1v1onK3dXn0eZ9C`). Style reference: `Reference/market_mockup_v2.jpg` (ID `1d_f_A4aSSKqf5o1LYHtP1Bh-YfgjH9Zi`).

Build pulls the files directly from that Drive folder using its Google Drive access (by the file IDs listed at the bottom) into `Assets/Art/Approved/`, keeping the file names exactly. Download the original PNG bytes, not previews or thumbnails, and check each file's dimensions against the manifest. Build does not regenerate, recolour or edit any sprite.

## 1. Import
- [ ] Download all 29 PNGs and `manifest.md` from Drive; confirm they are present in `Assets/Art/Approved/` (list below).
- [ ] Import settings from the manifest header: Sprite (2D and UI), Full Rect, Bilinear, High Quality, no compression.
- [ ] Set 9-slice borders in the Sprite Editor exactly as the manifest gives them (hide panel 150 all round; hide bar 145/145/45/45). For `ui_meter_frame.png`, place the L/R borders just inside the talons (~175px) and confirm visually.

## 2. Persistent top HUD (every scene)
- [ ] `ui_hide_bar.png` (Sliced, horizontal stretch) is the top bar in every scene.
- [ ] TextMeshPro on the hide: location name on the left, date and day-part in the centre, resources on the right.
- [ ] Right side: `ui_icon_scrubstone.png` + scrubstone count, `ui_icon_capacity.png` + cargo used/max. Icons 40–64px tall, Preserve Aspect.
- [ ] Remove the old settlement banner and Shimmersteel rail. Any banner code path (e.g. `FrontArt.Banner()`) should feed the location name into the HUD instead of drawing a banner. No vellum plate.
- [ ] The same bar sprite is used for the bottom bar where a scene has one.

## 3. Settlement screens
- [ ] `bg_settlement_generic.png` (blurred, 1920×1080) fills the screen behind every settlement scene, envelope-fit so there's no letterboxing. It's a placeholder until each location gets its own backdrop, so keep it a per-location field that defaults to this sprite.
- [ ] Do not use `bg_settlement_generic_sharp.png` in game (it's the source file only).
- [ ] Panels use `ui_hide_panel.png` (Sliced, Fill Center on).

## 4. Tabs and buttons (Scrubcast Resin seals)
- [ ] Tabs: Market, Inn, House, Gates use `ui_seal_<name>.png` with Sprite Swap transitions: Highlighted = `_hover`, Pressed = `_pressed`, Selected = normal (unless told otherwise).
- [ ] Buy and Sell buttons: `ui_seal_buy` / `ui_seal_sell` with the same `_hover` / `_pressed` swaps.
- [ ] `ui_seal_base.png` is for any other generic button. Label with TMP text on top; no signet.
- [ ] No baked text anywhere; Preserve Aspect on all seals.

## 5. Goods cells
- [ ] `ui_goods_cell.png` for every goods slot (market grids, wagon cargo, logbook); icon inset ~18% each side.
- [ ] Selected state swaps to `ui_goods_cell_selected.png`.
- [ ] Empty slot = normal cell at 50% alpha.

## 6. Wagon capacity meter (market, under Your Wagon)
- [ ] `ui_meter_frame.png` as the frame (Sliced, horizontal stretch only).
- [ ] `ui_meter_fill.png` inside the trough, anchored left, ~75% of trough height. Image Type Filled, Horizontal, Origin Left; fillAmount = cargo used / capacity.
- [ ] Matches the HUD capacity number.

## Acceptance
- [ ] Market scene screenshot matches the layout and feel of `market_mockup_v2.jpg`, with real Unity text.
- [ ] The HUD appears and updates in every scene (settlement, travel/map, any others).
- [ ] Hover/pressed swaps work on all six seals; selection swap works on goods cells; the meter fills correctly from empty to full.
- [ ] No banner, no rail, no copper, no wood art left in these screens.
- [ ] Post screenshots of market, one other settlement tab, and the travel scene for review. Nothing merged without Darkgecko's yes.

## File list (Drive IDs)
| File | Drive file ID | URL |
| --- | --- | --- |
| manifest.md | 1_lEh15ql9iI_CtvHS1v1onK3dXn0eZ9C | https://drive.google.com/file/d/1_lEh15ql9iI_CtvHS1v1onK3dXn0eZ9C/view?usp=drivesdk |
| ui_goods_cell.png | 1T-ZXelpd9P0gY1MiwilxLEQGJ-O26sc- | https://drive.google.com/file/d/1T-ZXelpd9P0gY1MiwilxLEQGJ-O26sc-/view?usp=drivesdk |
| ui_goods_cell_selected.png | 1QNoLtV-Xe8o8etxwcQcscybll2tBuWae | https://drive.google.com/file/d/1QNoLtV-Xe8o8etxwcQcscybll2tBuWae/view?usp=drivesdk |
| ui_hide_bar.png | 1t8GbEi1JYsWCYvleyZVHZ4dRzJEe_Tce | https://drive.google.com/file/d/1t8GbEi1JYsWCYvleyZVHZ4dRzJEe_Tce/view?usp=drivesdk |
| ui_hide_panel.png | 1v8NMgWIqmr88j2mX-Qr0eb3MXs6WM5Op | https://drive.google.com/file/d/1v8NMgWIqmr88j2mX-Qr0eb3MXs6WM5Op/view?usp=drivesdk |
| ui_icon_capacity.png | 1llXV9RGF8Rr2QCDGu466IMlIphLbngS4 | https://drive.google.com/file/d/1llXV9RGF8Rr2QCDGu466IMlIphLbngS4/view?usp=drivesdk |
| ui_icon_scrubstone.png | 1a8ffRpmFK6t-IW_okRQQk7eYLUhnKHgi | https://drive.google.com/file/d/1a8ffRpmFK6t-IW_okRQQk7eYLUhnKHgi/view?usp=drivesdk |
| ui_seal_base.png | 1HSnXBkg1qkhiUIdmmKETL0boOz9VsInn | https://drive.google.com/file/d/1HSnXBkg1qkhiUIdmmKETL0boOz9VsInn/view?usp=drivesdk |
| ui_seal_buy.png | 1TE0GbSsq55s3-vYJKI9G0zxh1uhNrLJO | https://drive.google.com/file/d/1TE0GbSsq55s3-vYJKI9G0zxh1uhNrLJO/view?usp=drivesdk |
| ui_seal_buy_hover.png | 1UhgduB__G7i8kpsEOJ9Cc3kA55MpoYwL | https://drive.google.com/file/d/1UhgduB__G7i8kpsEOJ9Cc3kA55MpoYwL/view?usp=drivesdk |
| ui_seal_buy_pressed.png | 1eIvSjvdFWP_4smeLWHklcSzIMq-f6oYg | https://drive.google.com/file/d/1eIvSjvdFWP_4smeLWHklcSzIMq-f6oYg/view?usp=drivesdk |
| ui_seal_gates.png | 1wB6WWZqPY1K7iipALmcROZCucQip3ty2 | https://drive.google.com/file/d/1wB6WWZqPY1K7iipALmcROZCucQip3ty2/view?usp=drivesdk |
| ui_seal_gates_hover.png | 1Mik6L__nM0fEsnigG65kcASc5jKwhUjq | https://drive.google.com/file/d/1Mik6L__nM0fEsnigG65kcASc5jKwhUjq/view?usp=drivesdk |
| ui_seal_gates_pressed.png | 1K86_AS8mctklxGig6nyYySeBiyicPNMb | https://drive.google.com/file/d/1K86_AS8mctklxGig6nyYySeBiyicPNMb/view?usp=drivesdk |
| ui_seal_house.png | 1uiTGqO3BUR0xj2Pn8fezreeh6wP91hXX | https://drive.google.com/file/d/1uiTGqO3BUR0xj2Pn8fezreeh6wP91hXX/view?usp=drivesdk |
| ui_seal_house_hover.png | 1jlO-OjbNUpdYZAs3eR1R1QjAiwvbTC6c | https://drive.google.com/file/d/1jlO-OjbNUpdYZAs3eR1R1QjAiwvbTC6c/view?usp=drivesdk |
| ui_seal_house_pressed.png | 1PqrEfI_NeZ-3u4AzrixNvltC8jLyI0_n | https://drive.google.com/file/d/1PqrEfI_NeZ-3u4AzrixNvltC8jLyI0_n/view?usp=drivesdk |
| ui_seal_inn.png | 1qYnj3g9KUyncngHh-8JJN1W299y1sccE | https://drive.google.com/file/d/1qYnj3g9KUyncngHh-8JJN1W299y1sccE/view?usp=drivesdk |
| ui_seal_inn_hover.png | 1O9iSN6mDjdIRF1z6Cvn5m_jtqyiUT8UE | https://drive.google.com/file/d/1O9iSN6mDjdIRF1z6Cvn5m_jtqyiUT8UE/view?usp=drivesdk |
| ui_seal_inn_pressed.png | 1uRIjiOFpKo39qhbaS_MVqCnXvx2yE1fu | https://drive.google.com/file/d/1uRIjiOFpKo39qhbaS_MVqCnXvx2yE1fu/view?usp=drivesdk |
| ui_seal_market.png | 1Z73afEIQzaAQft93c-UyxAGBeCMoYodE | https://drive.google.com/file/d/1Z73afEIQzaAQft93c-UyxAGBeCMoYodE/view?usp=drivesdk |
| ui_seal_market_hover.png | 1U8OdB5czdyoC4JjrZYBPafW96YCGXjfS | https://drive.google.com/file/d/1U8OdB5czdyoC4JjrZYBPafW96YCGXjfS/view?usp=drivesdk |
| ui_seal_market_pressed.png | 1iAvCww6isefPLuhjC946lv7-yRQApyaC | https://drive.google.com/file/d/1iAvCww6isefPLuhjC946lv7-yRQApyaC/view?usp=drivesdk |
| ui_seal_sell.png | 1kMYTXHAGgIVgqVFSD33agXKC7R8IPbeX | https://drive.google.com/file/d/1kMYTXHAGgIVgqVFSD33agXKC7R8IPbeX/view?usp=drivesdk |
| ui_seal_sell_hover.png | 11tAH9M19-nIB_EwYfFfqmbPkUtwL67bM | https://drive.google.com/file/d/11tAH9M19-nIB_EwYfFfqmbPkUtwL67bM/view?usp=drivesdk |
| ui_seal_sell_pressed.png | 1g83FOVU_gRB822WFU8MQvKbBwfv0Op97 | https://drive.google.com/file/d/1g83FOVU_gRB822WFU8MQvKbBwfv0Op97/view?usp=drivesdk |
| Reference/market_mockup_v2.jpg | 1d_f_A4aSSKqf5o1LYHtP1Bh-YfgjH9Zi | https://drive.google.com/file/d/1d_f_A4aSSKqf5o1LYHtP1Bh-YfgjH9Zi/view?usp=drivesdk |
| ui_meter_frame.png | 146QRpXJSRKpLT1maVU7qy7i1a0EXExji | https://drive.google.com/file/d/146QRpXJSRKpLT1maVU7qy7i1a0EXExji/view?usp=drivesdk |
| ui_meter_fill.png | 1gCUZ5vgRrzONc7UBDrl8U3BRDPfh-_or | https://drive.google.com/file/d/1gCUZ5vgRrzONc7UBDrl8U3BRDPfh-_or/view?usp=drivesdk |
| bg_settlement_generic.png | 154NMDDLjQPrxvI4e4zKjtCp3-W4_sTA0 | https://drive.google.com/file/d/154NMDDLjQPrxvI4e4zKjtCp3-W4_sTA0/view?usp=drivesdk |
| bg_settlement_generic_sharp.png | 1q8rnxqg9lJeaSCQPZLLi_PKbqIQYPpZg | https://drive.google.com/file/d/1q8rnxqg9lJeaSCQPZLLi_PKbqIQYPpZg/view?usp=drivesdk |
