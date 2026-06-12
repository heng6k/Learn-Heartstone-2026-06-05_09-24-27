# UIUX Pro Max Polish Findings

## Initial Findings

- Project scope is single-player Battlegrounds Tavern/training only.
- Existing UI work already split legacy trainer and realistic trainer; old UI must remain behaviorally available.
- Previous planning shows new realistic UI parity and acquisition modal/card art polish were already implemented and verified.
- The working tree is dirty with many existing modified/untracked files, so this task must avoid broad rewrites.
- Local `ui-ux-pro-max` skill is installed at `C:\Users\wch\.agents\skills\ui-ux-pro-max` and has real `data`, `scripts`, and `templates` directories.
- UI/UX Pro Max priorities relevant to this task: accessibility, touch target size, interaction feedback, consistent style, layout hierarchy, typography/color tokens, forms/feedback.
- PSYOP-Z/UIUXProMax README guidance: install/activate skill, generate or adopt a tailored design system, then apply a pre-delivery checklist covering no emoji icons, hover/interaction states, text contrast, focus states, reduced motion, and responsive breakpoints.
- The legacy UI is `Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/TavernTrainerView.cs`.
- Shared UGUI helpers live in `Assets/LearnHearthstone/Runtime/Presentation/Common/UiFactory.cs`.
- The legacy UI is built entirely in C# with Unity UGUI components, so polish should happen through helper methods, colors, spacing, layout sizes, and focused component composition rather than prefab or UI Toolkit work.
- Existing tests for legacy UI live in `Assets/LearnHearthstone/Tests/EditMode/TavernTrainerViewTests.cs` and already assert key layout/drag/drop features.
- Existing tests for the new realistic UI live in `Assets/LearnHearthstone/Tests/EditMode/RealisticTavernTrainerViewTests.cs`.
- Project uses `com.unity.ugui` 2.0.0 on Unity 6000.4.10f1.
- Unity UGUI package docs verified:
  - `Selectable.colors` is the `ColorBlock` for selectable objects and requires ColorTint transition to be visible.
  - `ColorBlock` exposes `normalColor`, `highlightedColor`, `pressedColor`, `disabledColor`, `colorMultiplier`, and `fadeDuration`.
  - `LayoutElement` exposes min/preferred/flexible width and height, matching existing `UiFactory` sizing helpers.
- The new realistic UI is a useful in-repo reference for tavern material colors, resource pills, 34px buttons, slots, and drawer tabs, but the old UI needs denser trainer clarity rather than full scenic imitation.

## Design Direction Draft

- Subject: a Hearthstone Battlegrounds single-player training tool.
- Audience: a player/developer repeatedly testing tavern actions, combat, opponent setup, and card acquisition.
- Primary job: make dense trainer controls easier to scan and operate without hiding important state.
- Palette direction: tavern board warmth balanced with readable parchment panels, brass primary actions, cool blue info accents, and ruby danger states.
- Signature element: command-console clarity layered over tavern materials, so debugging/training controls feel intentional rather than plain editor UI.

## Candidate Scope

- Add a small legacy UI design token layer inside `TavernTrainerView` rather than rewriting all UI.
- Improve the top toolbar/status hierarchy, action buttons, right inspector tabs, dock headers, cards, empty states, logs, and drop zones.
- Add accessibility/usability improvements in `UiFactory` where low-risk, such as stronger button color transitions and minimum touch sizes.
- Likely implementation: add button tinting helper and legacy color tokens, increase primary interactive heights to 40-44px, use warm/cool semantic colors for panels, and add focused tests for button min height/state colors.

## Final Findings

- The low-risk path was to polish the legacy UGUI code directly inside `TavernTrainerView` and avoid changing shared behavior broadly in `UiFactory`.
- `ColorBlock` state styling gives consistent hover/pressed/disabled feedback without new assets or dependencies.
- Structural tests are enough to guard the main usability improvements in batch mode, though they do not replace a human visual pass in the Unity Game view.
