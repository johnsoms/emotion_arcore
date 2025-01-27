﻿#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace DigitalRuby.WeatherMaker
{
    [CustomEditor(typeof(WeatherMakerWeatherManagerScript), true)]
    public class WeatherMakerWeatherManagerEditor : UnityEditor.Editor
    {
        // http://catlikecoding.com/unity/tutorials/editor/custom-list/
        private const float transitionGroupMinHeight = 70.0f;
        private const float transitionGroupHeightMinimized = 22.0f;
        private const float transitionHeight = 104.0f;
        private const float individualHeight = 20.0f;
        private const float individualHeightWithSpacing = individualHeight + 4.0f;
        private const float labelWidth = 70.0f;

        private GUIStyle comboBoxStyle;
        private ReorderableList transitionList;
        private SerializedProperty transitionListProperty;
        private WeatherMakerWeatherManagerScript script;
        private SerializedObject scriptObject;

        private static readonly Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

        private float HeightForTransitionGroup(int index)
        {
            string foldoutKey = "WM_TF_" + index;
            bool foldout;
            if (foldouts.TryGetValue(foldoutKey, out foldout) && !foldout)
            {
                return transitionGroupHeightMinimized;
            }
            WeatherMakerTransitionGroupWeight w = script.Transitions[index];
            return (w.TransitionGroup.Transitions.Count == 0 ? transitionGroupMinHeight + 20.0f : transitionGroupMinHeight + (w.TransitionGroup.Transitions.Count * transitionHeight));
        }

        private List<MemberInfo> GetPossibleFields(MonoBehaviour script)
        {
            List<MemberInfo> validFields = new List<MemberInfo>();
            MemberInfo[] members = script.GetType().GetMembers(BindingFlags.GetField | BindingFlags.SetField |
                BindingFlags.GetProperty | BindingFlags.SetProperty |
                BindingFlags.Instance | BindingFlags.Public);
            foreach (MemberInfo member in members)
            {
                if (member.DeclaringType != typeof(MonoBehaviour) && (member is FieldInfo || (member is PropertyInfo && (member as PropertyInfo).CanWrite)))
                {
                    foreach (Type t in WeatherMakerWeatherManagerScript.ValidFieldTypes)
                    {
                        if (t.IsAssignableFrom(member.GetUnderlyingType()))
                        {
                            validFields.Add(member);
                            break;
                        }
                    }
                }
            }
            validFields.Sort((f1, f2) => f1.Name.CompareTo(f2.Name));
            return validFields;
        }

        private object GetMemberValue(MemberInfo member, WeatherMakerPropertyTransition t)
        {
            if (t.Value != null)
            {
                if (member.GetUnderlyingType() == t.Value.GetType())
                {
                    return t.Value;
                }
            }
            return member.GetUnderlyingValue(t.Target);
        }

        private void RenderFloatField(Rect rect, WeatherMakerPropertyTransition t, GUIContent label, MemberInfo member, RangeAttribute range, SingleLineClampAttribute clamp)
        {
            float val = (float)GetMemberValue(member, t);
            if (clamp != null)
            {
                val = Mathf.Clamp(val, clamp.MinValue, clamp.MaxValue);
            }
            if (range == null)
            {
                t.Value = EditorGUI.FloatField(rect, label, val);
            }
            else
            {
                t.Value = EditorGUI.Slider(rect, label, val, range.min, range.max);
            }
        }

        private void RenderIntField(Rect rect, WeatherMakerPropertyTransition t, GUIContent label, MemberInfo member, RangeAttribute range, SingleLineClampAttribute clamp)
        {
            int val = (int)GetMemberValue(member, t);
            if (clamp != null)
            {
                val = Mathf.Clamp(val, (int)clamp.MinValue, (int)clamp.MaxValue);
            }
            if (range == null)
            {
                t.Value = EditorGUI.IntField(rect, label, val);
            }
            else
            {
                t.Value = EditorGUI.IntSlider(rect, label, val, (int)range.min, (int)range.max);
            }
        }

        private void RenderRangeOfFloats(Rect rect, WeatherMakerPropertyTransition t, MemberInfo member, SingleLineClampAttribute clamp)
        {
            GUIContent labelMin = new GUIContent("Min", "Minimum Value (Inclusive)");
            GUIContent labelMax = new GUIContent("Max", "Maximum Value (Inclusive)");
            RangeOfFloats r = (RangeOfFloats)GetMemberValue(member, t);
            float w = rect.width;
            rect.width *= 0.5f;
            rect.width -= 6.0f;
            r.Minimum = EditorGUI.FloatField(rect, labelMin, r.Minimum);
            rect.x += rect.width + 6.0f;
            r.Maximum = EditorGUI.FloatField(rect, labelMax, r.Maximum);
            t.Value = r;
        }

        private void RenderRangeOfInts(Rect rect, WeatherMakerPropertyTransition t, MemberInfo member, SingleLineClampAttribute clamp)
        {
            GUIContent labelMin = new GUIContent("Min", "Minimum Value (Inclusive)");
            GUIContent labelMax = new GUIContent("Max", "Maximum Value (Inclusive)");
            RangeOfIntegers r = (RangeOfIntegers)GetMemberValue(member, t);
            float w = rect.width;
            rect.width *= 0.5f;
            rect.width -= 6.0f;
            r.Minimum = EditorGUI.IntField(rect, labelMin, r.Minimum);
            rect.x += rect.width + 6.0f;
            r.Maximum = EditorGUI.IntField(rect, labelMax, r.Maximum);
            t.Value = r;
        }

        private void RenderField(Rect rect, WeatherMakerPropertyTransition t, MemberInfo member, float textFieldWidth)
        {
            if (t == null || member == null)
            {
                return;
            }
            string tooltip = null;
            RangeAttribute range = null;
            SingleLineClampAttribute clamp = null;
            object[] attributes = member.GetCustomAttributes(false);
            foreach (object obj in attributes)
            {
                if (obj is TooltipAttribute)
                {
                    tooltip = (obj as TooltipAttribute).tooltip;
                }
                else if (obj is SingleLineAttribute)
                {
                    tooltip = (obj as SingleLineAttribute).Tooltip;
                }
                else if (obj is RangeAttribute)
                {
                    range = obj as RangeAttribute;
                }
                else if (obj is SingleLineClampAttribute)
                {
                    clamp = obj as SingleLineClampAttribute;
                }
            }
            GUIContent label = new GUIContent("Value", tooltip);
            EditorGUIUtility.fieldWidth = 70.0f;
            Type type = member.GetUnderlyingType();
            if (type == typeof(float))
            {
                RenderFloatField(rect, t, label, member, range, clamp);
            }
            else if (type == typeof(int))
            {
                RenderIntField(rect, t, label, member, range, clamp);
            }
            else if (type == typeof(bool))
            {
                t.Value = EditorGUI.Toggle(rect, label, (bool)GetMemberValue(member, t));
            }
            else if (type == typeof(Color))
            {
                t.Value = EditorGUI.ColorField(rect, label, (Color)GetMemberValue(member, t));
            }
            else if (type == typeof(Vector2))
            {
                t.Value = EditorGUI.Vector2Field(rect, label, (Vector2)GetMemberValue(member, t));
            }
            else if (type == typeof(Vector3))
            {
                t.Value = EditorGUI.Vector3Field(rect, label, (Vector3)GetMemberValue(member, t));
            }
            else if (type == typeof(Vector4))
            {
                t.Value = EditorGUI.Vector4Field(rect, label, (Vector4)GetMemberValue(member, t));
            }
            else if (type == typeof(RangeOfFloats))
            {
                RenderRangeOfFloats(rect, t, member, clamp);
            }
            else if (type == typeof(RangeOfIntegers))
            {
                RenderRangeOfInts(rect, t, member, clamp);
            }
            else if (type.IsEnum)
            {
                t.Value = EditorGUI.EnumPopup(rect, label, (System.Enum)GetMemberValue(member, t));
            }
            else
            {
                EditorGUI.LabelField(rect, "Unsupported field type " + type);
            }
        }

        private string GetFieldDisplayName(string fieldInfoName)
        {
            switch (fieldInfoName)
            {
                case "Int32": return "Int";
                case "Single": return "Float";
                case "Boolean": return "Bool";
                default: return fieldInfoName;
            }
        }

        private void RenderTransition(WeatherMakerTransitionGroupWeight w, Rect rect, int index)
        {
            if (index >= w.TransitionGroup.Transitions.Count)
            {
                return;
            }

            float textFieldWidth = Mathf.Min(rect.width, 150.0f);
            float longWidth = Mathf.Min(rect.width, 300.0f);
            float fullWidth = rect.width - 10.0f;
            WeatherMakerPropertyTransition t = w.TransitionGroup.Transitions[index];
            EditorGUIUtility.labelWidth = labelWidth;
            float width = rect.width;
            float x = rect.x;
            rect.height = individualHeight;
            rect.width = fullWidth;
            MonoBehaviour newTarget = (MonoBehaviour)EditorGUI.ObjectField(rect, "Target", t.Target, typeof(MonoBehaviour), true);
            if (newTarget != t.Target)
            {
                t.Target = newTarget;
                t.FieldNamePopup = null;
            }
            rect.y += individualHeightWithSpacing;
            if (t.Target != null)
            {
                List<MemberInfo> members = GetPossibleFields(t.Target);
                if (members.Count == 0)
                {
                    EditorGUI.LabelField(rect, "* No valid fields found *");
                }
                else
                {
                    if (t.FieldNamePopup == null)
                    {
                        t.FieldNamePopup = new PopupList();
                        t.FieldNamePopup.SelectedIndexChanged = (pu, ea) =>
                        {
                            if (t.FieldNamePopup.SelectedItemIndex < members.Count)
                            {
                                t.FieldName = members[t.FieldNamePopup.SelectedItemIndex].Name;
                            }
                            t.Curve = null;
                        };
                    }
                    rect.width = longWidth;
                    int i = 0;
                    foreach (MemberInfo member2 in members)
                    {
                        if (member2.Name == t.FieldName)
                        {
                            t.FieldNamePopup.SelectedItemIndex = i;
                            break;
                        }
                        i++;
                    }
                    EditorGUI.LabelField(rect, "Field:");
                    rect.x += EditorGUIUtility.labelWidth;
                    rect.width = fullWidth - EditorGUIUtility.labelWidth;
                    GUIStyle buttonStyle = "button";
                    buttonStyle.alignment = TextAnchor.MiddleLeft;
                    if (GUI.Button(rect, members[t.FieldNamePopup.SelectedItemIndex].Name + " (" + GetFieldDisplayName(members[t.FieldNamePopup.SelectedItemIndex].GetUnderlyingType().Name) + ")", buttonStyle))
                    {
                        List<GUIContent> options = new List<GUIContent>();
                        foreach (MemberInfo member2 in members)
                        {
                            options.Add(new GUIContent(member2.Name + " (" + GetFieldDisplayName(member2.GetUnderlyingType().Name) + ")"));
                        }
                        t.FieldNamePopup.ListStyle = comboBoxStyle;
                        t.FieldNamePopup.Items = options.ToArray();
                        PopupWindow.Show(rect, t.FieldNamePopup);
                    }
                    string newName = members[t.FieldNamePopup.SelectedItemIndex].Name;
                    if (newName != t.FieldName)
                    {
                        t.FieldName = newName;
                        GUI.changed = true;
                    }
                    rect.x = x;
                    rect.y += individualHeightWithSpacing + 2.0f;
                    MemberInfo member = t.Target.GetType().GetMember(t.FieldName, BindingFlags.GetField | BindingFlags.SetField |
                        BindingFlags.GetProperty | BindingFlags.SetProperty |
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)[0];
                    rect.width = fullWidth;
                    RenderField(rect, t, member, textFieldWidth);
                    rect.y += individualHeightWithSpacing;
                    AnimationCurve curve = t.Curve;
                    if (curve == null || curve.length == 0)
                    {
                        Type ft = members[t.FieldNamePopup.SelectedItemIndex].GetUnderlyingType();

                        // default to immediately setting the value
                        if (ft == typeof(bool) || ft == typeof(System.Enum) || ft == typeof(RangeOfFloats) || ft == typeof(RangeOfIntegers))
                        {
                            curve = AnimationCurve.Linear(0.0f, 1.0f, 0.0f, 1.0f);
                        }
                        else
                        {
                            // default to linear curve
                            curve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
                        }
                    }
                    t.Curve = EditorGUI.CurveField(rect, "Curve", curve);
                    rect.y += individualHeightWithSpacing;
                    rect.height = 0.0f;
                    rect.width = fullWidth;
                    EditorDrawLine.DrawLine(rect, Color.cyan, 1.0f);
                }
            }
        }

        private void RenderTransitionGroup(Rect rect, int index)
        {
            if (rect.width <= 0)
            {
                return;
            }

            WeatherMakerTransitionGroupWeight w = script.Transitions[index];
            string foldoutKey = "WM_TF_" + index;
            bool foldout;
            foldouts.TryGetValue(foldoutKey, out foldout);
            rect.y += 2.0f;
            rect.x += 10.0f;
            rect.height = individualHeight - 4.0f;
            float width = rect.width;
            if (foldout)
            {
                rect.width = 24.0f;
                foldout = EditorGUI.Foldout(rect, foldout, string.Empty);
            }
            else
            {
                foldout = EditorGUI.Foldout(rect, foldout, new GUIContent(w.TransitionGroup.Title + ", Weight: " + w.Weight));
            }
            rect.width = width;
            foldouts[foldoutKey] = foldout;
            if (foldout)
            {
                rect.width -= 8.0f;
                Rect origRect = rect;
                float x = rect.x;
                EditorGUIUtility.labelWidth = 50.0f;
                EditorGUIUtility.fieldWidth = 30.0f;
                rect.width = 100.0f;
                w.Weight = EditorGUI.IntField(rect, new GUIContent("Weight", "Weight for this transition group. Higher weights make the transition group more likely to occur."), w.Weight);
                rect.x = rect.xMax + 10.0f;
                rect.width = Mathf.Max(1.0f, width - 120.0f);
                w.TransitionGroup.Title = EditorGUI.TextField(rect, new GUIContent("Title", "Title for this transition, a short description."), w.TransitionGroup.Title);
                origRect.y += individualHeight + 2.0f;
                origRect.height -= 24.0f;
                string propPath = "Transitions.Array.data[" + index + "].TransitionGroup.Transitions";
                SerializedProperty prop = scriptObject.FindProperty(propPath);
                if (w.TransitionList == null)
                {
                    w.TransitionList = new ReorderableList(prop.serializedObject, prop, true, true, true, true);
                    w.TransitionList.drawElementCallback = (rect2, index2, active, focused) =>
                    {
                        RenderTransition(w, rect2, index2);
                    };
                    w.TransitionList.drawHeaderCallback = rect2 =>
                    {
                        EditorGUI.LabelField(rect2, "Transitions");
                    };
                    w.TransitionList.elementHeightCallback = (int index2) =>
                    {
                        return transitionHeight;
                    };
                }
                w.TransitionList.DoList(origRect);
            }
        }

        private void OnEnable()
        {
            script = (WeatherMakerWeatherManagerScript)target;
            scriptObject = new SerializedObject(script);
            transitionListProperty = scriptObject.FindProperty("Transitions");
            transitionList = new ReorderableList(transitionListProperty.serializedObject, transitionListProperty, true, true, true, true);
            transitionList.drawElementCallback = (rect, index, active, focused) =>
            {
                RenderTransitionGroup(rect, index);
            };
            transitionList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Transitions & Weights");
            };
            transitionList.elementHeightCallback = (int index) =>
            {
                return HeightForTransitionGroup(index);
            };
        }

        public override void OnInspectorGUI()
        {
            if (script == null)
            {
                return;
            }

            comboBoxStyle = new GUIStyle("label");
            comboBoxStyle.alignment = TextAnchor.MiddleLeft;
            comboBoxStyle.normal.textColor = GUI.skin.label.normal.textColor;
            comboBoxStyle.normal.background = GUI.skin.label.normal.background;
            comboBoxStyle.padding.left =
            comboBoxStyle.padding.right =
            comboBoxStyle.padding.top =
            comboBoxStyle.padding.bottom = 4;

            DrawDefaultInspector();

            scriptObject.Update();
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginVertical();
            EditorGUILayout.Space();
            transitionList.DoLayoutList();
            GUILayout.EndVertical();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(script, "Edit Weather Maker Manager");
                EditorUtility.SetDirty(script);
                if (!Application.isPlaying)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                }
            }
            scriptObject.ApplyModifiedProperties();
        }
    }
}

#endif
