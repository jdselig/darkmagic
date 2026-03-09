# DarkMagic
### Making Unity better, by any means necessary 😈

---

## Quickstart (Unity Package Manager)

1) **Add the package from Git URL**

In Unity: **Window → Package Manager → + → Add package from git URL...**

Paste:

```text
https://github.com/jdselig/darkmagic.git#v3.6.0
```

2) **Import Samples (recommended)**
- In Package Manager, select **DarkMagic**
- Expand **Samples**
- Import **Config** (and **UI Starter** / **Input Starter** if you want the optional starters)

Then copy the imported **Config** folder into your project’s `Assets/` folder (so it becomes `Assets/Config/...`).

3) **TMP Essentials (UI module)**
If you see a TMP prompt, click **Import TMP Essentials**.
If you don’t get a prompt, but UI text looks missing, go to:
**Window → TextMeshPro → Import TMP Essential Resources**.

4) **Input System (I module)**
`I` uses the **New Input System** under the hood.
If your project doesn’t have it enabled, install/enable **Input System** and set:
**Project Settings → Player → Active Input Handling → Input System Package (New)** (or **Both**).

That’s it. You can now use:
- `V` (events), `StateMachine` (states), `W` (awaitable helpers), `I` (inputs), `U` (UI), `X` (utilities).

**V** is a tiny, DX-first toolkit for prototyping in Unity:
- A strongly-typed event bus (**V**) where events are **types**
- A simple, flexible state machine (**S**)

Designed for students and rapid iteration: minimal ceremony, helpful guardrails.

---

## Install (Unity Package Manager)

Unity → Window → Package Manager → + → Add package from git URL...

Use:
- `https://github.com/<your-org-or-user>/<your-repo>.git?path=com.archenemy.darkmagic`
- Or tag a release: `...git?path=com.archenemy.darkmagic#2.0.0`

---

## The DarkMagic Map

Use these three ideas to keep your brain tidy:

1) **V**: *What happened?* (events)
2) **S**: *What mode are we in?* (state)
3) **W**: *Wait for time/frames/conditions* (async helpers)
4) **I**: *Input, old-school style* (Input System wrapper)
5) **X**: *Tiny utilities* (vectors/transforms)
6) **A**: *Animation awaitables* (Animator helpers)
7) **U**: *Code-first UI* (banners, dialogue, choices, reactive displays)

**W** is optional: a safe async runner + tracing.

> Tip: After importing the **Config** sample, copy the **Config** folder into your project’s `Assets/` so you can edit the configs safely.


# Event Bus (V)

## Quick Start

Package Manager → **V** → **Samples** → Import **“VConfig”**

This adds `VConfig.cs`, where students:
- toggle `V.Trace` (Editor/Dev only)
- define event types (one class per event)

### Define events (in VConfig.cs)
```csharp
public sealed class GameStart : V.Event { }
public sealed class PlayerDamaged : V.Event<int> { }
```

### Broadcast
```csharp
V.Broadcast<GameStart>();
V.Broadcast<PlayerDamaged>(12);

// or, if you're feeling dramatic:
this.Yell<PlayerDamaged>(12);
```

### Listen
```csharp
this.On<GameStart>(() => Debug.Log("starting..."));

this.On<PlayerDamaged>(dmg => Debug.Log($"{dmg} damage!"));
```

### Once
```csharp
this.Once<GameStart>(() => Debug.Log("first start only"));
this.Once<PlayerDamaged>(dmg => Debug.Log($"first hit: {dmg}"));
```

### 2–3 payload events
```csharp
public sealed class ItemStolen : V.Event<Item, Character, Character> { }

V.Broadcast<ItemStolen>(item, stealer, stolenFrom);

this.On<ItemStolen>((item, stealer, stolenFrom) =>
{
    Debug.Log($"{stealer} stole {item} from {stolenFrom}");
});
```

---

# State Machine (S)

A **V-style** state machine:
- No hierarchy
- States are **types** (usually nested under `S.*`)
- `Enter`, `OnEnter`, `OnExit`, `OnChange`
- Locks (`Lock`, `LockTo`)
- Guardrails (warnings) + trace logs (Editor/Dev builds only)

## Quick Start

Package Manager → **V** → **Samples** → Import **“VStateConfig”**

