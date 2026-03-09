using UnityEngine;

namespace DarkMagic
{
    /// <summary>
    /// Useful extension methods (student-friendly).
    /// </summary>
    public static class X
    {
        // ---- Vector2 / Vector3: functional setters/adders (return NEW vectors) ----
        public static Vector2 SetX(this Vector2 v, float x) => new Vector2(x, v.y);
        public static Vector2 SetY(this Vector2 v, float y) => new Vector2(v.x, y);
        public static Vector2 AddX(this Vector2 v, float dx) => new Vector2(v.x + dx, v.y);
        public static Vector2 AddY(this Vector2 v, float dy) => new Vector2(v.x, v.y + dy);

        public static Vector3 SetX(this Vector3 v, float x) => new Vector3(x, v.y, v.z);
        public static Vector3 SetY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
        public static Vector3 SetZ(this Vector3 v, float z) => new Vector3(v.x, v.y, z);

        public static Vector3 AddX(this Vector3 v, float dx) => new Vector3(v.x + dx, v.y, v.z);
        public static Vector3 AddY(this Vector3 v, float dy) => new Vector3(v.x, v.y + dy, v.z);
        public static Vector3 AddZ(this Vector3 v, float dz) => new Vector3(v.x, v.y, v.z + dz);

        // ---- Transform: the student-proof versions (actually mutate the transform) ----
        public static void SetPosX(this Transform t, float x) { var p = t.position; p.x = x; t.position = p; }
        public static void SetPosY(this Transform t, float y) { var p = t.position; p.y = y; t.position = p; }
        public static void SetPosZ(this Transform t, float z) { var p = t.position; p.z = z; t.position = p; }

        public static void AddPosX(this Transform t, float dx) { var p = t.position; p.x += dx; t.position = p; }
        public static void AddPosY(this Transform t, float dy) { var p = t.position; p.y += dy; t.position = p; }
        public static void AddPosZ(this Transform t, float dz) { var p = t.position; p.z += dz; t.position = p; }

        public static void SetLocalX(this Transform t, float x) { var p = t.localPosition; p.x = x; t.localPosition = p; }
        public static void SetLocalY(this Transform t, float y) { var p = t.localPosition; p.y = y; t.localPosition = p; }
        public static void SetLocalZ(this Transform t, float z) { var p = t.localPosition; p.z = z; t.localPosition = p; }

        public static void AddLocalX(this Transform t, float dx) { var p = t.localPosition; p.x += dx; t.localPosition = p; }
        public static void AddLocalY(this Transform t, float dy) { var p = t.localPosition; p.y += dy; t.localPosition = p; }
        public static void AddLocalZ(this Transform t, float dz) { var p = t.localPosition; p.z += dz; t.localPosition = p; }

        public static void SetScaleX(this Transform t, float x) { var s = t.localScale; s.x = x; t.localScale = s; }
        public static void SetScaleY(this Transform t, float y) { var s = t.localScale; s.y = y; t.localScale = s; }
        public static void SetScaleZ(this Transform t, float z) { var s = t.localScale; s.z = z; t.localScale = s; }

        public static void AddScaleX(this Transform t, float dx) { var s = t.localScale; s.x += dx; t.localScale = s; }
        public static void AddScaleY(this Transform t, float dy) { var s = t.localScale; s.y += dy; t.localScale = s; }
        public static void AddScaleZ(this Transform t, float dz) { var s = t.localScale; s.z += dz; t.localScale = s; }
    }
}
