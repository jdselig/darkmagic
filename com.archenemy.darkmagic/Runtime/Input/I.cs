using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DarkMagic
{
    /// <summary>
    /// I: "old-school Input" ergonomics, powered by the new Input System when available,
    /// with a safe fallback to UnityEngine.Input when it's not.
    /// 
    /// Goal: near drop-in helpers like:
    ///     if (I.GetKeyDown(KeyCode.Space)) ...
    ///
    /// No prerequisite setup required.
    /// </summary>
    public static class I
    {
        // --------------------------
        // Config (optional)
        // --------------------------
        public static bool Trace = false;

        /// <summary>If true, logs a warning (Editor/Development only) when the Input System is not present and I falls back to legacy Input.</summary>
        public static bool WarnOnFallback = true;

        /// <summary>
        /// Custom button providers. If a name exists here, it overrides the built-in defaults.
        /// Signature: (held, down, up)
        /// </summary>
        public static readonly Dictionary<string, Func<(bool held, bool down, bool up)>> Buttons = new();

        /// <summary>Custom axis providers (smoothed).</summary>
        public static readonly Dictionary<string, Func<float>> Axes = new();

        /// <summary>Custom raw axis providers (snapped).</summary>
        public static readonly Dictionary<string, Func<float>> AxesRaw = new();

        // --------------------------
        // Key / Mouse / Touch (core)
        // --------------------------

        public static bool GetKey(KeyCode key)
            => InputSystemBridge.TryGetKey(key, out var v) ? v.held : UnityEngine.Input.GetKey(key);

        public static bool GetKeyDown(KeyCode key)
            => InputSystemBridge.TryGetKey(key, out var v) ? v.down : UnityEngine.Input.GetKeyDown(key);

        public static bool GetKeyUp(KeyCode key)
            => InputSystemBridge.TryGetKey(key, out var v) ? v.up : UnityEngine.Input.GetKeyUp(key);

        public static bool anyKey
            => InputSystemBridge.TryAnyKey(out bool v) ? v : UnityEngine.Input.anyKey;

        public static bool anyKeyDown
            => InputSystemBridge.TryAnyKeyDown(out bool v) ? v : UnityEngine.Input.anyKeyDown;

        public static bool GetMouseButton(int button)
            => InputSystemBridge.TryGetMouse(button, out var v) ? v.held : UnityEngine.Input.GetMouseButton(button);

        public static bool GetMouseButtonDown(int button)
            => InputSystemBridge.TryGetMouse(button, out var v) ? v.down : UnityEngine.Input.GetMouseButtonDown(button);

        public static bool GetMouseButtonUp(int button)
            => InputSystemBridge.TryGetMouse(button, out var v) ? v.up : UnityEngine.Input.GetMouseButtonUp(button);

        public static Vector3 mousePosition
            => InputSystemBridge.TryMousePosition(out var v) ? v : UnityEngine.Input.mousePosition;

        public static Vector2 mouseScrollDelta
            => InputSystemBridge.TryMouseScroll(out var v) ? v : UnityEngine.Input.mouseScrollDelta;

        public static int touchCount
            => InputSystemBridge.TryTouchCount(out int c) ? c : UnityEngine.Input.touchCount;

        public static Touch GetTouch(int index)
            => InputSystemBridge.TryGetTouch(index, out var t) ? t : UnityEngine.Input.GetTouch(index);

        // --------------------------
        // Buttons / Axes (defaults)
        // --------------------------

        public static bool GetButton(string buttonName)
        {
    #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return GetButtonNew(buttonName, mode: 0);
    #else
            try { return UnityEngine.Input.GetButton(buttonName); }
            catch (System.InvalidOperationException)
            {
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.LogWarning("[I] GetButton called but Legacy Input Manager is disabled. Use Input System mapping or enable both in Player Settings.");
    #endif
                return false;
            }
    #endif
        }

        public static bool GetButtonDown(string buttonName)
        {
    #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return GetButtonNew(buttonName, mode: 1);
    #else
            try { return UnityEngine.Input.GetButtonDown(buttonName); }
            catch (System.InvalidOperationException)
            {
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.LogWarning("[I] GetButtonDown called but Legacy Input Manager is disabled. Use Input System mapping or enable both in Player Settings.");
    #endif
                return false;
            }
    #endif
        }

        public static bool GetButtonUp(string buttonName)
        {
    #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return GetButtonNew(buttonName, mode: 2);
    #else
            try { return UnityEngine.Input.GetButtonUp(buttonName); }
            catch (System.InvalidOperationException)
            {
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.LogWarning("[I] GetButtonUp called but Legacy Input Manager is disabled. Use Input System mapping or enable both in Player Settings.");
    #endif
                return false;
            }
    #endif
        }

        public static float GetAxis(string axisName)
        {
    #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return GetAxisNew(axisName, raw: false);
    #else
            try { return UnityEngine.Input.GetAxis(axisName); }
            catch (System.InvalidOperationException)
            {
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.LogWarning("[I] GetAxis called but Legacy Input Manager is disabled. Use Input System mapping or enable both in Player Settings.");
    #endif
                return 0f;
            }
    #endif
        }

        public static float GetAxisRaw(string axisName)
        {
    #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return GetAxisNew(axisName, raw: true);
    #else
            try { return UnityEngine.Input.GetAxisRaw(axisName); }
            catch (System.InvalidOperationException)
            {
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.LogWarning("[I] GetAxisRaw called but Legacy Input Manager is disabled. Use Input System mapping or enable both in Player Settings.");
    #endif
                return 0f;
            }
    #endif
        }

        public static string[] GetJoystickNames()
            => InputSystemBridge.TryJoystickNames(out var v) ? v : UnityEngine.Input.GetJoystickNames();

        // ============================================================
        // Input System Reflection Bridge
        // ============================================================
        private static class InputSystemBridge
        {
            // Cached types
            private static readonly Type TKeyboard = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
            private static readonly Type TMouse = Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
            private static readonly Type TGamepad = Type.GetType("UnityEngine.InputSystem.Gamepad, Unity.InputSystem");
            private static readonly Type TTouchscreen = Type.GetType("UnityEngine.InputSystem.Touchscreen, Unity.InputSystem");
            private static readonly Type TInputSystem = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");

            private static readonly bool HasInputSystem = TKeyboard != null && TMouse != null;

            private static bool _warned;

            private static void MaybeWarnFallback()
            {
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (_warned) return;
                if (!I.WarnOnFallback) return;
                _warned = true;
                Debug.LogWarning("[V/I] Unity Input System (Unity.InputSystem) not found. I is falling back to legacy UnityEngine.Input.\n" +
                                "To force the new Input System backend: install the Input System package and set Project Settings → Player → Active Input Handling to include Input System.");
    #endif
            }


            // Member infos
            private static readonly PropertyInfo PKeyboardCurrent = TKeyboard?.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            private static readonly PropertyInfo PMouseCurrent = TMouse?.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            private static readonly PropertyInfo PGamepadCurrent = TGamepad?.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            private static readonly PropertyInfo PTouchCurrent = TTouchscreen?.GetProperty("current", BindingFlags.Public | BindingFlags.Static);

            // KeyCode -> Keyboard.<x>Key property names (practical subset)
            private static readonly Dictionary<KeyCode, string> KeyProps = BuildKeyProps();

            // Axis smoothing state (to feel closer to legacy)
            private static readonly Dictionary<string, float> AxisState = new();

            public static bool TryGetKey(KeyCode key, out (bool held, bool down, bool up) v)
            {
                v = default;
                if (!HasInputSystem) { MaybeWarnFallback(); return false; }

                // Support KeyCode.Mouse0..Mouse6 via mouse
                if (IsMouseKeyCode(key))
                    return TryGetMouse(KeyCodeToMouseButton(key), out v);

                object kb = PKeyboardCurrent?.GetValue(null);
                if (kb == null) return false;

                if (!KeyProps.TryGetValue(key, out string propName))
                    return false;

                object keyControl = kb.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(kb);
                if (keyControl == null) return false;

                return TryReadButtonControl(keyControl, out v);
            }

            public static bool TryAnyKey(out bool held)
            {
                held = false;
                if (!HasInputSystem) { MaybeWarnFallback(); return false; }

                object kb = PKeyboardCurrent?.GetValue(null);
                if (kb == null) return false;

                object anyKey = kb.GetType().GetProperty("anyKey", BindingFlags.Public | BindingFlags.Instance)?.GetValue(kb);
                if (anyKey == null) return false;

                if (!TryReadButtonControl(anyKey, out var v)) return false;
                held = v.held;
                return true;
            }

            public static bool TryAnyKeyDown(out bool down)
            {
                down = false;
                if (!HasInputSystem) { MaybeWarnFallback(); return false; }

                object kb = PKeyboardCurrent?.GetValue(null);
                if (kb == null) return false;

                // best-effort: scan known keys we mapped
                foreach (var kv in KeyProps)
                {
                    if (TryGetKey(kv.Key, out var v) && v.down)
                    {
                        down = true;
                        return true;
                    }
                }

                return true; // input system present, but none down
            }

            public static bool TryGetMouse(int button, out (bool held, bool down, bool up) v)
            {
                v = default;
                if (!HasInputSystem) { MaybeWarnFallback(); return false; }

                object mouse = PMouseCurrent?.GetValue(null);
                if (mouse == null) return false;

                string prop =
                    button == 0 ? "leftButton" :
                    button == 1 ? "rightButton" :
                    button == 2 ? "middleButton" :
                    button == 3 ? "forwardButton" :
                    button == 4 ? "backButton" :
                    null;

                if (prop == null) return false;

                object bc = mouse.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance)?.GetValue(mouse);
                if (bc == null) return false;

                return TryReadButtonControl(bc, out v);
            }

            public static bool TryMousePosition(out Vector3 pos)
            {
                pos = default;
                if (!HasInputSystem) { MaybeWarnFallback(); return false; }

                object mouse = PMouseCurrent?.GetValue(null);
                if (mouse == null) return false;

                object positionControl = mouse.GetType().GetProperty("position", BindingFlags.Public | BindingFlags.Instance)?.GetValue(mouse);
                if (positionControl == null) return false;

                // positionControl.ReadValue() returns Vector2
                var readValue = positionControl.GetType().GetMethod("ReadValue", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (readValue == null) return false;

                object v2 = readValue.Invoke(positionControl, null);
                if (v2 is Vector2 vv)
                {
                    pos = new Vector3(vv.x, vv.y, 0f);
                    return true;
                }

                return false;
            }

            public static bool TryMouseScroll(out Vector2 scroll)
            {
                scroll = default;
                if (!HasInputSystem) { MaybeWarnFallback(); return false; }

                object mouse = PMouseCurrent?.GetValue(null);
                if (mouse == null) return false;

                object scrollControl = mouse.GetType().GetProperty("scroll", BindingFlags.Public | BindingFlags.Instance)?.GetValue(mouse);
                if (scrollControl == null) return false;

                var readValue = scrollControl.GetType().GetMethod("ReadValue", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (readValue == null) return false;

                object v2 = readValue.Invoke(scrollControl, null);
                if (v2 is Vector2 vv)
                {
                    scroll = vv;
                    return true;
                }

                return false;
            }

        public static bool TryMouseDelta(out Vector2 delta)
        {
            delta = default;
            if (TMouse == null) { MaybeWarnFallback(); return false; }

            try
            {
                var mouse = PMouseCurrent?.GetValue(null);
                if (mouse == null) return false;

                var deltaProp = TMouse.GetProperty("delta", BindingFlags.Public | BindingFlags.Instance);
                var deltaCtrl = deltaProp?.GetValue(mouse);
                if (deltaCtrl == null) return false;

                var read = deltaCtrl.GetType().GetMethod("ReadValue", BindingFlags.Public | BindingFlags.Instance);
                if (read == null) return false;

                var obj = read.Invoke(deltaCtrl, null);
                if (obj is Vector2 v2) { delta = v2; return true; }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryGamepadLeftStick(out Vector2 stick)
        {
            stick = default;
            if (TGamepad == null) { MaybeWarnFallback(); return false; }

            try
            {
                var gp = PGamepadCurrent?.GetValue(null);
                if (gp == null) return false;

                var prop = TGamepad.GetProperty("leftStick", BindingFlags.Public | BindingFlags.Instance);
                var ctrl = prop?.GetValue(gp);
                if (ctrl == null) return false;

                var read = ctrl.GetType().GetMethod("ReadValue", BindingFlags.Public | BindingFlags.Instance);
                if (read == null) return false;

                var obj = read.Invoke(ctrl, null);
                if (obj is Vector2 v2) { stick = v2; return true; }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryGamepadDpad(out Vector2 dpad)
        {
            dpad = default;
            if (TGamepad == null) { MaybeWarnFallback(); return false; }

            try
            {
                var gp = PGamepadCurrent?.GetValue(null);
                if (gp == null) return false;

                var prop = TGamepad.GetProperty("dpad", BindingFlags.Public | BindingFlags.Instance);
                var ctrl = prop?.GetValue(gp);
                if (ctrl == null) return false;

                var read = ctrl.GetType().GetMethod("ReadValue", BindingFlags.Public | BindingFlags.Instance);
                if (read == null) return false;

                var obj = read.Invoke(ctrl, null);
                if (obj is Vector2 v2) { dpad = v2; return true; }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryGetGamepadButton(string buttonPropertyName, out (bool held, bool down, bool up) v)
        {
            v = default;
            if (TGamepad == null) { MaybeWarnFallback(); return false; }

            try
            {
                var gp = PGamepadCurrent?.GetValue(null);
                if (gp == null) return false;

                var prop = TGamepad.GetProperty(buttonPropertyName, BindingFlags.Public | BindingFlags.Instance);
                var btn = prop?.GetValue(gp);
                if (btn == null) return false;

                var tBtn = btn.GetType();
                var heldProp = tBtn.GetProperty("isPressed", BindingFlags.Public | BindingFlags.Instance);
                var downProp = tBtn.GetProperty("wasPressedThisFrame", BindingFlags.Public | BindingFlags.Instance);
                var upProp = tBtn.GetProperty("wasReleasedThisFrame", BindingFlags.Public | BindingFlags.Instance);

                v.held = heldProp != null && (bool)heldProp.GetValue(btn);
                v.down = downProp != null && (bool)downProp.GetValue(btn);
                v.up = upProp != null && (bool)upProp.GetValue(btn);
                return true;
            }
            catch
            {
                return false;
            }
        }

            public static bool TryTouchCount(out int count)
            {
                count = 0;
                if (TTouchscreen == null) return false;

                object ts = PTouchCurrent?.GetValue(null);
                if (ts == null) return true; // input system available but no touchscreen

                object touches = ts.GetType().GetProperty("touches", BindingFlags.Public | BindingFlags.Instance)?.GetValue(ts);
                if (touches == null) return false;

                // touches is ReadOnlyArray<TouchControl>
                var countProp = touches.GetType().GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
                int n = countProp != null ? (int)countProp.GetValue(touches) : 0;

                int pressed = 0;
                for (int i = 0; i < n; i++)
                {
                    object tc = touches.GetType().GetMethod("get_Item")?.Invoke(touches, new object[] { i });
                    if (tc == null) continue;
                    object press = tc.GetType().GetProperty("press")?.GetValue(tc);
                    if (press == null) continue;
                    if (TryReadButtonControl(press, out var v) && v.held) pressed++;
                }

                count = pressed;
                return true;
            }

            public static bool TryGetTouch(int index, out Touch touch)
            {
                touch = default;
                if (TTouchscreen == null) return false;

                object ts = PTouchCurrent?.GetValue(null);
                if (ts == null) return false;

                object touches = ts.GetType().GetProperty("touches", BindingFlags.Public | BindingFlags.Instance)?.GetValue(ts);
                if (touches == null) return false;

                int n = (int)(touches.GetType().GetProperty("Count")?.GetValue(touches) ?? 0);
                if (index < 0 || index >= n) return false;

                object tc = touches.GetType().GetMethod("get_Item")?.Invoke(touches, new object[] { index });
                if (tc == null) return false;

                Vector2 position = ReadVector2Control(tc, "position");
                Vector2 delta = ReadVector2Control(tc, "delta");

                object press = tc.GetType().GetProperty("press")?.GetValue(tc);

                bool pressed = false, down = false, up = false;
                if (press != null && TryReadButtonControl(press, out var v))
                {
                    pressed = v.held; down = v.down; up = v.up;
                }

                touch.fingerId = index;
                touch.position = position;
                touch.rawPosition = position;
                touch.deltaPosition = delta;
                touch.deltaTime = Time.deltaTime;

                if (down) touch.phase = TouchPhase.Began;
                else if (up) touch.phase = TouchPhase.Ended;
                else if (pressed && delta.sqrMagnitude > 0.0001f) touch.phase = TouchPhase.Moved;
                else if (pressed) touch.phase = TouchPhase.Stationary;
                else touch.phase = TouchPhase.Canceled;

                return true;
            }

            public static bool TryButton(string name, out (bool held, bool down, bool up) v)
            {
                v = default;
                if (!HasInputSystem) { MaybeWarnFallback(); return false; }

                object kb = PKeyboardCurrent?.GetValue(null);
                object gp = PGamepadCurrent?.GetValue(null);

                // Defaults: Jump, Fire1, Fire2, Submit, Cancel
                switch (name)
                {
                    case "Jump":
                        return Combine(
                            GetProp(kb, "spaceKey"),
                            GetProp(gp, "buttonSouth"),
                            out v);

                    case "Fire1":
                        return Combine(
                            GetProp(kb, "ctrlKey"),
                            GetProp(gp, "rightTrigger"),
                            out v);

                    case "Fire2":
                        return Combine(
                            GetProp(kb, "altKey"),
                            GetProp(gp, "leftTrigger"),
                            out v);

                    case "Submit":
                        return Combine(
                            GetProp(kb, "enterKey"),
                            GetProp(gp, "buttonSouth"),
                            out v);

                    case "Cancel":
                        return Combine(
                            GetProp(kb, "escapeKey"),
                            GetProp(gp, "buttonEast"),
                            out v);

                    default:
                        return false;
                }
            }

            public static bool TryAxis(string name, bool raw, out float value)
            {
                value = 0f;
                if (!HasInputSystem) { MaybeWarnFallback(); return false; }

                object kb = PKeyboardCurrent?.GetValue(null);
                object gp = PGamepadCurrent?.GetValue(null);

                float v = 0f;

                if (name == "Horizontal")
                {
                    float left = KeyHeld(kb, "aKey") || KeyHeld(kb, "leftArrowKey") ? 1f : 0f;
                    float right = KeyHeld(kb, "dKey") || KeyHeld(kb, "rightArrowKey") ? 1f : 0f;
                    float stick = ReadAxis(gp, "leftStick", "x");
                    v = (right - left) + stick;
                }
                else if (name == "Vertical")
                {
                    float down = KeyHeld(kb, "sKey") || KeyHeld(kb, "downArrowKey") ? 1f : 0f;
                    float up = KeyHeld(kb, "wKey") || KeyHeld(kb, "upArrowKey") ? 1f : 0f;
                    float stick = ReadAxis(gp, "leftStick", "y");
                    v = (up - down) + stick;
                }
                else return false;

                v = Mathf.Clamp(v, -1f, 1f);

                if (raw)
                {
                    value = v > 0.2f ? 1f : v < -0.2f ? -1f : 0f;
                    return true;
                }

                value = SmoothAxis(name, v);
                return true;
            }

            public static bool TryJoystickNames(out string[] names)
            {
                names = null;
                if (TInputSystem == null) return false;

                // InputSystem.devices is IEnumerable<InputDevice>. We’ll just return gamepads & joysticks we can see by type name.
                var devicesProp = TInputSystem.GetProperty("devices", BindingFlags.Public | BindingFlags.Static);
                object devices = devicesProp?.GetValue(null);
                if (devices == null) return false;

                var list = new List<string>();
                foreach (var d in (System.Collections.IEnumerable)devices)
                {
                    if (d == null) continue;
                    var t = d.GetType();
                    string tn = t.FullName ?? t.Name;

                    if (tn.Contains("Gamepad") || tn.Contains("Joystick"))
                    {
                        string display = (string)(t.GetProperty("displayName")?.GetValue(d) ?? t.Name);
                        list.Add(display);
                    }
                }

                names = list.ToArray();
                return true;
            }

            // ---- helpers ----

            private static bool TryReadButtonControl(object control, out (bool held, bool down, bool up) v)
            {
                v = default;
                if (control == null) return false;

                var t = control.GetType();
                var pHeld = t.GetProperty("isPressed") ?? t.GetProperty("isPressed", BindingFlags.Public | BindingFlags.Instance);
                var pDown = t.GetProperty("wasPressedThisFrame");
                var pUp = t.GetProperty("wasReleasedThisFrame");

                if (pHeld == null || pDown == null || pUp == null) return false;

                v = ((bool)pHeld.GetValue(control), (bool)pDown.GetValue(control), (bool)pUp.GetValue(control));
                return true;
            }

            private static object GetProp(object obj, string propName)
                => obj?.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(obj);

            private static bool KeyHeld(object kb, string propName)
            {
                object kc = GetProp(kb, propName);
                return kc != null && TryReadButtonControl(kc, out var v) && v.held;
            }

            private static float ReadAxis(object gp, string stickProp, string axisProp)
            {
                if (gp == null) return 0f;

                object stick = GetProp(gp, stickProp);
                if (stick == null) return 0f;

                // stick.ReadValue() returns Vector2
                var readValue = stick.GetType().GetMethod("ReadValue", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (readValue == null) return 0f;

                object v2 = readValue.Invoke(stick, null);
                if (v2 is Vector2 vv)
                {
                    return axisProp == "x" ? vv.x : vv.y;
                }

                return 0f;
            }

            private static Vector2 ReadVector2Control(object parent, string prop)
            {
                object control = parent?.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance)?.GetValue(parent);
                if (control == null) return default;

                var readValue = control.GetType().GetMethod("ReadValue", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (readValue == null) return default;

                object v2 = readValue.Invoke(control, null);
                return v2 is Vector2 vv ? vv : default;
            }

            private static bool Combine(object a, object b, out (bool held, bool down, bool up) v)
            {
                v = default;

                bool aOk = TryReadButtonControl(a, out var av);
                bool bOk = TryReadButtonControl(b, out var bv);

                if (!aOk && !bOk) return false;

                v = (
                    (aOk && av.held) || (bOk && bv.held),
                    (aOk && av.down) || (bOk && bv.down),
                    (aOk && av.up) || (bOk && bv.up)
                );

                return true;
            }

            private static bool IsMouseKeyCode(KeyCode kc)
                => kc == KeyCode.Mouse0 || kc == KeyCode.Mouse1 || kc == KeyCode.Mouse2
                || kc == KeyCode.Mouse3 || kc == KeyCode.Mouse4 || kc == KeyCode.Mouse5 || kc == KeyCode.Mouse6;

            private static int KeyCodeToMouseButton(KeyCode kc)
                => kc switch
                {
                    KeyCode.Mouse0 => 0,
                    KeyCode.Mouse1 => 1,
                    KeyCode.Mouse2 => 2,
                    KeyCode.Mouse3 => 3,
                    KeyCode.Mouse4 => 4,
                    _ => 0
                };

            private static float SmoothAxis(string axisName, float target)
            {
                const float speed = 12f;
                AxisState.TryGetValue(axisName, out float current);
                float next = Mathf.MoveTowards(current, target, speed * Time.deltaTime);
                AxisState[axisName] = next;
                return next;
            }

            private static Dictionary<KeyCode, string> BuildKeyProps()
            {
                // Practical, student-heavy subset.
                var m = new Dictionary<KeyCode, string>();

                // Letters
                for (char c = 'A'; c <= 'Z'; c++)
                {
                    var kc = (KeyCode)Enum.Parse(typeof(KeyCode), c.ToString());
                    m[kc] = char.ToLowerInvariant(c) + "Key"; // aKey, bKey...
                }

                // Digits Alpha0..Alpha9 => digit0Key..digit9Key (InputSystem naming is "digit1Key" on Keyboard)
                for (int i = 0; i <= 9; i++)
                    m[(KeyCode)Enum.Parse(typeof(KeyCode), "Alpha" + i)] = "digit" + i + "Key";

                // Arrows
                m[KeyCode.LeftArrow] = "leftArrowKey";
                m[KeyCode.RightArrow] = "rightArrowKey";
                m[KeyCode.UpArrow] = "upArrowKey";
                m[KeyCode.DownArrow] = "downArrowKey";

                // Common controls
                m[KeyCode.Space] = "spaceKey";
                m[KeyCode.Escape] = "escapeKey";
                m[KeyCode.Return] = "enterKey";
                m[KeyCode.Tab] = "tabKey";
                m[KeyCode.Backspace] = "backspaceKey";

                // Modifiers
                m[KeyCode.LeftShift] = "leftShiftKey";
                m[KeyCode.RightShift] = "rightShiftKey";
                m[KeyCode.LeftControl] = "leftCtrlKey";
                m[KeyCode.RightControl] = "rightCtrlKey";
                m[KeyCode.LeftAlt] = "leftAltKey";
                m[KeyCode.RightAlt] = "rightAltKey";

                return m;
            }
        }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        private static float GetAxisNew(string axisName, bool raw)
        {
            axisName = (axisName ?? "").Trim();
            var key = axisName.ToLowerInvariant();

            float v = 0f;

            if (key == "horizontal" || key == "horizontalaxis" || key == "x" || key == "move x")
            {
                v += KeyAxis(KeyCode.A, KeyCode.D, KeyCode.LeftArrow, KeyCode.RightArrow);
                if (InputSystemBridge.TryGamepadLeftStick(out var stick)) v += stick.x;
                if (InputSystemBridge.TryGamepadDpad(out var dpad)) v += dpad.x;
            }
            else if (key == "vertical" || key == "verticalaxis" || key == "y" || key == "move y")
            {
                v += KeyAxis(KeyCode.S, KeyCode.W, KeyCode.DownArrow, KeyCode.UpArrow);
                if (InputSystemBridge.TryGamepadLeftStick(out var stick)) v += stick.y;
                if (InputSystemBridge.TryGamepadDpad(out var dpad)) v += dpad.y;
            }
            else if (key == "mouse x" || key == "mousex" || key == "mousedelta x")
            {
                if (InputSystemBridge.TryMouseDelta(out var d)) v = d.x;
            }
            else if (key == "mouse y" || key == "mousey" || key == "mousedelta y")
            {
                if (InputSystemBridge.TryMouseDelta(out var d)) v = d.y;
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (IConfig.WARN_ON_FALLBACK)
                    UnityEngine.Debug.LogWarning($"[I] GetAxis('{axisName}') has no Input System mapping. Defaults: Horizontal, Vertical, Mouse X, Mouse Y.");
#endif
                v = 0f;
            }

            // Normalize typical axes
            if (key == "horizontal" || key == "vertical" || key == "horizontalaxis" || key == "verticalaxis" || key == "move x" || key == "move y" || key == "x" || key == "y")
            {
                v = UnityEngine.Mathf.Clamp(v, -1f, 1f);
                if (raw) v = v == 0f ? 0f : (v > 0f ? 1f : -1f);
            }
            else
            {
                if (raw) v = v == 0f ? 0f : (v > 0f ? 1f : -1f);
            }

            return v;
        }

        private static float KeyAxis(KeyCode negative1, KeyCode positive1, KeyCode negative2, KeyCode positive2)
        {
            float v = 0f;
            if (InputSystemBridge.TryGetKey(negative1, out var n1) && n1.held) v -= 1f;
            if (InputSystemBridge.TryGetKey(negative2, out var n2) && n2.held) v -= 1f;
            if (InputSystemBridge.TryGetKey(positive1, out var p1) && p1.held) v += 1f;
            if (InputSystemBridge.TryGetKey(positive2, out var p2) && p2.held) v += 1f;
            return v;
        }

        private static bool GetButtonNew(string buttonName, int mode)
        {
            // mode: 0=held, 1=down, 2=up
            buttonName = (buttonName ?? "").Trim();
            var key = buttonName.ToLowerInvariant();

            bool MatchKey(KeyCode kc)
            {
                if (!InputSystemBridge.TryGetKey(kc, out var v)) return false;
                return mode == 0 ? v.held : mode == 1 ? v.down : v.up;
            }

            bool MatchMouse(int btn)
            {
                if (!InputSystemBridge.TryGetMouse(btn, out var v)) return false;
                return mode == 0 ? v.held : mode == 1 ? v.down : v.up;
            }

            bool MatchGamepad(string propName)
            {
                if (!InputSystemBridge.TryGetGamepadButton(propName, out var v)) return false;
                return mode == 0 ? v.held : mode == 1 ? v.down : v.up;
            }

            if (key == "jump")
                return MatchKey(KeyCode.Space) || MatchGamepad("buttonSouth");

            if (key == "fire1")
                return MatchMouse(0) || MatchGamepad("buttonWest");

            if (key == "fire2")
                return MatchMouse(1) || MatchGamepad("buttonEast");

            if (key == "submit")
                return MatchKey(KeyCode.Return) || MatchKey(KeyCode.KeypadEnter) || MatchGamepad("buttonSouth");

            if (key == "cancel")
                return MatchKey(KeyCode.Escape) || MatchMouse(1) || MatchGamepad("buttonEast");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (IConfig.WARN_ON_FALLBACK)
                UnityEngine.Debug.LogWarning($"[I] GetButton('{buttonName}') has no Input System mapping. Defaults: Jump, Fire1, Fire2, Submit, Cancel.");
#endif
            return false;
        }
#endif

    }
}