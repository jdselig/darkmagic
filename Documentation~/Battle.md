# Teaching-friendly battle flow

A turn-based battle is easiest to reason about when the battle manager owns four explicit steps:

1. Pick a command.
2. Pick target(s), if the command needs them.
3. Resolve the command.
4. Advance the turn.

```csharp
public enum BattleCommandType
{
    Fight,
    Magic,
    Defend,
}

public sealed class BattleCommand
{
    public string Name;
    public BattleCommandType Type;
    public int Power;
    public bool TargetsAll;
    public Color OutcomeColor = Color.white;

    public BattleCommand(
        string name,
        BattleCommandType type,
        int power = 0,
        bool targetsAll = false
    )
    {
        Name = name;
        Type = type;
        Power = power;
        TargetsAll = targetsAll;
    }
}
```

Build command payloads once:

```csharp
var menu = new U.Flow.Menu("Battle")
    .AddSelect("Fight", new BattleCommand("Fight", BattleCommandType.Fight, 10))
    .AddSubmenu("Magic", magic =>
    {
        magic.AddSelect(
            new U.Option("Spark", "Hit one enemy."),
            new BattleCommand("Spark", BattleCommandType.Magic, 15)
            {
                OutcomeColor = Color.cyan,
            }
        );

        magic.AddSelect(
            new U.Option("Firewave", "Hit all enemies."),
            new BattleCommand("Firewave", BattleCommandType.Magic, 8, true)
            {
                OutcomeColor = Color.yellow,
            }
        );
    })
    .AddSelect("Defend", new BattleCommand("Defend", BattleCommandType.Defend));
```

Resolve one turn:

```csharp
var decision = await U.Flow.Pick<BattleCommand>(menu);
if (decision.Cancelled) return;

var command = decision.Value.Payload;

if (command.Type == BattleCommandType.Defend)
{
    actor.Stats["DEF"].Buff(5);
    await U.PopOutcome(actor.transform, "Defend!", Color.cyan);
    AdvanceTurn();
    return;
}

if (command.TargetsAll)
{
    var targets = await U.Target.SelectMany(enemies, U.TargetMode.All);
    if (targets.Cancelled) return;

    foreach (var target in targets.Value)
        ResolveDamage(target, command);
}
else
{
    var target = await U.Target.Select(enemies);
    if (target.Cancelled) return;

    ResolveDamage(target.Value, command);
}

AdvanceTurn();
```

`U.Flow.Run` is still useful for prototypes where leaf actions do everything and return to the menu. `U.Flow.Pick<T>` is preferred when exactly one decision should leave the menu and advance the battle.
