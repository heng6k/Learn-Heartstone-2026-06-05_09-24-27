# Acquisition Modal And Card Art Findings

## Reference Image Notes

- The desired acquisition page is a centered collection browser with a left tier rail, a top card-kind switch, a large middle card grid, and a right type/tribe rail.
- Tavern spell mode should show tavern spells; minion mode should show minions.
- Tier filters include all and 1-7.
- Type filters include all and minion tribe types for minions.
- The current old UI keeps acquisition inside the right rail, which makes the card list cramped and unlike the reference.
- The modal should dim the underlying trainer and visually read as a separate collection browser, with a clear close/back affordance.
- Reference cards are vertically aligned and centered inside card frames; crooked/cropped art likely comes from card image sizing or RectTransform offsets rather than data.

## Root Cause Analysis

**Error**: New realistic UI card images appeared crooked/cropped in the wrong area.
**Expected**: Official card images should appear as complete centered cards.
**Cause**: `CardImages/*` resources are full official card images, but `TavernCardView` rendered them inside a portrait mask meant for cropped art.
**Fix**: When a sprite exists, render it as a full-card `CardArtImage` stretched over the card rect with `preserveAspect`; use the old portrait/text fallback only when art is missing.
**Prevention**: Added an EditMode test that asserts official cards do not create `CardPortraitMask` and use full-card anchors.

## Implementation Findings

- Legacy `获取` now opens `CardAcquisitionModal` in the center instead of showing the old right-rail list.
- The modal provides card-kind toggle (`酒馆法术` / `随从`), tier filters (`全部`, `1`-`7`), and type filters.
- The modal card grid still calls `GameCommandType.AddCardToHand`, so hand limits and existing service behavior remain in one path.
- The old right rail keeps a small launcher panel while the acquisition browser lives in the modal.