This adds `SConfig.cs`, where students:
- toggle state trace + warnings
- define state types in `S`

### Define states (in SConfig.cs)
```csharp
public static   class S
{
    public static class Player
    {
        public readonly struct Idle { }
        public readonly struct Falling { }
    }
}
```

## Use it in a MonoBehaviour

To get the clean syntax `S.Enter<...>()`, inherit from `VStateBehaviour`:

```csharp
using UnityEngine;

public class PlayerController : VStateBehaviour
{
    void Awake()
    {
        S.Enter<S.Player.Idle>();

        S.OnEnter<S.Player.Falling>(() => Debug.Log("weeeee"));
        S.OnExit<S.Player.Falling>(() => Debug.Log("landed"));
        S.OnChange((from, to) => Debug.Log($"{from?.Name ?? "(none)"} -> {to.Name}"));
    }

    void Update()
    {
        if (!IsGrounded())
            S.Enter<S.Player.Falling>();
    }

    bool IsGrounded() => true;
}
```

## State changes as V events (optional)

Every transition broadcasts:
- `V.StateChanged` (anything, anywhere)
- `V.StateChanged<S.Player>` (only states nested under `S.Player`)

Listen like:

```csharp
this.On<V.StateChanged<S.Player>>(e =>
{
    Debug.Log($"Player state: {e.From?.Name ?? "(none)"} -> {e.To?.Name}");
});
```

Or the sugar wrapper:

```csharp
this.OnStateChanged<S.Player>(e => Debug.Log("Player state changed!"));
```

---


---


---

# W (Awaitable helpers)

Unity’s `Awaitable` is already great. **W** just makes it **faster to type** and **harder to mess up**.

> These helpers are from this package (extension methods), not Unity built-ins.

## Wait helpers (auto-cancel on destroy)

```csharp
await this.Seconds(1f);
await this.NextFrame();
await this.EndOfFrame();
await this.FixedUpdate();
```

## Wait until / while

```csharp
await this.Until(() => isGrounded);
await this.While(() => !isGrounded);
```

## Safe fire-and-forget

If you truly want to start something and not await it:

```csharp
this.Seconds(2f).Then(() => Debug.Log("two seconds later")).Forget(this);
```

Or:

```csharp
SomeAsync().Forget(this);
```

`Forget`:
- ignores cancellations
- logs exceptions (instead of silently swallowing them)

## Then (tiny continuation)

```csharp
await this.Seconds(1f).Then(() => Debug.Log("done"));
```

## Timeout

Timeout returns `true` if the awaitable completed before the timeout, otherwise `false`.

```csharp
bool ok = await this.Until(() => ready).Timeout(2f, W.TokenFor(this));
if (!ok) Debug.LogWarning("timed out waiting for ready");
```

## Await.All / Await.Any

```csharp
await Await.All(
    this.Seconds(1f),
    this.Seconds(2f)
);

int first = await Await.Any(
    this.Seconds(1f),
    this.Seconds(2f)
);
// first == 0 means the 1-second wait finished first
```

Notes:
- `All` is straightforward.
- `Any` is implemented with a lightweight polling runner (Unity Awaitable doesn't provide a built-in WhenAny).

> Tip: Start with **W**. If you want extra features like tracing + a safe runner, use **W** below.



---

# Input (I)
## What I is

`I` is a **tiny wrapper** that lets you write *old-school Input code* while keeping you on the **new Input System** when it’s available.

```csharp
void Update()
{
    if (I.GetKeyDown(KeyCode.Space))
        Debug.Log("Jump!");
}
```

## How I works (very specifically)

`I` tries to read input in this order:

1) **New Input System (preferred)**
   If the Input System package is present at runtime, `I` reads directly from devices like:
   - Keyboard
   - Mouse
   - Gamepad
   - Touchscreen
   It does this via a small **reflection bridge**, so projects without the Input System still compile.

2) **Fallback: legacy `UnityEngine.Input`**
   If the Input System package is not present, `I` calls the legacy `Input.*` APIs.

### Warning behavior (soft-require)

In **Editor / Development builds**, if `I` ever has to fall back, it logs a warning **once**:
- “Input System not found, falling back to legacy Input…”
- Toggle this with `I.WarnOnFallback` (or via `IConfig` in the Config sample).

> In Release builds, no warning is logged.

