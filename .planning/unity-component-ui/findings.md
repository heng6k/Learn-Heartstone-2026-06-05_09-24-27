# Unity Component UI Findings

- Existing `MainHubView` has two enabled entries: legacy trainer and realistic tavern UI.
- `LearnHearthstoneBootstrap` already creates the Canvas/EventSystem and switches views by clearing the canvas.
- Existing UI is mostly code-generated UGUI, so the new route should be isolated rather than rewriting current views in place.
