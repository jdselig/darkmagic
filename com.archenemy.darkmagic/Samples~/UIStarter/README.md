# UI Starter Sample (U)

Import this sample to see U in action.

1) Package Manager → V → Samples → Import **UI Starter**
2) Create an empty GameObject and add `UDemo` to it.
3) Press Play.

Keys:
- Confirm: Enter / Space / Left click
- Cancel: Esc / Backspace / Right click
- In menus: Up/Down arrows (or W/S), Confirm, Cancel
- Press **P** to increment score (HUD updates automatically)

Hooks:
U broadcasts V events:
- `U.DialoguePopped` (string: page text)
- `U.ChoiceMade` (string: option label)
- `U.ChoiceCanceled` (string: prompt/title)


Targeting:
- See main README for `U.Target.Single(...)` usage.
