// Copyright (c) 2026 NicoIer and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

using UnityEditor;
using UnityEngine;

namespace JoltPhysics.Unity.Editor
{
    /// <summary>
    /// Shared drawing code used by both the Project Settings panel and the ScriptableObject inspector.
    /// </summary>
    static class JoltPhysicsSettingsDrawer
    {
        static bool _collisionMatrixFoldout = true;
        static Vector2 _scrollPos;

        public static void DrawSettings(JoltPhysicsSettings settings)
        {
            var so = new SerializedObject(settings);
            so.Update();

            var dataProp = so.FindProperty("_data");

            EditorGUILayout.LabelField("Global Physics", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(dataProp.FindPropertyRelative("gravity"));
            EditorGUILayout.PropertyField(dataProp.FindPropertyRelative("collisionSteps"));
            EditorGUILayout.PropertyField(dataProp.FindPropertyRelative("maxBodies"));
            EditorGUILayout.PropertyField(dataProp.FindPropertyRelative("maxContactConstraints"));

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Object Layers", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(dataProp.FindPropertyRelative("layers"), true);

            so.ApplyModifiedProperties();

            // Collision Matrix
            EditorGUILayout.Space(10);
            _collisionMatrixFoldout = EditorGUILayout.Foldout(_collisionMatrixFoldout,
                "Layer Collision Matrix", true, EditorStyles.foldoutHeader);

            if (_collisionMatrixFoldout)
            {
                settings.EnsureCollisionMaskSize();
                DrawCollisionMatrix(settings);
            }

            if (GUI.changed)
                EditorUtility.SetDirty(settings);
        }

        const float CheckboxSize = 16f;
        const float RowHeight = 18f;

        static void DrawCollisionMatrix(JoltPhysicsSettings settings)
        {
            int n = settings.LayerCount;
            if (n == 0)
            {
                EditorGUILayout.HelpBox("No layers defined.", MessageType.Info);
                return;
            }

            var layers = settings.Layers;

            // Compute max label width from actual layer names
            float maxLabelW = 40f;
            for (int i = 0; i < n; i++)
            {
                float w = EditorStyles.label.CalcSize(new GUIContent(layers[i].name)).x;
                if (w > maxLabelW) maxLabelW = w;
            }
            maxLabelW += 8f;

            // Column headers are rotated -90°, so their text width becomes vertical height
            float headerHeight = maxLabelW + 4f;
            float totalWidth = maxLabelW + n * CheckboxSize + 20f;
            float totalHeight = headerHeight + n * RowHeight + 10f;

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos,
                GUILayout.MinHeight(Mathf.Min(totalHeight, 400f)));

            var areaRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);

            // Grid origin: top-left of checkbox area
            float gridX = areaRect.x + maxLabelW;
            float gridY = areaRect.y + headerHeight;

            // --- Column headers rotated -90° (vertical text, like Unity Physics) ---
            var savedMatrix = GUI.matrix;
            var headerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
            };

            for (int col = 0; col < n; col++)
            {
                float cx = gridX + col * CheckboxSize + CheckboxSize * 0.5f;
                float cy = gridY;

                var pivot = new Vector2(cx, cy);
                GUIUtility.RotateAroundPivot(-90f, pivot);

                // After -90° rotation around pivot, the label extends rightward (= upward visually)
                var labelRect = new Rect(cx, cy - CheckboxSize * 0.5f, maxLabelW, CheckboxSize);
                GUI.Label(labelRect, layers[col].name, headerStyle);

                GUI.matrix = savedMatrix;
            }

            // --- Rows (lower-left triangle) ---
            var rowLabelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight,
            };

            for (int row = 0; row < n; row++)
            {
                float rowY = gridY + row * RowHeight;

                // Row label on the left, right-aligned
                var rowLabelRect = new Rect(areaRect.x, rowY, maxLabelW - 4f, RowHeight);
                GUI.Label(rowLabelRect, layers[row].name, rowLabelStyle);

                // Checkboxes: columns 0..row (lower-left triangle)
                for (int col = 0; col <= row; col++)
                {
                    var toggleRect = new Rect(
                        gridX + col * CheckboxSize,
                        rowY + (RowHeight - CheckboxSize) * 0.5f,
                        CheckboxSize,
                        CheckboxSize);

                    bool current = settings.GetCollisionEnabled(row, col);
                    bool newVal = GUI.Toggle(toggleRect, current, GUIContent.none);
                    if (newVal != current)
                    {
                        Undo.RecordObject(settings, "Change Jolt Collision Matrix");
                        settings.SetCollisionEnabled(row, col, newVal);
                        EditorUtility.SetDirty(settings);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }

    /// <summary>
    /// Project Settings > Jolt Physics panel.
    /// </summary>
    static class JoltPhysicsSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/Jolt Physics", SettingsScope.Project)
            {
                label = "Jolt Physics",
                guiHandler = OnGUI,
                keywords = new[] { "Jolt", "Physics", "Layer", "Collision" }
            };
        }

        static void OnGUI(string searchContext)
        {
            var settings = JoltPhysicsSettings.Instance;
            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "JoltPhysicsSettings asset not found. Click below to create one.",
                    MessageType.Warning);
                if (GUILayout.Button("Create Settings Asset"))
                    _ = JoltPhysicsSettings.Instance;
                return;
            }

            JoltPhysicsSettingsDrawer.DrawSettings(settings);
        }
    }

    /// <summary>
    /// Custom inspector for the JoltPhysicsSettings ScriptableObject asset.
    /// Shows the same UI as Project Settings > Jolt Physics.
    /// </summary>
    [CustomEditor(typeof(JoltPhysicsSettings))]
    sealed class JoltPhysicsSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            JoltPhysicsSettingsDrawer.DrawSettings((JoltPhysicsSettings)target);
        }
    }
}