## What works out of the box (no setup)

### Keys
```csharp
I.GetKey(KeyCode.W);
I.GetKeyDown(KeyCode.Space);
I.GetKeyUp(KeyCode.Escape);
I.anyKey;
I.anyKeyDown;
```

### Mouse
```csharp
I.GetMouseButton(0);
I.GetMouseButtonDown(0);
I.GetMouseButtonUp(0);

Vector3 p = I.mousePosition;
Vector2 wheel = I.mouseScrollDelta;
```

### Touch (subset)
```csharp
int c = I.touchCount;
if (c > 0)
{
    Touch t = I.GetTouch(0);
}
```

## “Legacy-style” Buttons & Axes (zero-setup defaults)

The old Input Manager had lots of named mappings (`"Jump"`, `"Horizontal"`, etc).
The new Input System expects you to define actions in an asset.

To keep prototyping painless, `I` includes **sane defaults** for:

- Buttons: `"Jump"`, `"Fire1"`, `"Fire2"`, `"Submit"`, `"Cancel"`
- Axes: `"Horizontal"`, `"Vertical"`

Examples:
```csharp
float x = I.GetAxis("Horizontal");      // smoothed-ish
float y = I.GetAxisRaw("Vertical");     // snapped

if (I.GetButtonDown("Jump")) { }
if (I.GetButton("Fire1")) { }
```

### Customizing buttons/axes (recommended path)

Use `IConfig` (in the **Config** sample) to override anything:

```csharp
I.Buttons["Jump"] = () => (
    I.GetKey(KeyCode.Z),
    I.GetKeyDown(KeyCode.Z),
    I.GetKeyUp(KeyCode.Z)
);
```

---

## Optional: Input Actions starter sample

If you want a real Input Actions asset (rebinding, nicer gamepad feel, cleaner scaling), import the **Input Starter** sample.

It includes:
- `V_InputStarter.inputactions` (a small default action map)
- `InputStarter` component (simple reads like `Move`, `JumpDown`)
- optional `IInputActionsBridge` (feeds those actions into `I.GetAxis/GetButton`)

### Import
Package Manager → **V** → **Samples** → Import **Input Starter**

### Use InputStarter
```csharp
var input = FindFirstObjectByType<InputStarter>();
Vector2 move = input.Move;
if (input.JumpDown) Debug.Log("Jump!");
```

### Bridge actions into I (optional)
Add `IInputActionsBridge` to a GameObject (it will auto-find `InputStarter` if left unassigned).

Now your old-school calls can be powered by actions:
```csharp
float x = I.GetAxis("Horizontal");
if (I.GetButtonDown("Jump")) { }
```

### How to change or update actions

1. Open `V_InputStarter.inputactions` and edit bindings/actions.
2. Keep the action names the same (`Move`, `Look`, `Jump`, `Fire`, `Sprint`, `Pause`) unless you also update `InputStarter.cs`.
3. If you rename an action, update `FindAction("Name")` calls in `InputStarter.cs`.

# Async the W Way (W)

**W** is a tiny layer of sugar on Unity's `Awaitable` to make async/await feel like "coroutines, but nicer":
- **Auto-cancellation**: if you call from a `MonoBehaviour`, waits cancel when it’s destroyed
- **Run(...)**: a safe fire-and-forget runner that logs exceptions
- Optional **trace** + **guardrails** (Editor/Dev builds only)

## Quick Start

Package Manager → **V** → **Samples** → Import **“WConfig”**

## Wait helpers

Two styles:

### A) Scoped (explicit)
```csharp
await this.W().Seconds(0.5f);
await this.W().NextFrame();
await this.W().EndOfFrame();
await this.W().FixedUpdate();
```

### B) Direct (shortest)
```csharp
await this.Seconds(0.5f);
await this.NextFrame();
```

## Wait for a condition

```csharp
await this.Until(() => isGrounded);
await this.While(() => !isGrounded);
```

## Run: fire-and-forget without silent explosions

```csharp
this.Run(async (ct) =>
{
    await Awaitable.WaitForSecondsAsync(1f, ct);
    V.Broadcast<PlayerDamaged>(12);
}, name: "DamageAfterDelay");
```

Notes:
- `ct` auto-cancels on destroy (for MonoBehaviours)
- Exceptions are logged (cancellation is treated as normal)

