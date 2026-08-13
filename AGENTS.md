# AGENTS.md — DarkMagic Unity Package Context

This file preserves working context for AI/Codex agents editing the DarkMagic Unity package repo.

## Project identity

DarkMagic is a student-friendly Unity helper library/package by John Selig. The design goal is to make common Unity/game-dev tasks feel simple, teachable, expressive, and low-ceremony for students while still leaving room for optional advanced use.

Core philosophy:

- Prefer simple, memorable APIs over maximal configurability.
- Provide safe defaults and zero-config behavior whenever possible.
- Support optional escape hatches for advanced users.
- Keep examples classroom-friendly.
- Avoid clever abstractions that make debugging harder for beginners.
- Whenever adding or changing APIs, update `README.md` and remove/correct outdated examples.

Current Unity compatibility goal:

- Support Unity 6.5 and newer.
- Avoid APIs deprecated/erroring in Unity 6.5, especially `Object.GetInstanceID()`.

## Current latest known version

Latest version produced in the Codex session: **3.11.1**

Patch intent for 3.11.1:

- Set the documented/package support baseline to Unity 6.5+.
- Verify imported user config across real Unity assembly boundaries.
- Make `UConfigUser` overrides work in release Mono and IL2CPP players.

Patch intent for 3.11.0:

- Add Unity EditMode and PlayMode regression tests plus repeatable validation tooling.
- Make bundled TMP fonts work in a new project without importing TMP Essential Resources.
- Add beginner-friendly Stats modifier helpers and improve StatBlock consistency.
- Reorganize documentation into a concise README plus focused `Documentation~/` guides.
- Split floating outcome internals out of the large `U.cs` file while preserving `U.PopOutcome`.

Patch intent for 3.10.11:

- Fix Unity 6.5 errors caused by `Object.GetInstanceID()` becoming obsolete/erroring.
- Removed actual `GetInstanceID()` calls from:
  - `Runtime/V/V.cs`
  - `Runtime/Async/W.cs`
  - `Runtime/StateMachine/StateMachineRegistry.cs`
- Replaced internal owner-tracking IDs with a compatibility helper:

```csharp
using System.Runtime.CompilerServices;

private static int GetOwnerKey(UnityEngine.Object owner)
{
    return RuntimeHelpers.GetHashCode(owner);
}
```

The owner key remains internal guardrail/registry bookkeeping, not gameplay identity, persistence, or save data.

When opening this repo, first check:

```bash
grep -R "GetInstanceID" Runtime Editor -n
```

There should be no actual `.GetInstanceID()` calls. Comments are okay, but avoid comments that contain `.GetInstanceID(` if simple grep checks are being used.

Also check `package.json` version and keep it in sync with changes.

## Repo workflow expectations

When making changes:

1. Inspect existing files before patching.
2. Keep APIs student-first and stable.
3. Update `README.md` for any feature/API change.
4. Bump `package.json` version for package changes.
5. Prefer small, focused patches.
6. Mention exact files changed in summaries.
7. Keep the package compatible with Unity 6.5 and newer.
8. Search for duplicate/outdated docs after API changes.

Useful checks:

```bash
grep -R "GetInstanceID" Runtime Editor -n
grep -R "TODO\|FIXME" Runtime Editor -n
```

If Unity is available, compile the package in a blank Unity project after changes.

## User preferences

John prefers:

- Concise but clear explanations.
- Practical teaching examples.
- APIs that feel like “sugar” without hiding too much.
- Good README docs.
- Minimal ceremony for students.
- Friendly naming.
- Optional advanced paths, not mandatory architecture.
- Stable package versions and downloadable zips when working in ChatGPT.

Avoid:

- Over-engineering.
- Excessive generic architecture.
- Breaking existing examples.
- Leaving stale README sections.
- Assuming the local repo exactly matches prior artifact history without checking.

## Major modules and current expectations

### U UI helpers

Main helpers include:

- `U.PopDialogue`
- `U.PopChoice`
- `U.PopBanner`
- `U.Display`
- `U.Menu`
- `U.PopOutcome`
- `U.Target`
- `U.Flow`

General design:

- Uses TMP where appropriate.
- Student-facing API should remain easy to read.
- Supports pseudo-rich text / TMP rich text.
- Sensible prefab/canvas fallback behavior.

### U.PopOutcome

Intended API examples:

```csharp
U.PopOutcome(targetTransform, 125);
U.PopOutcome(targetTransform, 50, Color.green);
U.PopOutcome(targetTransform, "+999 DEXTERITY", Color.yellow, textSize: 56);
await U.PopOutcome(targetTransform, "Miss!");
```

It should work whether awaited or fire-and-forgotten, depending on the Unity Awaitable pattern used.

Position origin priority:

1. Child named `OutcomeAnchor`
2. `Collider2D` top-center
3. `Collider` top-center
4. `target.position + Vector3.up * UConfig.OutcomeWorldOffsetY`

Important config fields include:

- `OutcomeFontSize`
- `OutcomeColor`
- `OutcomeDuration`
- `OutcomeRisePx`
- `OutcomeBouncePx`
- `OutcomeScalePop`
- `OutcomeCanvasPaddingPx`
- `OutcomeOffsetX`
- `OutcomeOffsetY`
- `OutcomeWorldOffsetY`
- `OutcomeAnchorChildName = "OutcomeAnchor"`

A previous positioning issue was caused by two active Main cameras, not necessarily by the PopOutcome code.

### U.Target

Targeting is meant to be a beginner-friendly JRPG target picker.

Default behavior:

- List cycling / JRPG menu style.
- Optional mouse hover/raycast support.
- Supports 2D and 3D targets.

Important APIs:

```csharp
var t = await U.Target.Select(enemyTransforms);
var t = await U.Target.Select(enemyPartyTransform); // direct children
var t = await U.Target.Select(enemyUnits, filter: u => u.HP > 0);

var all = await U.Target.SelectMany(enemies, mode: U.TargetMode.All);
var some = await U.Target.SelectMany(enemies, mode: U.TargetMode.UpTo, count: 3);
var exact = await U.Target.SelectMany(enemies, mode: U.TargetMode.Exact, count: 2);

var rules = new U.TargetRules { AllowMouseRaycast = true };
var t2 = await U.Target.Select(enemies, rules: rules);
```

Target marker expectations:

- Default glyph: `^`
- Y scale: `-1` so caret points downward
- Default color: `#FFE108`
- Marker font defaults to the same font as U, unless overridden.
- Optional overrides:
  - `UConfig.TargetMarkerFont`
  - `UConfig.TargetMarkerPrefab`
  - `UConfig.TargetMarkerSprite`
- Marker origin priority:
  1. `TargetAnchor`
  2. `OutcomeAnchor`
  3. Collider top-center
  4. Pivot + `UConfig.TargetMarkerWorldOffsetY`
- Screen offset via `UConfig.TargetMarkerScreenOffset` and/or rules.

There was once an overload ambiguity with `List<Transform>`; keep explicit Transform/list overloads if touching this area.

### U.Flow

Recent teaching guidance:

`U.Flow.Run(root)` is an action-menu loop:

- Shows a menu.
- Runs selected action.
- Returns to the same menu.
- Submenus push onto the stack.
- Cancel/back pops.
- Cancel at root exits.

For a real turn-based battle system, prefer this structure:

```csharp
// 1. Pick command
// 2. Pick target(s), if needed
// 3. Resolve command
// 4. Advance turn
```

Use either:

#### Simple mode: actions do everything inside leaf handlers

```csharp
var root = new U.Flow.Menu("Battle!")
    .Add("Fight", async () =>
    {
        var target = await U.Target.Select(enemies);
        if (target.Cancelled) return;

        await U.PopBanner("Attack!");
        U.PopOutcome(target.Value, 12);
        // Apply damage here.
    });

await U.Flow.Run(root);
```

This loops until cancelled, which is good for prototypes but less ideal for one-command-per-turn battle logic.

#### Preferred battle mode: Pick a command payload

Use `AddSelect` and `U.Flow.Pick<T>` so the battle manager owns turn logic.

```csharp
public enum BattleCommandType
{
    Fight,
    Magic,
    Defend
}

public class BattleCommand
{
    public string Name;
    public BattleCommandType Type;
    public bool TargetsAll;
    public int Power;
    public Color OutcomeColor;

    public BattleCommand(string name, BattleCommandType type, int power = 0, bool targetsAll = false)
    {
        Name = name;
        Type = type;
        Power = power;
        TargetsAll = targetsAll;
        OutcomeColor = Color.white;
    }
}
```

