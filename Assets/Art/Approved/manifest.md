# Approved UI Art — Manifest

Style reference: `Reference/market_mockup_v2.jpg` (locked 2026-11-11).
All sprites: no baked text; text is Unity/TextMeshPro. Import as Sprite (2D and UI), Mesh Type Full Rect, Filter Bilinear, no compression artefacts (High Quality).

| File | Use | Size (px) | 9-slice borders L/R/T/B (px) | Materials | Matches |
|---|---|---|---|---|---|
| `ui_hide_panel.png` | Default UI kit panel (market columns, detail column, road list, house card inner, etc.). Image Type Sliced, Fill Center on. | 1081×795 | 150 / 150 / 150 / 150 | Dark tanned Giant Duzhu Hide, Hulv Bone corner beams (honeycomb cut ends), Zar'Kun Sinew lashing | Market mockup panels |
| `ui_hide_bar.png` | Persistent top HUD in every scene (location left, date and day-part centre, scrubstone and capacity right, as Unity text on the hide) and the bottom bar. Replaces the settlement banner. Image Type Sliced; stretch horizontally only. | 1239×187 | 145 / 145 / 45 / 45 | Dark tanned Giant Duzhu Hide, bronze-grade faceted Shimmersteel trim, Hulv Bone end caps, Zar'Kun Sinew | Market mockup top and bottom bars |
| `ui_goods_cell.png` | Goods cell, normal state (market grids, wagon cargo, logbook). Image Type Simple, Preserve Aspect. Icon sits inside the well floor, inset ~18% each side. Empty slot = this sprite at 50% alpha (no separate asset). | 581×558 | n/a | Dark tanned Giant Duzhu Hide, Zar'Kun Sinew stitching | Market mockup goods cells |
| `ui_goods_cell_selected.png` | Goods cell, selected state; swap sprite on selection. Same placement as normal. | 581×558 | n/a | Dark tanned Giant Duzhu Hide, silver-grade Shimmersteel rim with crystal chips (selection/focus grade) | Market mockup selected cell |
| `ui_seal_base.png` | Button and tab base (Market, Inn, House, Gates tabs; Buy/Sell). Image Type Simple, Preserve Aspect. Blank base for generic buttons with no signet. | 855×872 | n/a | Scrubcast Resin | Market mockup tab and Buy/Sell seals |
| `ui_seal_market.png` | Market tab (awning stall, table, two sacks), normal state. Image Type Simple, Preserve Aspect. Signet baked in. | 853×875 | n/a | Scrubcast Resin | Market mockup seals |
| `ui_seal_market_hover.png` | Market tab, hover state (+22% brightness). Sprite swap. | 853×875 | n/a | Scrubcast Resin | — |
| `ui_seal_market_pressed.png` | Market tab, pressed state (−28% brightness). Sprite swap. | 853×875 | n/a | Scrubcast Resin | — |
| `ui_seal_inn.png` | Inn tab (domed adobe inn, lantern, smoke curl), normal state. Image Type Simple, Preserve Aspect. Signet baked in. | 855×874 | n/a | Scrubcast Resin | Market mockup seals |
| `ui_seal_inn_hover.png` | Inn tab, hover state (+22% brightness). Sprite swap. | 855×874 | n/a | Scrubcast Resin | — |
| `ui_seal_inn_pressed.png` | Inn tab, pressed state (−28% brightness). Sprite swap. | 855×874 | n/a | Scrubcast Resin | — |
| `ui_seal_house.png` | House tab (desert tower-house with pennant), normal state. Image Type Simple, Preserve Aspect. Signet baked in. | 855×878 | n/a | Scrubcast Resin | Market mockup seals |
| `ui_seal_house_hover.png` | House tab, hover state (+22% brightness). Sprite swap. | 855×878 | n/a | Scrubcast Resin | — |
| `ui_seal_house_pressed.png` | House tab, pressed state (−28% brightness). Sprite swap. | 855×878 | n/a | Scrubcast Resin | — |
| `ui_seal_buy.png` | Buy button (arrow down into tied sack), normal state. Image Type Simple, Preserve Aspect. Signet baked in. | 857×873 | n/a | Scrubcast Resin | Market mockup seals |
| `ui_seal_buy_hover.png` | Buy button, hover state (+22% brightness). Sprite swap. | 857×873 | n/a | Scrubcast Resin | — |
| `ui_seal_buy_pressed.png` | Buy button, pressed state (−28% brightness). Sprite swap. | 857×873 | n/a | Scrubcast Resin | — |
| `ui_seal_sell.png` | Sell button (arrow up out of sack), normal state. Image Type Simple, Preserve Aspect. Signet baked in. | 855×875 | n/a | Scrubcast Resin | Market mockup seals |
| `ui_seal_sell_hover.png` | Sell button, hover state (+22% brightness). Sprite swap. | 855×875 | n/a | Scrubcast Resin | — |
| `ui_seal_sell_pressed.png` | Sell button, pressed state (−28% brightness). Sprite swap. | 855×875 | n/a | Scrubcast Resin | — |
| `ui_seal_gates.png` | Gates tab (rounded adobe towers, arch, road), normal state. Image Type Simple, Preserve Aspect. Signet baked in. | 854×876 | n/a | Scrubcast Resin | Market mockup seals |
| `ui_seal_gates_hover.png` | Gates tab, hover state (+22% brightness). Sprite swap. | 854×876 | n/a | Scrubcast Resin | — |
| `ui_seal_gates_pressed.png` | Gates tab, pressed state (−28% brightness). Sprite swap. | 854×876 | n/a | Scrubcast Resin | — |
| `ui_icon_scrubstone.png` | HUD scrubstone icon, top bar right beside the scrubstone count; also usable in prices. Image Type Simple, Preserve Aspect, ~40-64px tall in HUD. | 635×676 | n/a | Scrubstone (compressed coarse crystalline sand, jagged sandstone coins with square holes) | HUD |
| `ui_icon_capacity.png` | HUD wagon capacity icon, top bar right beside the cargo count (used/max). Image Type Simple, Preserve Aspect, ~40-64px tall in HUD. | 709×660 | n/a | Three stacked hide sacks, Zar'Kun Sinew lashing, dark tanned Duzhu hide strip | HUD |
| `ui_meter_frame.png` | Wagon capacity meter frame (market Your Wagon column; later Caravan page). Image Type Sliced, stretch horizontally only; set borders to just inside the talons. Fill sits in the trough. | 1259×319 | ~175 / ~175 / 0 / 0 (verify in Sprite Editor) | Dark tanned Duzhu hide trough, bronze-grade faceted Shimmersteel trim, Terrorshriek Talon end grips, Zar'Kun Sinew lashing | Market mockup capacity meter |
| `ui_meter_fill.png` | Capacity meter fill. Image Type Filled, Horizontal, Origin Left (or Sliced with width driven by used/max), anchored to left end of trough, ~75% of trough height. | 1252×75 | n/a | Scrubcast Resin (dark amber, grit) | — |
| `bg_settlement_generic.png` | Generic full-screen settlement backdrop (blurred), behind all settlement screens until each location gets its own. Image stretched to fill the canvas (Preserve Aspect, envelope). | 1920×1080 | n/a | Mudbrick and adobe, hide awnings, desert | Market mockup backdrop |
| `bg_settlement_generic_sharp.png` | Unblurred source for the above, kept for re-blurring or other uses. | 1920×1080 | n/a | Same | — |
