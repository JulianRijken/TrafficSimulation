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
            //Don't go further if we hide gizmos
            if (script.HideGizmos)
                return;

            foreach (var segment in script.Segments)
            {
                //Draw segment names
                var style = new GUIStyle { normal = { textColor = new Color(0, 0, 0) }, fontSize = 15 };
                Handles.Label(segment.transform.position, segment.name, style);

                //Draw waypoint
                for (var j = 0; j < segment.Waypoints.Count; j++)
                {
                    //Get current waypoint position
                    var p = segment.Waypoints[j].transform.position;

                    //Draw sphere, increase color to show the direction
                    Gizmos.color = new Color(1f, 1f, 1f, (j + 1) / (float)segment.Waypoints.Count);
                    Gizmos.DrawSphere(p, script.WaypointSize);

                    //Get next waypoint position
                    var pNext = Vector3.zero;

                    if (j < segment.Waypoints.Count - 1 && segment.Waypoints[j + 1] != null)
                        pNext = segment.Waypoints[j + 1].transform.position;

                    if (pNext != Vector3.zero)
                    {
                        Gizmos.color = segment == script.CurSegment ? new Color(1f, .3f, .1f) : new Color(1f, 0f, 0f);

                        //Draw connection line of the two waypoints
                        Gizmos.DrawLine(p, pNext);

                        //Set arrow count based on arrowDrawType
                        var arrows = GetArrowCount(p, pNext, script);

                        //Draw arrows
                        for (var i = 1; i < arrows + 1; i++)
                        {
                            var point = Vector3.Lerp(p, pNext, (float)i / (arrows + 1));
                            DrawArrow(point, p - pNext, script.ArrowSizeWaypoint);
                        }
                    }
                }

                //Draw line linking segments
                foreach (var nextSegment in segment.ConnectedSegments)
                {
                    if (nextSegment != null)
                    {
                        var p1 = segment.Waypoints.Last().transform.position;
                        var p2 = nextSegment.Waypoints.First().transform.position;

                        Gizmos.color = new Color(1f, 1f, 0f);
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

            Gizmos.DrawLine(point, point + left);
            Gizmos.DrawLine(point, point + right);
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