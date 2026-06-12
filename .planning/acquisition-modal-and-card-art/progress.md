# Acquisition Modal And Card Art Progress

## 2026-06-11

- Started work on legacy acquisition modal and realistic card art alignment.
- Inspected three user reference images and mapped the desired layout to current UGUI: mode tabs, left tier rail, center card grid, right type rail.
- Replaced the legacy right-rail acquisition list with a centered `CardAcquisitionModal` opened from the `获取` tab.
- Added acquisition filters for tavern spell/minion, tier all/1-7, and minion tribe type.
- Kept acquisition behavior on the existing `AddCardToHand` command path.
- Fixed realistic card rendering so official full-card sprites display as complete centered cards instead of being cropped through the portrait mask.
- Added EditMode tests for modal opening, modal filters, add-to-hand behavior, and full-card art anchoring.
- Ran Unity EditMode with `TestResults-AcquisitionModalCardArt.xml`; result: `237/237` passed.
