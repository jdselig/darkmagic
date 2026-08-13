# Code-first UI with U

`U` creates classroom-friendly UI at runtime with sensible defaults. It needs no prefabs or hand-built canvas. DarkMagic ships its font assets and uses Unity's official TMP shaders.

## One-time TMP setup

When DarkMagic first detects a missing or broken TMP shader, choose **Import Now**. You can repeat the check from **Tools → DarkMagic → Setup UI**. Unity imports TMP Essential Resources into `Assets/TextMesh Pro`; no separate package download is needed.

DarkMagic will stop with a clear setup message instead of showing magenta blocks. It also checks before player builds. If your project assigns a working custom `UConfig.FontAsset`, DarkMagic uses it and skips the import prompt.

Import the Config sample for optional project-owned styling. `UConfigUser` can live in its own assembly; DarkMagic discovers its public static fields and applies them in Editor, Mono, and IL2CPP builds. Keep the sample's `[Preserve]` attribute so release stripping cannot remove it.

## Banners and dialogue

```csharp
await U.PopBanner("Level up!");
await U.PopBanner("Saved", secondsToLive: 1f);

await U.PopDialogue("Page one.<pbr/>Page two.");
```

TMP rich text works normally. DarkMagic also recognizes `<br/>`, `<pbr/>`, and convenience colors such as `<color=Colors.gold>`.

## Choices

```csharp
var result = await U.PopChoice(
    "Choose a spell",
    new U.Option("Spark", "Hit one enemy."),
    new U.Option("Firewave", "Hit all enemies.")
);

if (result.Cancelled) return;
Debug.Log(result.Value);
```

Menus return `U.Result<T>` with `Cancelled` and `Value`. Cancel is normal control flow, not an exception.

## Reactive displays

```csharp
U.IDisplayHandle display = U.Display(
    () => "HP " + stats["HP"].Current,
    U.Placements.TopLeft
);

display.Hide();
display.Show();
display.SetText(() => $"MP {stats["MP"].Current}");
display.Dispose();
```

## Target selection

```csharp
var one = await U.Target.Select(enemyTransforms);
var child = await U.Target.Select(enemyPartyTransform);
var typed = await U.Target.Select(enemies, filter: enemy => enemy.HP > 0);

var all = await U.Target.SelectMany(enemies, mode: U.TargetMode.All);
var some = await U.Target.SelectMany(enemies, mode: U.TargetMode.UpTo, count: 3);
var exact = await U.Target.SelectMany(enemies, mode: U.TargetMode.Exact, count: 2);
```

The default picker cycles like a JRPG menu. Enable optional mouse raycasts with `new U.TargetRules { AllowMouseRaycast = true }`.

Marker position priority:

1. `TargetAnchor` child.
2. `OutcomeAnchor` child.
3. Collider top-center.
4. Pivot plus `UConfig.TargetMarkerWorldOffsetY`.

## Menu flow

`U.Flow.Run(root)` executes action leaves and returns to the current menu:

```csharp
var root = new U.Flow.Menu("Camp")
    .Add("Rest", async () => await Rest())
    .AddSubmenu("Items", items =>
    {
        items.Add("Potion", async () => await UsePotion());
    });

await U.Flow.Run(root);
```

For one command per battle turn, use payload leaves:

```csharp
var root = new U.Flow.Menu("Battle")
    .AddSelect("Fight", BattleCommand.Fight)
    .AddSelect("Defend", BattleCommand.Defend);

var pick = await U.Flow.Pick<BattleCommand>(root);
```

See [Battle workflow](Battle.md) for target selection and resolution.

## Floating outcomes

```csharp
U.PopOutcome(target, 25);
U.PopOutcome(target, "+10 HP", Color.green);
await U.PopOutcome(target, "Miss!");
```

Use `OutcomeAnchor`/`TargetAnchor` children for explicit placement. Config fields and optional marker sprite/prefab/font overrides live in `UConfig`.
