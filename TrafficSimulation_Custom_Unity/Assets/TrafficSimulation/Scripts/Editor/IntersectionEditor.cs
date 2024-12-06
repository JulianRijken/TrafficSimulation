// Traffic Simulation
// https://github.com/mchrbn/unity-traffic-simulation

using TrafficSimulation.Scripts;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulation
{
    [CustomEditor(typeof(Intersection))]
    public class IntersectionEditor : Editor
    {
        private Intersection intersection;

        private void OnEnable()
        {
            intersection = target as Intersection;
        }

        public override void OnInspectorGUI()
        {
            intersection._intersectionType =
                (IntersectionType)EditorGUILayout.EnumPopup("Intersection type", intersection._intersectionType);

            EditorGUI.BeginDisabledGroup(intersection._intersectionType != IntersectionType.Stop);

            EditorGUILayout.LabelField("Stop", EditorStyles.boldLabel);
            var sPrioritySegments = serializedObject.FindProperty("prioritySegments");
            EditorGUILayout.PropertyField(sPrioritySegments, new GUIContent("Priority Segments"), true);
            serializedObject.ApplyModifiedProperties();

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(intersection._intersectionType != IntersectionType.TrafficLight);

            EditorGUILayout.LabelField("Traffic Lights", EditorStyles.boldLabel);
            intersection._lightsDuration =
                EditorGUILayout.FloatField("Light Duration (in s.)", intersection._lightsDuration);
            intersection._orangeLightDuration =
                EditorGUILayout.FloatField("Orange Light Duration (in s.)", intersection._orangeLightDuration);
            var sLightsNbr1 = serializedObject.FindProperty("lightsNbr1");
            var sLightsNbr2 = serializedObject.FindProperty("lightsNbr2");
            EditorGUILayout.PropertyField(sLightsNbr1, new GUIContent("Lights #1 (first to be red)"), true);
            EditorGUILayout.PropertyField(sLightsNbr2, new GUIContent("Lights #2"), true);
            serializedObject.ApplyModifiedProperties();

            EditorGUI.EndDisabledGroup();
        }
    }
}