## Optional: V events for async tracing

When `W.Run` starts/finishes/cancels/fails, it broadcasts:
- `V.WStarted`
- `V.WFinished`
- `V.WCanceled`
- `V.WFailed`

Example:
```csharp
this.On<V.WFailed>(info => Debug.LogError($"Async failed: {info}"));
```


## Common Pitfalls

### A) Subscribing in Update
Don’t call `this.On(...)` or `S.OnEnter(...)` inside `Update()`.
That creates duplicates every frame.

✅ Subscribe once in `Awake`, `Start`, or `OnEnable`.

### B) “Immortal” listeners
If you subscribe using the core API without an owner, it may live forever.

✅ In MonoBehaviours, prefer:
- `this.On(...)`
- `this.Once(...)`

### C) Ordering assumptions
Events and hooks don’t guarantee ordering.
If you need strict sequencing or frame-perfect timing, use direct calls/state machines/explicit update order.

### D) “It’s firing twice!”
Most common cause: you subscribed twice. Check your subscription locations.

---

## The Three Rules (for students)

1) **Events are “what happened,” not “what to do.”**
2) **Subscribe once (Awake/Start/OnEnable), never in Update.**
3) **In MonoBehaviours, use `this.On` / `this.Once` so listeners die cleanly.**


---

# Utilities (X)

These are small helpers aimed at the most common “wait why can’t I…” moments for new Unity devs.

## Vector SetX / SetY / SetZ (returns a new vector)

```csharp
transform.position = transform.position.SetY(5f);   // keep X/Z, change Y
rb.velocity = rb.velocity.SetX(0f);                 // keep Y/Z, change X
```

Also available:
- `AddX/AddY/AddZ` for `Vector3`

## Transform helpers (mutate Transform directly)

```csharp
transform.SetPosY(2f);
transform.AddPosX(1f);
transform.ResetLocal();

transform.LookAt2D(target.position); // rotates around Z so +X faces target
```


---

# Animation awaitables (A)

Waiting for animations is a classic Unity headache. These helpers let you do it with `await`:

```csharp
await animator.PlayAndWait("Attack");
```

Or, if you already started the state:

```csharp
await A.WaitForAnimation(animator, "Attack");
```

## Important notes

- `stateName` should match the **Animator STATE name** (or full path), not necessarily the clip name.
- This waits until:
  - the Animator is in that state (on the given layer)
  - `normalizedTime >= 1`
  - and the Animator is **not** in a transition

## Cancellation (optional)

If you want cancellation, pass a token:

```csharp
await animator.PlayAndWait("Attack", cancellationToken: token);
```


---

# UI (U)

U is a **code-first**, student-friendly UI layer built on **uGUI + TextMeshPro** (TMP required).
It’s designed for fast prototyping: banners, dialogue, choices/menus, and simple reactive HUD displays.

## Core ideas

- **One modal at a time** (awaitable): `Pop*` methods.
- **Many displays** at once (persistent HUD): `Display`.
- **Back/Cancel** is always supported and returns a result object (no exceptions for students).
- U uses your `I` wrapper for input by default (Enter/Space/Click for confirm; Esc/Backspace/Right-click for cancel).

## Banners

```csharp
await U.PopBanner("A party of goblins attacks!", placement: U.Placements.TopCenter);
```

## Dialogue (auto paginated)

```csharp
await U.PopDialogue( // defaults to left-aligned text
"Long dialogue text... U will split it into pages and wait for confirm/cancel.");
```

Pagination is controlled by `UConfig.DialogueMaxCharsPerPage (auto-scales a bit with dialogue font size / modal height)`.

## Choices / Menus

```csharp
var result = await U.PopChoice("Do you accept this quest?", "Yes", "Nope");

if (!result.Cancelled && result.Value == "Yes")
{
    // ...
}
```

Menu is just sugar over choice:

```csharp
var cmd = await U.Menu("Knight", "Fight", "Magic", "Defend", "Item");
```

### Result behavior (important)

Every modal returns a `U.Result<T>`:

- `result.Cancelled == true` if the player backed out
- `result.Value` is the chosen string (when not cancelled)

## Reactive HUD / Displays

If you want text to update automatically, pass a **lambda**:

