# Events with V

`V` uses event types instead of string names. Define events in one obvious config file:

```csharp
public sealed class GameStarted : V.Event { }
public sealed class PlayerDamaged : V.Event<int> { }
public sealed class ItemStolen : V.Event<string, string> { }
```

Broadcast them from anywhere:

```csharp
V.Broadcast<GameStarted>();
V.Broadcast<PlayerDamaged>(12);
V.Broadcast<ItemStolen>("Potion", "Goblin");
```

## Owner-scoped listeners

Inside a `MonoBehaviour`, prefer extension methods:

```csharp
this.On<GameStarted>(() => Begin());
this.Once<GameStarted>(() => ShowTutorial());
this.On<PlayerDamaged>(value => Debug.Log(value));
```

The Unity object becomes the listener owner. Destroyed owners are removed automatically.

For strongly typed payload arithmetic, supply the payload generic explicitly:

```csharp
this.On<PlayerDamaged, int>(damage => health -= damage);
```

Global listeners are supported but need intentional lifetime management:

```csharp
using var subscription = V.OnDisposable<PlayerDamaged, int>(OnDamage);
```

## Payload count

V supports zero through three payload values:

```csharp
public sealed class Moved : V.Event<Vector3> { }
public sealed class Trade : V.Event<string, int> { }
public sealed class Hit : V.Event<GameObject, int, bool> { }
```

Use the matching typed overload for maximum compile-time checking:

```csharp
V.Broadcast<Hit, GameObject, int, bool>(enemy, 10, true);
```

## Guardrails

`V.Guardrails` warns in Editor/development builds about likely global or duplicate subscriptions. `V.Trace` logs broadcasts. Both default to classroom-safe behavior and can be configured through the Config sample.

Internal owner keys use `RuntimeHelpers.GetHashCode`. They are runtime bookkeeping only—not gameplay identity or save data.
