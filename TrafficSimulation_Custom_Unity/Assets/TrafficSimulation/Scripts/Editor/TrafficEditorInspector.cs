﻿// Traffic Simulation
// https://github.com/mchrbn/unity-traffic-simulation

using System;
using TrafficSimulation.Scripts;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulation
{
    public static class TrafficEditorInspector
    {
        //Whole Inspector layout
        public static void DrawInspector(TrafficSystem trafficSystem, SerializedObject serializedObject,
            out bool restructureSystem)
        {
            //-- Gizmo settings
            Header("Gizmo Config");
            Toggle("Hide Gizmos", ref trafficSystem.HideGizmos);

            //Arrow config
            DrawArrowTypeSelection(trafficSystem);
            FloatField("Waypoint Size", ref trafficSystem.WaypointSize);
            EditorGUILayout.Space();

            //-- System config
            Header("System Config");
            FloatField("Segment Detection Threshold", ref trafficSystem.SegmentDetectionThreshold);

            PropertyField("Collision Layers", "CollisionLayers", serializedObject);

            EditorGUILayout.Space();

            //Helper
            HelpBox(
                "Ctrl + Left Click to create a new segment\nShift + Left Click to create a new waypoint.\nAlt + Left Click to create a new intersection");
            HelpBox(
                "Reminder: The vehicles will follow the point depending on the sequence you added them. (go to the 1st waypoint added, then to the second, etc.)");
            EditorGUILayout.Space();

            restructureSystem = Button("Re-Structure Traffic System");
        }

        //-- Helper to draw the Inspector
        private static void Label(string label)
        {
            EditorGUILayout.LabelField(label);
        }

        private static void Header(string label)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }

        private static void Toggle(string label, ref bool toggle)
        {
            toggle = EditorGUILayout.Toggle(label, toggle);
        }

        private static void IntField(string label, ref int value)
        {
            value = EditorGUILayout.IntField(label, value);
        }

        private static void IntField(string label, ref int value, int min, int max)
        {
            value = Mathf.Clamp(EditorGUILayout.IntField(label, value), min, max);
        }

        private static void FloatField(string label, ref float value)
        {
            value = EditorGUILayout.FloatField(label, value);
        }

        private static void PropertyField(string label, string value, SerializedObject serializedObject)
        {
            var extra = serializedObject.FindProperty(value);
            EditorGUILayout.PropertyField(extra, new GUIContent(label), true);
        }

        private static void HelpBox(string content)
        {
            EditorGUILayout.HelpBox(content, MessageType.Info);
        }

        private static bool Button(string label)
        {
            return GUILayout.Button(label);
        }

        private static void DrawArrowTypeSelection(TrafficSystem trafficSystem)
        {
            trafficSystem.ArrowDrawType =
                (ArrowDraw)EditorGUILayout.EnumPopup("Arrow Draw Type", trafficSystem.ArrowDrawType);
            EditorGUI.indentLevel++;

            switch (trafficSystem.ArrowDrawType)
            {
                case ArrowDraw.FixedCount:
                    IntField("Count", ref trafficSystem.ArrowCount, 1, int.MaxValue);
                    break;
                case ArrowDraw.ByLength:
                    FloatField("Distance Between Arrows", ref trafficSystem.ArrowDistance);
                    break;
                case ArrowDraw.Off:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (trafficSystem.ArrowDrawType != ArrowDraw.Off)
            {
                FloatField("Arrow Size Waypoint", ref trafficSystem.ArrowSizeWaypoint);
                FloatField("Arrow Size Intersection", ref trafficSystem.ArrowSizeIntersection);
            }

            EditorGUI.indentLevel--;
        }
    }
}