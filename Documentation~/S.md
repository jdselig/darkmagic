# State machines with S

DarkMagic state machines store one active state type per Unity object.

```csharp
public static class PlayerStates
{
    public sealed class Idle { }
    public sealed class Running { }
    public sealed class Attacking { }
}
```

```csharp
public class Player : MonoBehaviour
{
    StateMachine state;

    void Awake()
    {
        state = this.CreateStateMachine();
        state.OnEnter<PlayerStates.Attacking>(() => PlayAttack());
        state.OnExit<PlayerStates.Attacking>(() => StopAttack());
        state.StartIn<PlayerStates.Idle>();
    }

    void Update()
    {
        if (I.GetButtonDown("Fire1"))
            state.Go<PlayerStates.Attacking>();
    }
}
```

Common operations:

```csharp
state.Is<PlayerStates.Idle>();
state.Go<PlayerStates.Running>();
state.TryEnter<PlayerStates.Attacking>();
state.ForceEnter<PlayerStates.Idle>();
state.Reenter<PlayerStates.Attacking>();
state.LockTo<PlayerStates.Attacking>();
state.Unlock();
```

State changes also broadcast `V.StateChanged` and the group-specific `V.StateChanged<TGroup>` event. This keeps animations, audio, and UI optional rather than tightly coupled to gameplay logic.

Enable `StateMachine.Trace` while teaching transitions. `StateMachine.Warnings` keeps invalid or surprising transitions visible in development.