```csharp
var scoreHud = U.Display(() => $"SCORE: {score}", placement: U.Placements.TopLeft);

// later
scoreHud.Dispose(); // removes it
```

Why a lambda? Because C# can’t “see” which variables were used inside a string after it’s already been evaluated.
A `Func<string>` lets U re-evaluate the text periodically.

## Hooks (V events)

U broadcasts V events so students can hook sound effects, analytics, etc:

- `U.DialoguePopped` (string: the page text shown)
- `U.ChoiceMade` (string: the selected option label)
- `U.ChoiceCanceled` (string: the prompt/title)

Example:

```csharp
this.On<U.ChoiceMade>(choice => Debug.Log("SFX: click " + choice));
```

## Styling and defaults (UConfig)

U is opinionated by default, but most knobs live in `UConfig`:

- sizes (banner/dialogue/modal percent of screen)
- colors
- max chars per page
- key mappings
- optional TMP font (`UConfig.Font`)

Import the **Config** sample and copy `Config/` into `Assets/`, then edit `UConfig.cs`.


## Targeting (v1: FF-style cycling)

U.Target lets you select from a list without raycasts (great for turn-based RPGs).
You provide a list and a way to get each target's Transform.

```csharp
var target = await U.Target.Single(enemies, e => e.transform);
if (!target.Cancelled)
    Debug.Log("Chose: " + target.Value.name);
```

Keys:
- Left/Right arrows (or A/D) to cycle targets
- Confirm / Cancel as usual


## Choice icons (optional)

You can pass `U.Option` values with optional sprites:

```csharp
U.Option fire = new U.Option("Fire", fireSprite);
U.Option ice  = new U.Option("Ice", iceSprite);

var spell = await U.PopChoice("Magic", fire, ice, "Back");
```


## Menu flow helper (U.Flow)

U.Flow is for **nested menus** (Magic → Spell list → Back) with minimal boilerplate.
It supports **icons** (via `U.Option`) and optional **descriptions**.

### A) Run: execute immediately (simple)

```csharp
var root = new U.Flow.Menu("Knight")
    .Add("Fight", async () => { await U.PopBanner("Fight!"); })
    .AddSubmenu("Magic", magic =>
    {
        magic.Description = label => label switch
        {
            "Firewave" => "Hit all enemies.",
            "Spark" => "Hit one enemy.",
            _ => ""
        };

        magic.Add(new U.Option("Firewave", fireIcon), async () => { await U.PopBanner("Firewave!"); });
        magic.Add(new U.Option("Spark", sparkIcon), async () => { await U.PopBanner("Spark!"); });
    })
    .Add("Defend", async () => await U.PopBanner("Defend!"));

await U.Flow.Run(root);
```

Rules:
- Selecting an **action** returns you to the current menu (classic JRPG behavior).
- **Cancel** exits the root menu.
- In submenus, U.Flow adds **Back** automatically (and Cancel also goes back).

### B) Pick<T>: return a decision payload (advanced)

Use `AddSelect<T>` entries to return a payload you execute later:

```csharp
// Example: spells are ScriptableObjects (MagicCommand)
var magicMenu = new U.Flow.Menu("Magic");

foreach (var spell in knownSpells)
    magicMenu.AddSelect(new U.Option(spell.DisplayName, spell.Icon), spell);

magicMenu.Description = label => knownSpells.Find(s => s.DisplayName == label)?.Description;

var pick = await U.Flow.Pick<MagicCommand>(magicMenu);
if (!pick.Cancelled)
{
    var spell = pick.Value.Payload;
    // Choose targets, then execute in your battle resolver.
}
```

## Targeting: single vs party

Use `U.Target.Party(...)` when an option can target a single enemy OR the whole enemy party:

```csharp
var t = await U.Target.Party(enemies, e => e.transform);

if (!t.Cancelled)
{
    // If the player chose ALL, you'll get the full list back.
    // If the player chose one enemy, you'll get a list with 1 entry.
    foreach (var enemy in t.Value)
        Debug.Log("Hit: " + enemy.name);
}
```



---

# Events: Broadcast payload quick note

If you want the very student-friendly syntax:

```csharp
V.Broadcast<PlayerDamaged>(12);
```

you must use the **single-generic Broadcast overload** (added in v2.6.1).
It runtime-validates that `PlayerDamaged : V.Event<int>` and then publishes.

