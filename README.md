# DarkMagic

DarkMagic is a student-friendly Unity helper package by John Selig. It keeps common game-dev code readable, low-ceremony, and easy to teach while leaving advanced paths available when a project grows.

Current version: **3.11.1**

Unity support: **6.5+**

Verified with: **Unity 6000.5.8f1**

## Install

In Unity, open **Window → Package Manager → + → Add package from Git URL** and enter:

```text
https://github.com/jdselig/darkmagic.git#v3.11.1
```

For local package development, add this to the consuming project’s `Packages/manifest.json`:

```json
"com.archenemy.darkmagic": "file:/absolute/path/to/darkmagic"
```

DarkMagic includes its own dynamic TMP fonts and minimal fallback settings. Students do not need to import TMP Essential Resources before using `U`, though a project can still provide its own TMP settings or set `UConfig.FontAsset`.

## The map

| Module | Purpose | First API to learn |
|---|---|---|
| `V` | Type-based events | `V.Broadcast<T>()`, `this.On<T>()` |
| `S` / `StateMachine` | Small per-object state machines | `this.CreateStateMachine()` |
| `W` | Unity Awaitable helpers | `await this.Seconds(1)` |
| `I` | Input System/legacy-friendly input | `I.GetButtonDown("Jump")` |
| `X` | Vector and Transform sugar | `transform.SetPosY(2)` |
| `A` | Awaitable animation playback | `await animator.PlayAndWait("Attack")` |
| `U` | Code-first UI | `U.PopBanner`, `U.PopDialogue`, `U.PopChoice` |
| `U.Target` | JRPG-style target selection | `await U.Target.Select(enemies)` |
| `U.Flow` | Nested menus and command payloads | `await U.Flow.Pick<T>(menu)` |
| Stats | JRPG-friendly stats | `Stat`, `StatBlock` |

Focused guides:

- [Events (`V`)](Documentation~/V.md)
- [State machines (`S`)](Documentation~/S.md)
- [Awaitables (`W`)](Documentation~/W.md)
- [Input (`I`)](Documentation~/I.md)
- [UI, targeting, and menu flow (`U`)](Documentation~/U.md)
- [Stats](Documentation~/Stats.md)
- [Battle workflow](Documentation~/Battle.md)
- [Testing and package validation](Documentation~/Testing.md)

## Five-minute start

### Events

```csharp
public sealed class PlayerDamaged : V.Event<int> { }

public class PlayerView : MonoBehaviour
{
    void Start()
    {
        this.On<PlayerDamaged>(damage => Debug.Log($"Took {damage}!"));
    }
}

V.Broadcast<PlayerDamaged>(12);
```

Listeners attached with `this.On` stop automatically when their Unity owner is destroyed.

### State

```csharp
public static class PlayerStates
{
    public sealed class Idle { }
    public sealed class Attacking { }
}

public class Player : MonoBehaviour
{
    StateMachine state;

    void Awake()
    {
        state = this.CreateStateMachine();
        state.StartIn<PlayerStates.Idle>();
    }

    public void Attack() => state.Go<PlayerStates.Attacking>();
}
```

### Awaitables

```csharp
async Awaitable Start()
{
    await this.Seconds(1f); // cancels if this component is destroyed
    Debug.Log("One second later");
}
```

For safe fire-and-forget work:

```csharp
this.Run(async () =>
{
    await this.Seconds(1f);
    Debug.Log("Done");
}, name: "Example");
```

### Input

```csharp
void Update()
{
    float horizontal = I.GetAxis("Horizontal");
    if (I.GetButtonDown("Jump")) Jump();
}
```

Default axes/buttons work with Unity’s Input System or legacy input. Custom mappings can be supplied through `I.Buttons`, `I.Axes`, and `I.AxesRaw`.

### UI

```csharp
await U.PopBanner("Battle start!", 1.25f);
await U.PopDialogue("Welcome, hero.<pbr/>Choose carefully.");

var choice = await U.PopChoice("Your move?", "Fight", "Magic", "Run");
if (!choice.Cancelled)
    Debug.Log(choice.Value);
```

Reactive HUD text:

```csharp
U.IDisplayHandle hpDisplay;

void Start() => hpDisplay = U.Display(() => "HP " + stats["HP"].Current);
void OnDestroy() => hpDisplay?.Dispose();
```

Floating combat text:

```csharp
U.PopOutcome(targetTransform, 125);
U.PopOutcome(targetTransform, "+50 HP", Color.green);
await U.PopOutcome(targetTransform, "Miss!");
```

