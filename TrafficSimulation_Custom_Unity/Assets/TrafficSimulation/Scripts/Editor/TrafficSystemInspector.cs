using UnityEditor;

namespace TrafficSimulation.Scripts.Editor
{
    public static class TrafficSystemInspector
    {
        //Whole Inspector layout
        public static void DrawInspector(TrafficSystem trafficSystem, SerializedObject serializedObject,
            out bool restructureSystem, out bool moveWaypointsToFloor)
        {
                //-- Gizmo settings
                EditorHelper.Header("Gizmo Config");
                EditorHelper.Toggle("Hide Gizmos", ref trafficSystem.HideGizmos);

                //Arrow config
                EditorHelper.DrawArrowTypeSelection(trafficSystem);
                EditorHelper.FloatField("Waypoint Size", ref trafficSystem.WaypointSize);
                EditorHelper.IntField("Font Size", ref trafficSystem.FontSize);
                EditorGUILayout.Space();

                //-- System config
                EditorHelper.Header("System Config");
                EditorHelper.FloatField("Segment Detection Threshold", ref trafficSystem.SegmentDetectionThreshold);
                EditorHelper.FloatField("Waypoint Height", ref trafficSystem.WaypointHeight);
                EditorHelper.PropertyField("Waypoint Ground Snap Ignore Layer Mask", "WaypointGroundSnapIgnoreLayerMask", serializedObject);
                
                //Helper
                EditorHelper.HelpBox(
                    "Ctrl + Left Click to create a new segment\nShift + Left Click to create a new waypoint.\nAlt + Left Click to create a new intersection");
                EditorHelper.HelpBox(
                    "Reminder: The vehicles will follow the point depending on the sequence you added them. (go to the 1st waypoint added, then to the second, etc.)");
                EditorGUILayout.Space();

                restructureSystem = EditorHelper.Button("Re-Structure Traffic System");
                
                moveWaypointsToFloor = EditorHelper.Button("Move Waypoints to Floor");
        }
    }
}