(Older C# rules don’t allow “ ly specifying” generic type arguments, so the previous `Broadcast<TEvent, T>(T payload)` requires writing `Broadcast<PlayerDamaged, int>(12)`.)


## Choice descriptions (optional)

If you pass `description: ...`, U will show a small panel under the list and update it as selection changes:

```csharp
var pick = await U.PopChoice(
    "Magic",
    new [] { new U.Option("Firewave"), new U.Option("Spark"), new U.Option("Back") },
    description: label => label switch
    {
        "Firewave" => "Hit all enemies.",
        "Spark" => "Hit one enemy.",
        _ => ""
    }
);
```


## Input (I): Axis/Button note (Unity 6 default Input System)

If your project uses **Input System Package** mode (Unity 6 default), Unity throws if you call legacy `UnityEngine.Input.GetAxis`.

`I.GetAxis(...)` and `I.GetButton(...)` now do this:

- If Legacy Input Manager is enabled: use the legacy Input Manager.
- If Legacy Input Manager is disabled: use **Input System** under the hood with a few default legacy-style name mappings.

Default axis mappings:
- `"Horizontal"` / `"horizontal"`: A/D, Left/Right arrows, Gamepad left stick X, Gamepad dpad X
- `"Vertical"` / `"vertical"`: W/S, Up/Down arrows, Gamepad left stick Y, Gamepad dpad Y
- `"Mouse X"`, `"Mouse Y"`: Mouse delta

Default button mappings:
- `"Jump"`: Space, Gamepad South
- `"Fire1"`: Mouse0, Gamepad West
- `"Fire2"`: Mouse1, Gamepad East
- `"Submit"`: Enter, Gamepad South
- `"Cancel"`: Escape, Gamepad East

Turn off warnings for unknown names: `IConfig.WARN_ON_FALLBACK = false`.



### V.On payload tip (why `this.On<MyEvent>(x => ...)` sometimes errors)

In C#, lambdas must convert to a *specific delegate type* (like `Action<int>`). A lambda does **not** automatically convert to `System.Delegate`,
so this will not compile if the only visible overload is `On<TEvent>(Action handler)`:

```csharp
this.On<PlayerDamaged>(dmg => { }); // ❌ "Action does not take 1 arguments"
```

Use one of these (both IL2CPP-safe):

```csharp
// Option A (recommended): specify the payload type as a 2nd generic arg
this.On<PlayerDamaged, int>(dmg => { });

// Option B: keep 1 generic arg, but give the lambda an explicit delegate type via V.A<T>()
this.On<PlayerDamaged>(V.A<int>(dmg => { }));
```



### If you see duplicate-definition or ambiguous-call errors after updating
If Unity reports duplicate or ambiguous members from old file paths, delete the package from `Library/PackageCache/` (or restart Unity), then re-add the package.


### V.On payload shorthand options

Sometimes you want the shortest possible listener:

```csharp
public sealed class PlayerDamaged : V.Event<int> { }

this.On<PlayerDamaged>(dmg =>
{
    // dmg is `object` here, cast if needed:
    int total = 2 + (int)dmg;
    Debug.Log(total);
});
```

V supports three IL2CPP-safe ways to listen to payload events:

- **Option A (strongly typed):** `this.On<MyEvent, int>(x => ...)`
- **Option B (single generic + helper):** `this.On<MyEvent>(V.A<int>(x => ...))`
- **Option C (single generic + cast):** `this.On<MyEvent>(x => { var v = (int)x; ... })`  *(uses Action<object> under the hood; value types box)*



## U (UI)

U is a code-first, student-friendly UI helper (uGUI + TextMeshPro).

### Quick start

```csharp
using DarkMagic;
using TMPro;

public class Example : MonoBehaviour
{
    int score;

    async void Start()
    {
        // Persistent HUD (reactive)
        U.Display(() => $"SCORE: {score}", U.Placements.TopLeft);

        // Timed banner
        await U.PopBanner("A party of goblins attacks!", 2.5f);

        // Dialogue (auto pages)
        await U.PopDialogue( // defaults to left-aligned text
"Wow, I never see people come all the way out here...\n\nStay safe.");

        // Choice (returns selected label)
        var r = await U.PopChoice("Got it?", "Yes", "No");
        if (r.Ok) Debug.Log($"Player chose: {r.Value}");
    }
}
```

### Pop vs Display

- **Pop\*** = modal, awaits confirm/cancel/selection.
- **Display** = non-modal, stays on screen and updates via a `Func<string>`.

### PopBanner

```csharp
await U.PopBanner("Hello!");
await U.PopBanner("Timed", 1.25f);
await U.PopBanner("Bottom right", placement: U.Placements.BottomRight);
```

### PopDialogue

Dialogue auto-pages based on `UConfig.DialogueMaxChars`.

```csharp
await U.PopDialogue( // defaults to left-aligned text
"Long message that may span multiple pages...");
```

### PopChoice

- **Cancel/back** (Esc / right-click / etc.) defaults to selecting the **last** option.

```csharp
var choice = await U.PopChoice(
    "SPACETEST! Arrows to move, spacebar to shoot, X for a bomb. Got it?",
    "Yes", "No"
);

if (choice.Ok && choice.Value == "Yes")
{
    // start game
}
```

### Options with icons + descriptions

Use `U.Option` when you want icons and/or descriptions.

```csharp
var r = await U.PopChoice(
    "Pick a spell:",
    new U.Option("Firewave", description: "Hits all enemies", icon: fireSprite),
    new U.Option("Ice", description: "Single target")
);

if (r.Ok) Debug.Log(r.Value);
```

If you provide descriptions, U shows a small description panel under the list and updates it as selection changes.

### Text overrides (optional)

All Pop methods support:
- `textSize: int?`
- `textColor: Color?`
- `textAlign: TextAlignmentOptions?`

```csharp
await U.PopBanner("Big yellow", textSize: 36, textColor: Color.yellow, textAlign: TextAlignmentOptions.Center);
await U.PopDialogue( // defaults to left-aligned text
"Left aligned cyan", textColor: Color.cyan);
var r = await U.PopChoice("Pick one:", textAlign: TextAlignmentOptions.Center, "Yes", "No");
```

### Placement rules

Placements affect:
- panel anchor + pivot + offsets
- default text alignment (Right placements right-align, Center placements center-align)

### Events (hooks)

U broadcasts V events so students can hook sound effects, etc.

- `DialoguePopped` when a dialogue/banner is shown
- `ChoiceMade` when a choice is selected

### Configuration

Defaults live in `UConfig`. Students should copy `Samples~/Config/UConfig.cs` into `Assets/Config` to override.

Common knobs:
- `TextColor`, `SelectedColor`, `SelectedTextColor`
- `BodyFontSize`, `TitleFontSize`, `ChoiceFontSize`, `DescFontSize`
- `ChoiceSpacingPx`
- `DialogueMaxChars`, `BannerMaxChars`
- `ReferenceResolution`


### Text markup (TMP rich text + DarkMagic extras)

U uses TextMeshPro text components, so **TMP rich text tags work out of the box** in `PopDialogue`, `PopBanner`, and `PopChoice` text.

That means you can do things like:

```csharp
await U.PopDialogue(
    "Hm…? <size=40>HELLO!</size> Welcome to <b>The Fortress</b>! " +
    "My name is <color=#FFD700>Edward</color>, I’ll be your <i>trainer</i>."
);
```

DarkMagic adds a few tiny conveniences:

- `<br/>` inserts a newline (equivalent to `\n`)
- `<pbr/>` forces a **page break** for paginated dialogue (the text after it starts on the next page)
- `<color=Colors.gold> ... </color>` converts to a TMP hex color using your UI config's gold-ish highlight color

Example:

```csharp
await U.PopDialogue(
    "Line one<br/>Line two<pbr/>New page! <color=Colors.gold>Shiny!</color>"
);
```

Notes:
- Markup inside your string always wins over defaults and config (for that part of the text).
- If you use `<pbr/>` in banners, it is treated like a blank line separator (banners don't paginate by default).

## Quick logging helpers

```csharp
V.L("Hello!");     // bold navy
V.Log("Same");     // alias
```

(Uses Unity rich-text tags. You can change the color later in `VLog.cs`.)


## UConfig: making your changes actually apply

`UConfig` lives inside the package assembly. If you edit the copy in Samples, it won't affect runtime until you copy it into your project.

**Recommended workflow:**
1) In Package Manager, import the Samples.
2) Copy `Samples/Config` into your project: `Assets/Config`
3) Edit **UConfig.cs** inside `Assets/Config`

