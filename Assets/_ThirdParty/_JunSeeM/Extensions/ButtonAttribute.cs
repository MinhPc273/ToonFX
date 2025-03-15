using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JunEngine
{

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class ButtonAttribute : Attribute
    {
        public readonly string Name;
        public readonly string Row;
        public readonly float Space;
        public readonly bool HasRow;
        public ButtonAttribute(string name = default, string row = default, float space = default)
        {
            Row = row;
            HasRow = !string.IsNullOrEmpty(Row);
            Name = name;
            Space = space;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Object), true), CanEditMultipleObjects]
    internal class ObjectEditor : Editor
    {
        private ButtonsDrawer _buttonsDrawer;

        private void OnEnable()
        {
            _buttonsDrawer = new ButtonsDrawer(target);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            _buttonsDrawer.DrawButtons(targets);
            SceneView.RepaintAll();
        }
    }

    public class ButtonsDrawer
    {
        public readonly List<IGrouping<string, ButtonType>> ButtonGroups;

        public ButtonsDrawer(object target)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var methods = target.GetType().GetMethods(flags);
            var buttons = new List<ButtonType>();
            var rowNumber = 0;

            foreach (MethodInfo method in methods)
            {
                var buttonAttribute = method.GetCustomAttribute<ButtonAttribute>();

                if (buttonAttribute == null)
                    continue;

                buttons.Add(new ButtonType(method, buttonAttribute));
            }

            ButtonGroups = buttons.GroupBy(button =>
            {
                var attribute = button.ButtonAttribute;
                var hasRow = attribute.HasRow;
                return hasRow ? attribute.Row : $"__{rowNumber++}";
            }).ToList();
        }

        public void DrawButtons(IEnumerable<object> targets)
        {
            foreach (var buttonGroup in ButtonGroups)
            {
                if (buttonGroup.Count() > 0)
                {
                    var space = buttonGroup.First().ButtonAttribute.Space;
                    if (space != 0) EditorGUILayout.Space(space);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    foreach (var button in buttonGroup)
                    {
                        button.Draw(targets);
                    }
                }
            }
        }
    }

    public class ButtonType
    {
        public readonly string DisplayName;
        public readonly MethodInfo Method;
        public readonly ButtonAttribute ButtonAttribute;

        public ButtonType(MethodInfo method, ButtonAttribute buttonAttribute)
        {
            ButtonAttribute = buttonAttribute;
            DisplayName = string.IsNullOrEmpty(buttonAttribute.Name)
                ? ObjectNames.NicifyVariableName(method.Name)
                : buttonAttribute.Name;

            Method = method;
        }

        internal void Draw(IEnumerable<object> targets)
        {
            if (!GUILayout.Button(DisplayName)) return;

            foreach (object target in targets)
            {
                Method.Invoke(target, null);
            }
        }
    }
#endif
}
