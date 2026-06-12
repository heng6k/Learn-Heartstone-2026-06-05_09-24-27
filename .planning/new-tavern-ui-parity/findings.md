# New Tavern UI Parity Findings

## Initial Notes

- User wants the newly designed UI to gain all old UI functions, with a new start-screen entry for the new UI.
- Existing old UI must stay untouched from the player's perspective.
- Visual direction: closer to real Hearthstone Battlegrounds tavern UI.

## Implementation Findings

- Main hub now has separate entries: `酒馆训练器` opens the legacy trainer, and `真实酒馆 UI` opens the new realistic trainer.
- `LearnHearthstoneBootstrap` owns the split routing and passes a legacy-tools callback into the realistic trainer.
- New realistic UI exposes old trainer feature groups through drawer tabs: info, opponent editor, battle/scenario/replay, logs, and debug card acquisition.
- The realistic action panel includes old direct tavern actions: refresh, freeze, upgrade, next turn, sell drop zone, and direct `SimulateCombat`.
- Drag/drop parity includes shop to hand, discover to hand, hand to board, board reorder, board to hand, board sell, and opponent board reorder.
- Unity test coverage confirms the separate hub entries, realistic zones, drawer panels, battle drawer, quick combat button, and key drag command mappings.
