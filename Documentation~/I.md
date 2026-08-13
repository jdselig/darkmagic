# Input with I

`I` keeps familiar Unity input calls available across new Input System and legacy-input projects.

```csharp
if (I.GetKeyDown(KeyCode.Space)) Jump();
if (I.GetMouseButtonDown(0)) Select();

float horizontal = I.GetAxis("Horizontal");
bool jump = I.GetButtonDown("Jump");
```

Default mappings cover common axes and buttons with no setup. When the Input System is present, `I` reads it through a compatibility layer. When only legacy input is enabled, it falls back to `UnityEngine.Input`.

## Custom mappings

```csharp
I.Buttons["Interact"] = () =>
{
    bool down = I.GetKeyDown(KeyCode.E);
    bool held = I.GetKey(KeyCode.E);
    bool up = I.GetKeyUp(KeyCode.E);
    return (held, down, up);
};

I.Axes["Throttle"] = () => Mathf.Clamp01(myThrottle);
I.AxesRaw["Throttle"] = () => myThrottle;
```

Custom names are case-insensitive and take priority over built-in Input System or legacy mappings.

Import **Input Starter** from Package Manager when students want a visible Input Actions asset. Its optional bridge feeds actions back into the same `I.GetButton`/`I.GetAxis` calls.

`I.WarnOnFallback` explains missing mappings once instead of throwing. `I.Trace` adds development diagnostics.
