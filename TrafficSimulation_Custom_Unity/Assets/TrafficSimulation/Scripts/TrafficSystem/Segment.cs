using System.Collections.Generic;
using UnityEngine;

namespace TrafficSimulation
{
    public class Segment : MonoBehaviour
    {
        public List<Segment> ConnectedSegments;

        [HideInInspector] public int Id;
        [HideInInspector] public List<Waypoint> Waypoints;

        public bool IsOnSegment(Vector3 position)
        {
            // TODO: Find a more efficient way get the TrafficSystem component
            var trafficSystem = GetComponentInParent<TrafficSystem>();

            for (var i = 0; i < Waypoints.Count - 1; i++)
            {
                var waypoint1 = Waypoints[i].transform.position;
                var waypoint2 = Waypoints[i + 1].transform.position;

                var d1 = Vector3.Distance(position, waypoint1);
                var d2 = Vector3.Distance(position, waypoint2);
                var d3 = Vector3.Distance(waypoint1, waypoint2);
                var a = d1 + d2 - d3;

                if (a < trafficSystem.SegmentDetectionThreshold && a > -trafficSystem.SegmentDetectionThreshold)
                    return true;
            }

            return false;
        }
    }
}