```csharp
var root = new U.Flow.Menu("Battle!")
    .AddSelect("Fight", new BattleCommand("Fight", BattleCommandType.Fight, power: 10))
    .AddSubmenu("Magic", magic =>
    {
        magic.Description = label =>
            label switch
            {
                "Firewave" => "Hit all enemies.",
                "Spark" => "Hit one enemy.",
                _ => "",
            };

        magic.AddSelect(
            new U.Option("Firewave", "Hit all enemies."),
            new BattleCommand("Firewave", BattleCommandType.Magic, power: 8, targetsAll: true)
            {
                OutcomeColor = Color.yellow
            }
        );

        magic.AddSelect(
            new U.Option("Spark", "Hit one enemy."),
            new BattleCommand("Spark", BattleCommandType.Magic, power: 15)
            {
                OutcomeColor = Color.cyan
            }
        );
    })
    .AddSelect("Defend", new BattleCommand("Defend", BattleCommandType.Defend));

var decision = await U.Flow.Pick<BattleCommand>(root);
if (decision.Cancelled) return;

var command = decision.Value.Payload;
```

Then resolve targeting outside the menu.

## Stats system

Stats were added around v3.10.x.

Key files:

- `Runtime/Stats/Stat.cs`
- `Runtime/Stats/StatBlock.cs`
- `Runtime/Stats/Archenemy.DarkMagic.Stats.asmdef`
- `Editor/Stats/StatDrawer.cs`
- `Editor/Stats/StatBlockDrawer.cs`

The Stats asmdef should be `autoReferenced: true` so user scripts in `Assembly-CSharp` can see `Stat` and `StatBlock`.

### Stat constructor

Current intended constructor:

```csharp
public Stat(string name, string abbr, int initial, bool persists = false, bool isLethal = false, bool isResource = false)
```

The third integer `initial` sets:

- `Initial = initial`
- `Base = initial`
- `Remaining = initial`

It also sets:

- `Delta = 1`
- `Threshold = 0`
- modifiers to `0`
- flags from arguments
- `IsDisabled = false`

### Stat fields

Expected fields include:

- `Name`
- `Abbreviation`
- `Initial`
- `Base`
- `Remaining`
- `Delta`
- `Threshold`
- `TempModifiers`
- `EquipmentModifiers`
- `IsResource`
- `Persists`
- `IsLethal`
- `IsDisabled`

Events:

- `OnBaseChanged`
- `OnThresholdMet`

Events may need warning suppression if the library does not subscribe internally.

### Stat behavior

Expected methods:

- `Refresh(bool force = false)`
- `Heal(int amount)`
- `Damage(int amount)`
- `ModifyTemp(int amount)`
- `Buff(int amount)`
- `Debuff(int amount)`
- `ModifyEquipment(int amount)`
- `ModifyBase(int amount)`
- `ClampRemainingToMax()`
- `LevelUp()`
- `CheckThreshold()`
- `ClampAndCheckLethal()`

Threshold behavior:

- `Threshold = 0` means disabled.
- Threshold checks should return early if `Threshold <= 0`.

Current/default stat meaning:

- `Delta = 1`
- `Threshold = 0`

`Stat.LevelUp()` should:

- Apply `Delta` to `Base`.
- For resources, refill `Remaining` to the new effective max.

### Operators

Expected operators:

```csharp
public static implicit operator int(Stat stat);
public static Stat operator +(Stat stat, int amount);
public static Stat operator -(Stat stat, int amount);
public static Stat operator >>(Stat stat, int baseDelta);
```

Meaning:

- For `IsResource` stats:
  - `+=` heals
  - `-=` damages
- For non-resource stats:
  - `+=` temp buff
  - `-=` temp debuff
- `>>` modifies Base persistently.

C# cannot overload `>>>`, so `>>` is used for persistent base change.

Important: `StatBlock` indexer must have a setter so this compiles:

```csharp
stats["HP"] -= 11;
```

### Potential Stats bug to check

If `CalculateCurrent()` still returns `Remaining` for non-resource stats when `Remaining < effectiveMax`, then this may fail to show buffs:

```csharp
stats["STR"] += 5;
Debug.Log(stats["STR"]);
```

Preferred logic:

```csharp
public int CalculateCurrent()
{
    int effectiveMax = Base + TempModifiers + EquipmentModifiers;

    if (!IsResource)
        return effectiveMax;

    return Remaining < effectiveMax ? Remaining : effectiveMax;
}
```

For non-resource stats, `Remaining` should usually be ignored. For resource stats, `Remaining` matters.

### StatBlock

Expected features:

- Stores serialized list of `Stat`.
- Runtime lookup by name or abbreviation, case-insensitive.
- `All` read-only list.
- `Add(Stat s)`
- `Get(string key)`
- Indexer getter/setter:
  - `stats["HP"]`
  - `stats["Strength"]`
- `GetInt(string)`
- `SetBase`
- `AddTemp`
- `AddBase`
- `Refresh(bool force = false)`
- `LevelUp()`
- `GetOrCreate(string, int initial = 0)`
- `AutoRegisterFields()` for typed StatBlock subclasses.

Typed StatBlocks are encouraged for dot access:

```csharp
public sealed class PlayerStats : StatBlock
{
    public Stat HP = new("Health", "HP", 120, persists: true, isLethal: true, isResource: true);
    public Stat MP = new("Magic", "MP", 30, persists: true, isResource: true);
    public Stat STR = new("Strength", "STR", 12);

    public PlayerStats()
    {
        AutoRegisterFields();
    }
}
```

C# cannot dynamically create `.STR` or `.Strength` properties from dictionary keys. Dot access requires real fields/properties.

### Stats inspector

Expected inspector files:

- `Editor/Stats/StatDrawer.cs`
- `Editor/Stats/StatBlockDrawer.cs`

Expected behavior:

- `Stat` collapsed header is a button-like row, not a default foldout triangle.
- Header text should be bold and formatted:
  - `[ABBR] Full Name: Current`
- Clicking the header toggles expansion.
- No triangle should appear outside panel bounds.
- Expanded stat layout:
  1. `Current` at top, bold, no parenthetical.
  2. `Abbreviation`
  3. `Name`
  4. `Remaining` only if `IsResource`
  5. `Base`
  6. `Initial`
  7. `Delta`
  8. `Threshold`
  9. `Temp Mods`
  10. `Equip Mods`
  11. `Is Resource`
  12. `Persists`
  13. `Is Lethal`
  14. `Is Disabled`
- `Abbreviation` and `Name` should be read-only once set, editable while empty.
- Labels should be wide enough to avoid ellipses.
- Tooltips should explain fields.
- `Current` should use `GUI.Label` with rich text enabled, not an `EditorGUI.LabelField` overload that shows literal tags.
- Avoid ambiguous `new GUIContent(string, null)`.
- `Remaining` should only appear when `IsResource` is checked.
- `StatBlock` should:
  - Avoid redundant nested foldouts where possible.
  - Hide list size count.
  - Use fixed order, not drag-reorder.
  - Keep add/remove controls.
  - Put `+`, `-`, `LevelUpAll`, `RefreshAll`, and `RefreshForce` together in the same footer bar.

## Prior artifact/version history

Useful recent milestones:

- `v3.8.9`: `U.PopOutcome` with anchor/collider fallback.
- `v3.9.x`: `U.Target` targeting.
- `v3.9.6`: target marker defaults: `^`, Y scale `-1`, gold color.
- `v3.10.0`: added `Stat` and `StatBlock`.
- `v3.10.1`: typed StatBlock sample/config, auto-threshold docs.
- `v3.10.2`: Stats asmdef and `IsResource`.
- `v3.10.3`: added missing `ModifyBase` and suppressed event warning.
- `v3.10.4`: StatBlock indexer setter so compound assignment works.
- `v3.10.5`: `Delta = 1`, `Threshold = 0`, threshold ignored when zero.
- `v3.10.6`: `LevelUp`, `StatBlock.LevelUp`, `ClampRemainingToMax`.
- `v3.10.7`: initial Stats custom inspector.
- `v3.10.8`: Stats inspector polish.
- `v3.10.9`: fixed `GUIContent(string, null)` overload ambiguity.
- `v3.10.10`: fixed literal `<b>` tags in Current by using rich text `GUI.Label`.
- `v3.10.11`: Unity 6.5 compatibility for internal owner tracking.
- `v3.10.12`: fixed non-resource Current so modifiers display correctly.
- `v3.11.0`: tests/validation, zero-config TMP fallback, Stats helpers, focused docs, and U outcome refactor.
- `v3.11.1`: Unity 6.5+ baseline and release-safe cross-assembly user config.

## Likely future work

The user is actively building a real battle system for class. Likely next work may involve:

- Better U.Flow examples and API ergonomics.
- Battle command payload helpers.
- More Targeting workflows.
- Stats correctness and battle damage examples.
- PopOutcome animation polish.
- Turn order / action menus.
- Teaching-friendly sample scenes.
- README battle-system tutorial section.

When in doubt, make the simplest thing that can be taught in one class session.
