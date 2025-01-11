using System.Collections.Generic;
using UnityEngine;

namespace TrafficSimulation
{
    public class TrafficSystem : MonoBehaviour
    {
        public bool HideGizmos;
        public float SegmentDetectionThreshold = 0.1f;
        public ArrowDrawType ArrowDrawType = ArrowDrawType.FixedCount;
        public int ArrowCount = 1;
        public float ArrowDistance = 5;
        public float ArrowSizeWaypoint = 1;
        public float ArrowSizeIntersection = 0.5f;
        public float WaypointSize = 0.5f;
        public float WaypointHeight = 0.5f;
        public int FontSize = 22;
        public LayerMask WaypointGroundSnapIgnoreLayerMask;
        
        
        public Segment CurSegment;

        [HideInInspector] public List<Segment> Segments = new();
        [HideInInspector] public List<Waypoint> Waypoints = new();

        public Waypoint GetNextWaypointRandom(Waypoint currentWaypoint)
        {
            if (currentWaypoint.NextWaypoint != null)
                return currentWaypoint.NextWaypoint;

            var connectedSegments = currentWaypoint.Segment.ConnectedSegments;
            if (connectedSegments.Count > 0)
                return connectedSegments[Random.Range(0, connectedSegments.Count)].Waypoints[0];

            return null;
        }
        
        public Segment GetNextSegmentRandom(Segment currentSegment)
        {
            if (currentSegment.ConnectedSegments.Count > 0)
                return currentSegment.ConnectedSegments[Random.Range(0, currentSegment.ConnectedSegments.Count)];
            
            return null;
        }
        
        public Segment GetClosestSegment(Vector3 position)
        {
            float closestDistance = float.MaxValue;
            Segment closestSegment = null;
            
            foreach (var segment in Segments)
            {
                var sample = segment.SampleFromPositionClamped(position);

                float distanceFromPath = Vector3.Distance(sample.Position, position);
                if (float.IsNaN(distanceFromPath))
                {
                    Debug.LogError("Distance from path is NaN");
                    Debug.Log("segment: " + segment.name);
                    continue;
                }
                
                if (distanceFromPath > closestDistance) 
                    continue;
                
                if(sample.IsAtEndOfSegment)
                    continue;
                
                closestDistance = distanceFromPath;
                closestSegment = segment;
            }

            return closestSegment;
        }
    }

    public enum ArrowDrawType
    {
        FixedCount,
        ByLength,
        Off
    }
}