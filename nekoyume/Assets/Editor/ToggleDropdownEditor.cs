﻿using Nekoyume.UI.Module;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(ToggleDropdown))]
    public class ToggleDropdownEditor : UnityEditor.UI.ToggleEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onObject"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("offObject"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("allOffOnAwake"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onClickToggle"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("allowSwitchOffWhenIsOn"));
            DrawList(serializedObject.FindProperty("items"), "item");

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }

            base.OnInspectorGUI();
        }

        private void DrawList(SerializedProperty listProperty, string labalName)
        {
            listProperty.isExpanded =
                EditorGUILayout.Foldout(listProperty.isExpanded, listProperty.name);
            if (listProperty.isExpanded)
            {
                EditorGUILayout.PropertyField(listProperty.FindPropertyRelative("Array.size"));
                var count = listProperty.arraySize;
                for (var i = 0; i < count; ++i)
                {
                    EditorGUILayout.PropertyField(listProperty.GetArrayElementAtIndex(i),
                        new GUIContent(labalName + i));
                }
            }
        }
    }
}