`PopOutcome` chooses its world position in this order:

1. Child named `OutcomeAnchor`.
2. `Collider2D` top-center.
3. `Collider` top-center.
4. Target pivot plus `UConfig.OutcomeWorldOffsetY`.

### Targeting

```csharp
var target = await U.Target.Select(enemies);
if (target.Cancelled) return;

var all = await U.Target.SelectMany(enemies, mode: U.TargetMode.All);
var exact = await U.Target.SelectMany(
    enemies,
    mode: U.TargetMode.Exact,
    count: 2
);
```

`Select` supports `Transform` collections, a party parent’s direct children, or typed lists with a filter:

```csharp
var living = await U.Target.Select(enemyUnits, filter: enemy => enemy.HP > 0);
```

### Stats

```csharp
var stats = new StatBlock(
    new Stat("Health", "HP", 120, persists: true, isLethal: true, isResource: true),
    new Stat("Strength", "STR", 12)
);

stats["HP"] -= 10;  // resource: damage Remaining
stats["HP"] += 5;   // resource: heal Remaining
stats["STR"] += 5;  // normal stat: temporary buff

Debug.Log(stats["HP"].Current);  // 115
Debug.Log(stats["STR"].Current); // 17
```

Normal stat `Current` is:

```text
Base + TempModifiers + EquipmentModifiers
```

Resource stat `Current` is `Remaining`, capped at that effective maximum.

Named helpers are available when they read better in class:

```csharp
stats["STR"].Buff(5);
stats["STR"].Debuff(2);
stats["STR"].ModifyEquipment(3);
stats.AddEquipment("STR", 1);
```

## Battle command flow

`U.Flow.Run(root)` is intentionally an action-menu loop: after a leaf action finishes, it returns to the same menu. For one-command-per-turn battles, let the battle manager own resolution:

```csharp
public enum BattleCommand { Fight, Defend }

var menu = new U.Flow.Menu("Battle")
    .AddSelect("Fight", BattleCommand.Fight)
    .AddSelect("Defend", BattleCommand.Defend);

var decision = await U.Flow.Pick<BattleCommand>(menu);
if (decision.Cancelled) return;

var target = await U.Target.Select(enemies);
if (target.Cancelled) return;

Resolve(decision.Value.Payload, target.Value);
AdvanceTurn();
```

The teaching sequence is:

1. Pick a command.
2. Pick target(s) when needed.
3. Resolve the command.
4. Advance the turn.

See [Battle workflow](Documentation~/Battle.md) for a complete payload example.

## Configuration and samples

Package Manager exposes three optional samples:

- **Config**: classroom-friendly configuration files for V, S, W, I, U, and typed Stats.
- **Input Starter**: an Input Actions asset and optional bridge into `I`.
- **UI Starter**: banners, dialogue, choices, and a reactive HUD.

Most projects can begin with no setup. Import a sample when students need one obvious place to customize behavior.

The Config sample compiles in its own assembly, so its global event/state names and `DarkMagic` settings are checked across a real Unity assembly boundary. Its U overrides are preserved and applied in Editor, Mono, and IL2CPP builds.

## Compatibility

DarkMagic targets Unity `6000.5` and supports Unity 6.5 and newer.

Version 3.11.1 is verified in the Unity 6000.5.8f1 Editor and in standalone macOS Mono and IL2CPP players.

Unity 6.5 deprecated/error-gated `Object.GetInstanceID()`. DarkMagic’s internal owner registries instead use `RuntimeHelpers.GetHashCode(owner)`. These keys are only for runtime bookkeeping and must not be used as gameplay IDs or save data.

## Testing

The package contains EditMode and PlayMode tests. To run them from a test project:

1. Reference this package with a local `file:` dependency.
2. Add `"com.archenemy.darkmagic"` to the project manifest’s `testables` array.
3. Close the Unity editor for that project.
4. Run:

```bash
Scripts~/validate.sh /absolute/path/to/DarkMagicTest
```

The validator checks compatibility APIs and release-version consistency, compiles the package, then runs EditMode and PlayMode tests. Set `UNITY_PATH` if Unity Hub’s newest installed editor should not be used.

## Design rules

- Prefer memorable, readable APIs over maximal configurability.
- Make the common classroom path zero-config.
- Keep advanced escape hatches optional.
- Avoid hidden architecture that makes student debugging harder.
- Update docs, samples, tests, and package version whenever behavior changes.

## License

DarkMagic is available under the [MIT License](LICENSE).
