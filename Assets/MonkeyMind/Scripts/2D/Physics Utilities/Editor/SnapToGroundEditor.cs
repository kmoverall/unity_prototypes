using UnityEngine;
using System.Collections;
using UnityEditor;

namespace MonkeyMind.TwoD
{
    [CustomEditor(typeof(SnapToGround))]
    public class SnapToGroundEditor : Editor
    {
        public static bool autoSnapEditor = true;
        public static bool autoSnapGame = false;
        public static bool useRenderer = true;
        public static bool useCollider = false;
        public static bool useTransform = false;
        public static bool showOptions = false;

        public static LayerMask snapToLayers;

        void OnEnable()
        {
            useRenderer = serializedObject.FindProperty("useRenderer").boolValue;
            useCollider = serializedObject.FindProperty("useCollider").boolValue;
            useTransform = serializedObject.FindProperty("useTransform").boolValue;
            autoSnapGame = serializedObject.FindProperty("autoSnap").boolValue;
            autoSnapEditor = serializedObject.FindProperty("autoSnapEditor").boolValue;
            snapToLayers = serializedObject.FindProperty("snapLayer").intValue;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            //Auto Snap Options
            autoSnapEditor = EditorGUILayout.ToggleLeft(" Auto-Snap In Editor", autoSnapEditor);
            autoSnapGame = EditorGUILayout.ToggleLeft(" Auto-Snap In Game", autoSnapGame);
            serializedObject.FindProperty("autoSnap").boolValue = autoSnapGame;
            serializedObject.FindProperty("autoSnapEditor").boolValue = autoSnapEditor;

            //Snap Button
            GUI.enabled = !autoSnapEditor;
            if (GUILayout.Button("Snap To Ground", GUILayout.ExpandWidth(false)))
            {
                SnapToGround s = (SnapToGround)target;
                s.Snap();
            }
            GUI.enabled = true;

            //Snap Location Options
            showOptions = EditorGUILayout.Foldout(showOptions, "Options");
            if (showOptions)
            {
                useRenderer = EditorGUILayout.Toggle("Snap To Renderer: ", useRenderer);

                useCollider = EditorGUILayout.Toggle("Snap To Collider: ", useCollider);

                useTransform = EditorGUILayout.Toggle("Snap To Transform: ", useTransform);

                if (!useRenderer && !useCollider && !useTransform)
                {
                    useRenderer = serializedObject.FindProperty("useRenderer").boolValue;
                    useCollider = serializedObject.FindProperty("useCollider").boolValue;
                    useTransform = serializedObject.FindProperty("useTransform").boolValue;
                }

                if (!serializedObject.FindProperty("useRenderer").boolValue && useRenderer)
                {
                    useCollider = false;
                    useTransform = false;
                }
                if (!serializedObject.FindProperty("useCollider").boolValue && useCollider)
                {
                    useRenderer = false;
                    useTransform = false;
                }
                if (!serializedObject.FindProperty("useTransform").boolValue && useTransform)
                {
                    useRenderer = false;
                    useCollider = false;
                }

                snapToLayers = EditorTools.LayerMaskField("Mask", snapToLayers);
            }

            serializedObject.FindProperty("useRenderer").boolValue = useRenderer;
            serializedObject.FindProperty("useCollider").boolValue = useCollider;
            serializedObject.FindProperty("useTransform").boolValue = useTransform;
            serializedObject.FindProperty("snapLayer").intValue = snapToLayers;

            serializedObject.ApplyModifiedProperties();
        }

        void OnSceneGUI()
        {
            if (autoSnapEditor && !Application.isPlaying)
            {
                SnapToGround s = (SnapToGround)target;
                s.Snap();
            }
        }
    }
}
