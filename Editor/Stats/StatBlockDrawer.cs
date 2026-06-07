using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DarkMagic.Editor
{
    [CustomPropertyDrawer(typeof(DarkMagic.StatBlock))]
    public class StatBlockDrawer : PropertyDrawer
    {
        private ReorderableList _list;

        private const float Line = 18f;
        private const float VPad = 4f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            EnsureList(property);
            float h = 0f;

            h += Line + VPad;               // header
            h += _list.GetHeight() + VPad;  // list (includes our custom footer)
            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            EnsureList(property);

            float y = position.y;
            float x = position.x;
            float w = position.width;

            // Header (no foldout)
            var headerRect = new Rect(x, y, w, Line);
            EditorGUI.LabelField(headerRect, new GUIContent(label.text, "A collection of Stats (Name/Abbreviation lookup)."));
            y += Line + VPad;

            // List
            var listRect = new Rect(x, y, w, _list.GetHeight());
            _list.DoList(listRect);

            EditorGUI.EndProperty();
        }

        private void EnsureList(SerializedProperty property)
        {
            var statsProp = property.FindPropertyRelative("_stats");
            if (_list != null && _list.serializedProperty == statsProp)
                return;

            _list = new ReorderableList(property.serializedObject, statsProp, draggable: false, displayHeader: false, displayAddButton: false, displayRemoveButton: false);

            _list.elementHeightCallback = (index) =>
            {
                var el = statsProp.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(el, includeChildren: true) + 2f;
            };

            _list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var el = statsProp.GetArrayElementAtIndex(index);
                rect.y += 1f;
                rect.height = EditorGUI.GetPropertyHeight(el, includeChildren: true);
                EditorGUI.PropertyField(rect, el, GUIContent.none, includeChildren: true);
            };

            _list.footerHeight = Line + 6f;

            _list.drawFooterCallback = (rect) =>
            {
                DrawFooter(rect, property, statsProp);
            };
        }

        private static void DrawFooter(Rect rect, SerializedProperty statBlockProp, SerializedProperty statsProp)
        {
            // Left side: + and -
            var plusRect = new Rect(rect.x, rect.y + 2f, 26f, Line);
            var minusRect = new Rect(rect.x + 30f, rect.y + 2f, 26f, Line);

            if (GUI.Button(plusRect, "+"))
            {
                statsProp.arraySize += 1;
                statBlockProp.serializedObject.ApplyModifiedProperties();
            }

            using (new EditorGUI.DisabledScope(statsProp.arraySize == 0))
            {
                if (GUI.Button(minusRect, "-"))
                {
                    statsProp.arraySize -= 1;
                    statBlockProp.serializedObject.ApplyModifiedProperties();
                }
            }

            // Right side: action buttons
            float rightX = rect.x + 64f;
            float available = rect.width - 64f;

            float btnW = (available - 12f) / 3f;
            var b1 = new Rect(rightX, rect.y + 2f, btnW, Line);
            var b2 = new Rect(rightX + btnW + 6f, rect.y + 2f, btnW, Line);
            var b3 = new Rect(rightX + (btnW + 6f) * 2f, rect.y + 2f, btnW, Line);

            if (GUI.Button(b1, "LevelUpAll"))
                InvokeOnTargets(statBlockProp, sb => sb.LevelUp(), "LevelUpAll");

            if (GUI.Button(b2, "RefreshAll"))
                InvokeOnTargets(statBlockProp, sb => sb.Refresh(force: false), "RefreshAll");

            if (GUI.Button(b3, "RefreshForce"))
                InvokeOnTargets(statBlockProp, sb => sb.Refresh(force: true), "RefreshAllForce");
        }

        private static void InvokeOnTargets(SerializedProperty prop, Action<DarkMagic.StatBlock> action, string undoName)
        {
            var so = prop.serializedObject;
            foreach (var target in so.targetObjects)
            {
                Undo.RecordObject(target, undoName);

                var statBlock = GetTargetObjectOfProperty(prop, target) as DarkMagic.StatBlock;
                if (statBlock != null)
                    action(statBlock);

                EditorUtility.SetDirty(target);
            }

            so.Update();
            so.ApplyModifiedProperties();
        }

        // Resolve nested objects by property path.
        private static object GetTargetObjectOfProperty(SerializedProperty prop, UnityEngine.Object root)
        {
            if (prop == null || root == null) return null;

            object obj = root;
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            var elements = path.Split('.');

            foreach (var element in elements)
            {
                if (obj == null) return null;

                if (element.Contains("["))
                {
                    var name = element.Substring(0, element.IndexOf("[", StringComparison.Ordinal));
                    var idxStr = element.Substring(element.IndexOf("[", StringComparison.Ordinal) + 1);
                    idxStr = idxStr.Substring(0, idxStr.Length - 1);

                    if (!int.TryParse(idxStr, out var idx)) return null;

                    var fi = obj.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (fi == null) return null;

                    var list = fi.GetValue(obj) as System.Collections.IList;
                    if (list == null || idx < 0 || idx >= list.Count) return null;
                    obj = list[idx];
                }
                else
                {
                    var fi = obj.GetType().GetField(element, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (fi == null) return null;
                    obj = fi.GetValue(obj);
                }
            }

            return obj;
        }
    }
}