U will automatically detect `UConfig` and apply matching fields at startup.

### HUD placement / padding

`U.Display(...)` uses `UConfig.DisplayMarginPx` (or your `UConfig.DisplayMarginPx`) as the distance from the screen edge.


## U.Placements: how positioning works

U uses `RectTransform` anchors + pivots on the `U_Root` canvas.
For example `BottomRight` sets:

- `anchorMin/anchorMax = (1, 0)`
- `pivot = (1, 0)`
- `anchoredPosition = (-margin, +margin)`

So the panel’s **bottom-right corner** hugs the screen corner, inset by `UConfig.DisplayMarginPx`.

If you ever see UI not hugging the edge, it usually means the parent is not a `RectTransform` (or a different canvas is involved). U now ensures `U_Root` always has a `RectTransform`.


## Logging helpers (Log / L)

This package includes tiny logging helpers:

Option A (recommended for teaching): add this once at the top of the file:

```csharp
using static DarkMagic.Logs;
```

Then you can write:

```csharp
Log("Hello!");   // navy
L("Same thing"); // alias
Warn("Careful!");
Error("Nope.");
LogOnce("Only once!");
Single("Also only once!");
```

Option B (no using): call them with the class name:

```csharp
DarkMagic.Logs.Log("Hello!");
DarkMagic.Logs.Warn("Careful!");
DarkMagic.Logs.Error("Nope.");
```


