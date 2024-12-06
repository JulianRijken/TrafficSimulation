using System.Collections.Generic;
using UnityEngine;

namespace TrafficSimulation.Scripts
{
    public class TrafficSystem : MonoBehaviour
    {
        public bool HideGizmos;
        public float SegmentDetectionThreshold = 0.1f;
        public ArrowDraw ArrowDrawType = ArrowDraw.ByLength;
        public int ArrowCount = 1;
        public float ArrowDistance = 5;
        public float ArrowSizeWaypoint = 1;
        public float ArrowSizeIntersection = 0.5f;
        public float WaypointSize = 0.5f;
        public string[] CollisionLayers;

        public List<Segment> Segments = new();
        public List<Intersection> Intersections = new();
        public Segment CurSegment;

        public List<Waypoint> GetAllWaypoints()
        {
            var points = new List<Waypoint>();

            foreach (var segment in Segments)
                points.AddRange(segment.Waypoints);

            return points;
        }

        public void SaveTrafficSystem()
        {
            var intersectionsFound = FindObjectsByType<Intersection>(FindObjectsSortMode.None);
            foreach (var intersection in intersectionsFound)
                intersection.SaveIntersectionStatus();
        }

        public void ResumeTrafficSystem()
        {
            var intersectionsFound = FindObjectsByType<Intersection>(FindObjectsSortMode.None);
            foreach (var intersection in intersectionsFound)
                intersection.ResumeIntersectionStatus();
        }
    }

    public enum ArrowDraw
    {
        FixedCount,
        ByLength,
        Off
    }
}