# Stats

`Stat` models a readable JRPG-style value:

```text
effective max = Base + TempModifiers + EquipmentModifiers
```

For normal stats, `Current` is that effective value. For resource stats such as HP or MP, `Current` is `Remaining`, capped at the effective maximum.

```csharp
var hp = new Stat(
    "Health",
    "HP",
    120,
    persists: true,
    isLethal: true,
    isResource: true
);

var strength = new Stat("Strength", "STR", 12);
```

## Readable changes

```csharp
hp.Damage(10);
hp.Heal(5);

strength.Buff(4);
strength.Debuff(2);
strength.ModifyEquipment(3);
strength.ModifyBase(1);
```

Operators provide optional sugar:

```csharp
hp -= 10;       // resource damage
hp += 5;        // resource healing
strength += 4;  // temporary buff
strength -= 2;  // temporary debuff
strength >> 1;  // persistent Base change
```

C# has no overloadable `>>>` operator, so `>>` is the persistent-base operator.

## StatBlock

```csharp
var stats = new StatBlock(hp, strength);

stats["HP"] -= 10;
stats["strength"].Buff(2); // names and abbreviations are case-insensitive
stats.AddEquipment("STR", 3);

int currentStrength = stats.GetInt("STR");
```

The indexer includes a setter so compound assignments such as `stats["HP"] -= 10` compile. It keeps the original `Stat` instance rather than duplicating it.

## Typed blocks

Use real fields when dot access is clearer:

```csharp
public sealed class PlayerStats : StatBlock
{
    public Stat HP = new("Health", "HP", 120, persists: true, isLethal: true, isResource: true);
    public Stat MP = new("Magic", "MP", 30, persists: true, isResource: true);
    public Stat STR = new("Strength", "STR", 12);

    public PlayerStats() => AutoRegisterFields();
}
```

## Refresh, levels, and thresholds

```csharp
stats.Refresh();
stats.Refresh(force: true);

stats["HP"].Delta = 5;
stats["HP"].LevelUp(); // raises Base and refills resources to effective max

stats["XP"].Threshold = 100;
stats["XP"].OnThresholdMet += _ => LevelUp();
```

`Threshold <= 0` disables threshold checks. `StatBlock.LevelUp()` applies `LevelUp()` to every registered stat.