## X extensions (Vector / Transform)

Vector helpers return **new vectors** (because Vector3 is a struct):

```csharp
transform.position = transform.position.AddZ(10);
```

Transform helpers actually mutate the transform (recommended for beginners):

```csharp
transform.AddPosZ(10);
transform.SetPosX(3);
```


### More logging helpers

```csharp
Log("Hello!");               // navy
Warn("Careful!");            // dark orange
Error("Nope.");              // dark red
LogOnce("Only once!");       // navy + "SINGLE: " prefix
Single("Also only once!");   // alias
```


### Timed banners

```csharp
// Auto-dismiss after 2.5 seconds (returns UResult.Timeout if the timer wins)
await U.PopBanner("A party of goblins attacks!", 2.5f);
```


## U: Text overrides

All of these support optional overrides:

```csharp
await U.PopBanner("Hello", textSize: 34, textColor: Color.yellow, textAlign: TextAlignmentOptions.Center);
await U.PopDialogue( // defaults to left-aligned text
"Longer text...", textSize: 28);
var r = await U.PopChoice("Pick one:", textColor: Color.cyan, "Yes", "No");
```


### U Borders + Fades

All U.Pop* panels now have a code-generated border and optional fade.

- Config:
  - `UConfig.BorderSize`, `UConfig.BorderColor`
  - `UConfig.PopFadeIn`, `UConfig.PopFadeOut`, `UConfig.PopFadeDuration`
- Per-call overrides (optional): `borderSize`, `borderColor`


### TMP Font Default (JRPG vibe)

DarkMagic ships with a default TMP font asset (Resources path `Default/Default SDF`). U will use this automatically unless you override `UConfig.FontAsset` or you have a TMP project default font set.


## U Style Presets

U includes two built-in style presets:

- **JRPG**: FFVI-ish menu blue panels (88% opacity) + bundled JRPG TMP font.
- **Liberation**: neutral look + bundled Liberation Sans TMP font.

Set it in `UConfig.cs`:

```csharp
using DarkMagic;

public static class UConfig
{
    public static UStylePreset StylePreset = UStylePreset.JRPG;
}
```

You can still override `PanelColor`, `BorderColor`, `FontAsset`, etc. Presets only fill in defaults when you haven't explicitly set those values.


### PopChoice overload note

If you call `PopChoice` with just strings, use the student-first overload:

```csharp
var result = await U.PopChoice("Prompt", "Yes", "No");
```

Advanced overloads also let you pass optional styling params like `textSize:` and `panelColor:`.


## Assembly Definitions (asmdef)

DarkMagic is split into small assemblies (Core, V, W, U, Input, StateMachine, Animation, X) to keep Unity Editor recompile times snappy.