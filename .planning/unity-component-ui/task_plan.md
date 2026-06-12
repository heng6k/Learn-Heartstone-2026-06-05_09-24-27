# Unity Prefab UI Migration Plan

Goal: follow `Docs/UnityPrefabUiImplementationPlan.md` in small phases, keeping the existing C# domain/application logic and old UI entries intact while the new Unity-style UGUI surface becomes prefab-ready.

## Current Slice

- [completed] Phase 0: keep the new MainHub entry and old UI entries available.
- [completed] Phase 1: split `UnityTavernTrainerView` shell from `UnityTavernTrainerController`.
- [completed] Phase 2 first slice: create/load `UnityTavernRoot.prefab` with generated fallback.
- [completed] Phase 3 first slice: create/load `TavernCard.prefab`, `BoardMinion.prefab`, and `CardSlot.prefab`; make card binding prefer serialized references while preserving generated fallback UI.
- [completed] Add focused EditMode coverage for controller split, root prefab injection, card reference binding, and card prefab binding.
- [completed] Compile verification with Unity bundled compiler.
- [completed] Phase 4: create/load shop, hand, player-board, and opponent-board zone prefabs with serialized title/subtitle/slot-parent/card-prefab references while retaining generated fallback binding.
- [completed] Record visual self-review and next phase recommendation.

## Later Phases

- [completed] Phase 2: create `UnityTavernRoot.prefab` in Unity and let the shell load it when available.
- [completed] Phase 3: create real `TavernCard.prefab`, `BoardMinion.prefab`, and `CardSlot.prefab`.
- [completed] Phase 4: create shop/hand/player-board/opponent-board zone prefabs with prefab-authored headers, slot rows, slot prefab, tavern card prefab, and board minion prefab references.
- [completed] Phase 5: add drag/drop command mapping for shop, discover, hand, player board, opponent board, and sell zone.
- [completed] Phase 6 first slice: create prefab-backed right panel shell, discover modal, and error toast with serialized section/message references.
- [completed] Phase 6 interaction add-on: make the right inspector panel toggle between docked and floating stretched layouts.
- [completed] Phase 6 second slice: extract action, selected-card detail, advisor, recruit log, and combat log into prefab-backed child panels.
- [completed] Phase 6 final slice: create `CardDetailModal.prefab` and route selected-card detail expansion through a prefab-backed modal.
- [completed] Phase 7: prefab combat replay panel and timeline, including play/pause, speed cycling, frame jump controls, stable 7-slot boards, timeline windowing, and event highlight chips.
- [completed] UI gap pass: add prefab-backed trainer tools for existing C# debug/test/opponent-edit commands not previously visible in the Unity-style UI.
- [completed] Phase 8: add restrained hover/selected/action animations and command feedback. Card hover/selected/press feedback, drop target highlight/outline, drag ghost `CanvasGroup`, Unity fake-null component ensures, responsive Canvas scaling, Unity trainer editor menu routing, Chinese UI polish, and command success/error feedback are complete.
- [completed] Combat animation add-on: turn existing `CombatReplay` frames into visible Unity UGUI battle motion. Opening combat now auto-plays the replay; attack, hit, death, summon, trigger, and related entities receive tile-level motion/highlight feedback without changing Domain/Application combat logic.
- [completed] Phase 9: retire the old generated tavern UI from normal user paths after feature parity and tests.
- [completed] Phase 9 follow-up: remove unused generated fallback builders from the Unity-style controller after the prefab path covered those panels.

## Verification

- Runtime and EditMode test assemblies should compile with Unity's bundled Mono/C# compiler.
- Unity batchmode EditMode tests should pass before starting drag/drop behavior.
- Unity batchmode `-runTests` can be delayed while the Test Framework completes setup; wait for the XML result before treating it as blocked.

## Decisions

- Keep Domain/Application logic untouched.
- UI commands continue to flow through `MatchService.Apply(GameCommand)`.
- Keep old UI entries available until the prefab UI covers the core flow.
- Prefer serialized component references for production prefab binding.
- Preserve generated fallback elements so the view remains usable before prefabs are authored in the Unity Editor.
- Visual direction: tavern card table, compact controls, stable slot sizes, warm wood plus muted blue/green panels and gold/red accents.

## Visual Self-Review

- Current generated/fallback layout is usable and less brittle: fixed card/slot sizes, stable action buttons, prefab-backed card roots, and compact badges.
- Runtime window scaling is now owned by Bootstrap instead of only by newly generated Canvas instances, so existing scene canvases and newly created canvases use the same 1920x1080 responsive scale policy.
- It is still not final art direction: zone prefabs are root shells, not hand-tuned full layouts yet.
- Zone prefabs now have prefab-authored header, title/subtitle text, card row, slot prefab, tavern card prefab, and board minion prefab references.
- Phase 8 now has visible command feedback without changing Domain/Application logic: successful buy/play/sell/refresh/upgrade/combat/tool commands show a compact green feedback toast, while invalid actions keep the red error toast.
- Card detail, discover, combat replay controls, replay empty states, and prefab-generated default labels are now Chinese-facing. Internal object names remain stable for prefab/test binding.
- Battle replay now has a first-pass visual performance layer: attacking minions pulse/tilt, targets shake/red-flash, deaths fade out from the prior slot, summons pop in, and triggered/related entities receive gold/table highlights.

## Next Recommended Slice

- Do a manual Unity Play Mode visual pass in the target Game view sizes, focused on the large tools card library, right drawer, combat replay overlay, and narrow-window text fit.
- If the Unity prefab UI still feels usable after that pass, the next code cleanup can physically delete or archive the old generated `TavernTrainerView`/Realistic entry code and their obsolete MainHub tests.
