using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("TextSize")] public int FontSize = 22;
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
    }

    public enum ArrowDrawType
    {
        FixedCount,
        ByLength,
        Off
    }
}