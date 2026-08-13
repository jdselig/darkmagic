# Awaitables with W

`W` adds student-friendly lifetime and error handling around Unity `Awaitable`.

## Direct waits

```csharp
await this.NextFrame();
await this.EndOfFrame();
await this.FixedUpdate();
await this.Seconds(1.5f);
await this.Until(() => ready);
await this.While(() => moving);
```

These extension methods use a cancellation token tied to the Unity owner. Destroying the component or GameObject cancels the wait.

## Scoped style

```csharp
await this.W().Seconds(1f);
await this.W().Until(() => enemyReady);
```

Use whichever style reads best for the class.

## Safe fire-and-forget

```csharp
this.Run(async token =>
{
    await Awaitable.WaitForSecondsAsync(1f, token);
    SpawnEnemy();
}, name: "Delayed spawn");
```

`Run` reports unexpected exceptions instead of silently losing them. `W.Trace` and `W.Guardrails` add development diagnostics.

## Composition

```csharp
await W.All(LoadMap(), LoadParty());
int first = await W.Any(WaitForConfirm(), WaitForTimeout());

bool finished = await LongAction().Timeout(this, 3f);
await this.Seconds(1f).Then(() => Debug.Log("Done"));
```

Use `.Forget("label")` only when an Awaitable truly has no owner and cannot use `this.Run`.
