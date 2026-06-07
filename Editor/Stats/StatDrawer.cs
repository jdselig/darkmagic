using System;
using UnityEditor;
using UnityEngine;

namespace DarkMagic.Editor
{
    [CustomPropertyDrawer(typeof(DarkMagic.Stat))]
    public class StatDrawer : PropertyDrawer
    {
        private const float Line = 18f;
        private const float VPad = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Header always
            float h = Line;

            if (!property.isExpanded)
                return h;

            bool isResource = property.FindPropertyRelative("IsResource").boolValue;

            // Current line (top when expanded)
            int lines = 1;

            // Abbreviation, Name
            lines += 2;

            // Remaining only if resource
            if (isResource) lines += 1;

            // Base, Initial, Delta, Threshold, Temp, Equip
            lines += 6;

            // Flags: IsResource, Persists, IsLethal, IsDisabled
            lines += 4;

            // padding
            return h + VPad + (lines * (Line + VPad)) + VPad;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var abbrProp = property.FindPropertyRelative("Abbreviation");
            var nameProp = property.FindPropertyRelative("Name");

            var abbr = (abbrProp.stringValue ?? "").Trim();
            var name = (nameProp.stringValue ?? "Stat").Trim();
            int current = ComputeCurrent(property);

            // Header as a button-like row (no foldout triangle)
            var headerRect = new Rect(position.x, position.y, position.width, Line);

            var headerStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                fontStyle = FontStyle.Normal,
                padding = new RectOffset(8, 8, 2, 2),
            };

            string headerText = $"<b>[{abbr}] {name}: {current}</b>";
            if (GUI.Button(headerRect, headerText, headerStyle))
                property.isExpanded = !property.isExpanded;

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            float oldLabelWidth = EditorGUIUtility.labelWidth;
            float oldFieldWidth = EditorGUIUtility.fieldWidth;
            EditorGUIUtility.labelWidth = 140f;
            EditorGUIUtility.fieldWidth = 60f;

            float y = position.y + Line + VPad;
            float x = position.x;
            float w = position.width;

            var pRemaining = property.FindPropertyRelative("Remaining");
            var pBase = property.FindPropertyRelative("Base");
            var pInitial = property.FindPropertyRelative("Initial");
            var pDelta = property.FindPropertyRelative("Delta");
            var pThreshold = property.FindPropertyRelative("Threshold");
            var pTemp = property.FindPropertyRelative("TempModifiers");
            var pEquip = property.FindPropertyRelative("EquipmentModifiers");
            var pIsRes = property.FindPropertyRelative("IsResource");
            var pPersists = property.FindPropertyRelative("Persists");
            var pLethal = property.FindPropertyRelative("IsLethal");
            var pDisabled = property.FindPropertyRelative("IsDisabled");

            // Current (top, bold)
            DrawCurrentLine(ref y, x, w, current);

            // Abbreviation + Name (read-only once set)
            bool lockId = !string.IsNullOrWhiteSpace(abbrProp.stringValue) || !string.IsNullOrWhiteSpace(nameProp.stringValue);
            DrawReadOnlyOrEditable(ref y, x, w, abbrProp, "Abbreviation", "Short key for lookups (e.g., STR, HP).", lockId);
            DrawReadOnlyOrEditable(ref y, x, w, nameProp, "Name", "Human-friendly name (e.g., Strength, Health).", lockId);

            bool isResource = pIsRes.boolValue;
            if (isResource)
                DrawLine(ref y, x, w, pRemaining, "Remaining", "Current resource amount (HP/MP). Used for Current when below max.");

            DrawLine(ref y, x, w, pBase, "Base", "Persistent base value. For resources, acts like Max.");
            DrawLine(ref y, x, w, pInitial, "Initial", "Starting value when the stat was created.");
            DrawLine(ref y, x, w, pDelta, "Delta", "Default amount applied by LevelUp().");
            DrawLine(ref y, x, w, pThreshold, "Threshold", "0 disables threshold logic. If Current >= Threshold, OnThresholdMet fires.");

            DrawLine(ref y, x, w, pTemp, "Temp Mods", "Buffs/debuffs for the encounter/session. Cleared on Refresh().");
            DrawLine(ref y, x, w, pEquip, "Equip Mods", "Equipment modifiers from gear.");

            DrawLine(ref y, x, w, pIsRes, "Is Resource", "If true: += heals and -= damages (changes Remaining). Otherwise +=/-= affect Temp Mods.");
            DrawLine(ref y, x, w, pPersists, "Persists", "If false, Remaining resets to Base on Refresh().");
            DrawLine(ref y, x, w, pLethal, "Is Lethal", "If true and Remaining hits 0, IsDisabled becomes true.");
            DrawLine(ref y, x, w, pDisabled, "Is Disabled", "Set when a lethal stat hits 0 (e.g., KO).");

            EditorGUIUtility.labelWidth = oldLabelWidth;
            EditorGUIUtility.fieldWidth = oldFieldWidth;

            EditorGUI.EndProperty();
        }

        private static void DrawLine(ref float y, float x, float w, SerializedProperty prop, string label, string tip)
        {
            var r = new Rect(x, y, w, Line);
            EditorGUI.PropertyField(r, prop, new GUIContent(label, tip), true);
            y += Line + VPad;
        }

        private static void DrawReadOnlyOrEditable(ref float y, float x, float w, SerializedProperty prop, string label, string tip, bool readOnly)
        {
            var r = new Rect(x, y, w, Line);
            EditorGUI.BeginDisabledGroup(readOnly);
            EditorGUI.PropertyField(r, prop, new GUIContent(label, tip), true);
            EditorGUI.EndDisabledGroup();
            y += Line + VPad;
        }

        private static void DrawCurrentLine(ref float y, float x, float w, int current)
        {
            var r = new Rect(x, y, w, Line);
            var style = new GUIStyle(EditorStyles.label) { richText = true };
            GUI.Label(r, $"<b>Current</b>: <b>{current}</b>", style);
            y += Line + VPad;
        }

        private static int ComputeCurrent(SerializedProperty stat)
        {
            int baseV = stat.FindPropertyRelative("Base").intValue;
            int temp = stat.FindPropertyRelative("TempModifiers").intValue;
            int equip = stat.FindPropertyRelative("EquipmentModifiers").intValue;
            int remaining = stat.FindPropertyRelative("Remaining").intValue;
            bool isResource = stat.FindPropertyRelative("IsResource").boolValue;

            int max = baseV + temp + equip;
            if (isResource)
                return remaining < max ? remaining : max;

            return max;
        }
    }
}