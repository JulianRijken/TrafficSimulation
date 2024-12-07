using System;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulation.Scripts.Editor
{
    public static class EditorHelper
    {
        public static void SetUndoGroup(string label)
        {
            //Create new Undo Group to collect all changes in one Undo
            Undo.SetCurrentGroupName(label);
        }

        public static void BeginUndoGroup(string undoName, TrafficSystem trafficSystem)
        {
            //Create new Undo Group to collect all changes in one Undo
            Undo.SetCurrentGroupName(undoName);

            //Register all TrafficSystem changes after this (string not relevant here)
            Undo.RegisterFullObjectHierarchyUndo(trafficSystem.gameObject, undoName);
        }

        public static GameObject CreateGameObject(string name, Transform parent = null)
        {
            var newGameObject = new GameObject(name);

            //Register changes for Undo (string not relevant here)
            Undo.RegisterCreatedObjectUndo(newGameObject, "Spawn new GameObject");
            Undo.SetTransformParent(newGameObject.transform, parent, "Set parent");

            return newGameObject;
        }

        public static T AddComponent<T>(GameObject target) where T : Component
        {
            return Undo.AddComponent<T>(target);
        }

        //Determines if a ray hits a sphere
        public static bool SphereHit(Vector3 center, float radius, Ray r)
        {
            var oc = r.origin - center;
            var a = Vector3.Dot(r.direction, r.direction);
            var b = 2f * Vector3.Dot(oc, r.direction);
            var c = Vector3.Dot(oc, oc) - radius * radius;
            var discriminant = b * b - 4f * a * c;

            if (discriminant < 0f) return false;

            var sqrt = Mathf.Sqrt(discriminant);

            return -b - sqrt > 0f || -b + sqrt > 0f;
        }

        //From S_Darkwell: https://forum.unity.com/threads/adding-layer-by-script.41970/
        public static void CreateLayer(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name", "New layer name string is either null or empty.");

            var tagManager =
                new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layerProps = tagManager.FindProperty("layers");
            var propCount = layerProps.arraySize;

            SerializedProperty firstEmptyProp = null;

            for (var i = 0; i < propCount; i++)
            {
                var layerProp = layerProps.GetArrayElementAtIndex(i);

                var stringValue = layerProp.stringValue;

                if (stringValue == name) return;

                if (i < 8 || stringValue != string.Empty) continue;

                if (firstEmptyProp == null)
                    firstEmptyProp = layerProp;
            }

            if (firstEmptyProp == null)
            {
                Debug.LogError(
                    "Maximum limit of " + propCount + " layers exceeded. Layer \"" + name + "\" not created.");
                return;
            }

            firstEmptyProp.stringValue = name;
            tagManager.ApplyModifiedProperties();
        }

        //From SkywardRoy: https://forum.unity.com/threads/change-gameobject-layer-at-run-time-wont-apply-to-child.10091/
        public static void SetLayer(this GameObject gameObject, int layer, bool includeChildren = false)
        {
            if (!includeChildren)
            {
                gameObject.layer = layer;
                return;
            }

            foreach (var child in gameObject.GetComponentsInChildren(typeof(Transform), true))
                child.gameObject.layer = layer;
        }

        public static void Label(string label)
        {
            EditorGUILayout.LabelField(label);
        }

        public static void Header(string label)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }

        public static void Toggle(string label, ref bool toggle)
        {
            toggle = EditorGUILayout.Toggle(label, toggle);
        }

        public static void IntField(string label, ref int value)
        {
            value = EditorGUILayout.IntField(label, value);
        }

        public static void IntField(string label, ref int value, int min, int max)
        {
            value = Mathf.Clamp(EditorGUILayout.IntField(label, value), min, max);
        }

        public static void FloatField(string label, ref float value)
        {
            value = EditorGUILayout.FloatField(label, value);
        }

        public static void PropertyField(string label, string value, SerializedObject serializedObject)
        {
            var extra = serializedObject.FindProperty(value);
            EditorGUILayout.PropertyField(extra, new GUIContent(label), true);
        }

        public static void HelpBox(string content)
        {
            EditorGUILayout.HelpBox(content, MessageType.Info);
        }

        public static bool Button(string label)
        {
            return GUILayout.Button(label);
        }

        public static void DrawArrowTypeSelection(TrafficSystem trafficSystem)
        {
            trafficSystem.ArrowDrawType =
                (ArrowDrawType)EditorGUILayout.EnumPopup("Arrow Draw Type", trafficSystem.ArrowDrawType);
            EditorGUI.indentLevel++;

            switch (trafficSystem.ArrowDrawType)
            {
                case ArrowDrawType.FixedCount:
                    IntField("Count", ref trafficSystem.ArrowCount, 1, int.MaxValue);
                    break;
                case ArrowDrawType.ByLength:
                    FloatField("Distance Between Arrows", ref trafficSystem.ArrowDistance);
                    break;
                case ArrowDrawType.Off:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (trafficSystem.ArrowDrawType != ArrowDrawType.Off)
            {
                FloatField("Arrow Size Waypoint", ref trafficSystem.ArrowSizeWaypoint);
                FloatField("Arrow Size Intersection", ref trafficSystem.ArrowSizeIntersection);
            }

            EditorGUI.indentLevel--;
        }
    }
}