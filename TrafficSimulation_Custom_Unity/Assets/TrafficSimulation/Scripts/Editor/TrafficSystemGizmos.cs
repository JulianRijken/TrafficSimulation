using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulation
{
    public static class TrafficSystemGizmos
    {
        //Custom Gizmo function
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active)]
        private static void DrawGizmo(TrafficSystem script, GizmoType gizmoType)
        {
            if (script.HideGizmos)
                return;

            foreach (var segment in script.Segments)
            {
                // Draw text
                if (script.FontSize > 0)
                {
                    //Draw segment names
                    //Custom font
                    var font = (Font)Resources.Load("JetBrainsMono-Bold");
                    var styleFirst = new GUIStyle
                        { normal = { textColor = new Color(0, 0, 0, 1.0f) }, fontSize = script.FontSize, font = font };
                    var styleSecond = new GUIStyle
                        { normal = { textColor = new Color(1, 1, 1, 1.0f) }, fontSize = script.FontSize, font = font };

                    Vector3 firstWaypoint = segment.Waypoints.First().transform.position;
                    Vector3 lastWaypoint = segment.Waypoints.Last().transform.position;
                    Handles.Label(firstWaypoint + Vector3.up, segment.name, styleFirst);
                    Handles.Label(lastWaypoint + Vector3.up, segment.name, styleSecond);
                }
                
                // Draw segments
                foreach (var waypoint in segment.Waypoints)
                {
                    Vector3 from = waypoint.Position;
                    Gizmos.color = new Color(1f, 1f, 1f, 1.0f);
                    Gizmos.DrawSphere(from, script.WaypointSize);
                    
                    if(waypoint.NextWaypoint == null)
                        continue;
                    
                    Vector3 to = waypoint.NextWaypoint.Position;
                    
                    Gizmos.color = new Color(40.0f/ 255.0f, 40.0f / 255.0f, 201.0f / 255.0f,1.0f);
                    Handles.color = Gizmos.color;
                    Handles.DrawLine(from, to, 5.0f);
                        
                    
                    var center = Vector3.Lerp(from, to, 0.5f);
                    DrawArrow(center, to - from, script.ArrowSizeWaypoint);
                    // var arrows = GetArrowCount(from, to, script);
                    // for (var i = 1; i < arrows + 1; i++)
                    // {
                    //     var point = Vector3.Lerp(from, to, (float)i / (arrows + 1));
                    //     DrawArrow(point, to - from, script.ArrowSizeWaypoint);
                    // }
                }

                //Draw line linking segments
                foreach (var nextSegment in segment.ConnectedSegments)
                {
                    if (nextSegment != null)
                    {
                        var p1 = segment.Waypoints.Last().transform.position;
                        var p2 = nextSegment.Waypoints.First().transform.position;

                        Gizmos.color = new Color(0.9f, 0.8f, 0.2f);
                        Gizmos.DrawLine(p1, p2);
                        
                        
                        if (script.ArrowDrawType != ArrowDrawType.Off)
                            DrawArrow((p1 + p2) / 2f, p1 - p2, script.ArrowSizeIntersection);
                    }
                }
            }
        }

        private static void DrawArrow(Vector3 point, Vector3 forward, float size)
        {
            forward = forward.normalized * size;
            var left = Quaternion.Euler(0, 45, 0) * forward;
            var right = Quaternion.Euler(0, -45, 0) * forward;

            // Gizmos.DrawLine(point, point + left);
            // Gizmos.DrawLine(point, point + right);
            
            Handles.color = Gizmos.color;
            Handles.DrawLine(point, point + left, 5.0f);
            Handles.DrawLine(point, point + right, 5.0f);
        }

        private static int GetArrowCount(Vector3 pointA, Vector3 pointB, TrafficSystem script)
        {
            switch (script.ArrowDrawType)
            {
                case ArrowDrawType.FixedCount:
                    return script.ArrowCount;
                case ArrowDrawType.ByLength:
                    //Minimum of one arrow
                    return Mathf.Max(1, (int)(Vector3.Distance(pointA, pointB) / script.ArrowDistance));
                case ArrowDrawType.Off:
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}