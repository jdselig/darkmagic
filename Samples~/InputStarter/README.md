# Input Starter Sample

This sample is OPTIONAL. It does not change how `I` works.

It includes:
- `V_InputStarter.inputactions` (small default action map)
- `InputStarter.cs` (tiny wrapper that reads actions in a student-friendly way)
- `IInputActionsBridge.cs` (optional bridge to feed actions into `I.GetButton` / `I.GetAxis`)

## Import
Unity → Window → Package Manager → select **V** → **Samples** → Import **Input Starter**.

## Use (fastest)
1. Create an empty GameObject called **Input**.
2. Add **InputStarter** to it.
3. In any script:
   ```csharp
   var input = FindFirstObjectByType<InputStarter>();
   Vector2 move = input.Move;
   if (input.JumpDown) Debug.Log("Jump!");
   ```

## Bridge into I (optional)
1. Add **IInputActionsBridge** to any GameObject.
2. Assign the **InputStarter** reference (or leave it empty to auto-find).
3. Now legacy calls can be powered by actions:
   ```csharp
   float x = I.GetAxis("Horizontal");
   if (I.GetButtonDown("Jump")) { }
   ```

## Editing actions
- Open `V_InputStarter.inputactions` and change bindings/actions as needed.
- Keep the action names the same (`Move`, `Look`, `Jump`, `Fire`, `Sprint`, `Pause`) unless you also update `InputStarter.cs`.
