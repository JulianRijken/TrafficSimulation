using System.Collections.Generic;
using UnityEngine;

namespace TrafficSimulation
{
    public class Segment : MonoBehaviour
    {
        public List<Segment> ConnectedSegments;

        [HideInInspector] public int Id;
        [HideInInspector] public List<Waypoint> Waypoints;

        private List<float> cumulativeLengths;
        
        public float TotalLength => cumulativeLengths[^1];
        
        private void Awake()
        {
            if (Waypoints == null || Waypoints.Count < 2)
                throw new System.InvalidOperationException("At least two waypoints are required to sample a position.");
            
            ComputeSegmentLengths();
        }
        
        private void ComputeSegmentLengths()
        {
            cumulativeLengths = new List<float> { 0f };

            for (int i = 1; i < Waypoints.Count; i++)
            {
                var segmentLength = Vector3.Distance(Waypoints[i - 1].Position, Waypoints[i].Position);
                cumulativeLengths.Add(cumulativeLengths[i - 1] + segmentLength);
            }
        }


        // Samples position from alpha along the segment
        public Vector3 SamplePositionFromAlpha(float alpha)
        {
            float targetLength = alpha * TotalLength;
            return SamplePositionFromDistanceAlongPath(targetLength);
        }
        
        public Vector3 SamplePositionFromDistanceAlongPath(float distance)
        {
            distance = Mathf.Clamp(distance, 0, TotalLength);
            
            // Find the two waypoints that the target length falls between
            int waypointIndex = 0;
            while (waypointIndex < cumulativeLengths.Count - 1 && cumulativeLengths[waypointIndex + 1] < distance)
                waypointIndex++;

            // Find the distance between the two waypoints
            float fromWaypointDistance = cumulativeLengths[waypointIndex];
            float toWaypointDistance = cumulativeLengths[waypointIndex + 1];
            float waypointDistance = toWaypointDistance - fromWaypointDistance;
            float waypointAlpha = (distance - fromWaypointDistance) / waypointDistance;

            // Interpolate between waypoints
            var fromWaypointPosition = Waypoints[waypointIndex].Position;
            var toWaypointPosition = Waypoints[waypointIndex + 1].Position;
            return Vector3.Lerp(fromWaypointPosition,toWaypointPosition, waypointAlpha);
        }
        
        public float SampleAlphaFromPosition(Vector3 position)
        {
            float closestDistance = float.MaxValue;
            float closestAlpha = 0f;

            for (int i = 0; i < Waypoints.Count - 1; i++)
            {
                var fromWaypointPosition = Waypoints[i].Position;
                var toWaypointPosition = Waypoints[i + 1].Position;
                var directionBetweenWaypoints = (toWaypointPosition - fromWaypointPosition).normalized;
                var distanceBetweenWaypoints = Vector3.Distance(fromWaypointPosition, toWaypointPosition);
                var alphaBetweenWaypoints = Vector3.Dot(position - fromWaypointPosition, directionBetweenWaypoints) / distanceBetweenWaypoints;
                var positionBetweenWaypoints = Vector3.Lerp(fromWaypointPosition, toWaypointPosition, alphaBetweenWaypoints);
                var distanceToClosestPosition = Vector3.Distance(position, positionBetweenWaypoints);
                    
                if(distanceToClosestPosition < closestDistance)
                {
                    closestDistance = distanceToClosestPosition;
                    closestAlpha  = (cumulativeLengths[i] + alphaBetweenWaypoints * distanceBetweenWaypoints) / TotalLength;
                }
            }
            return closestAlpha;    
        }
        
        public float SampleDistanceAlongPathFromPosition(Vector3 position)
        {
            return SampleAlphaFromPosition(position) * TotalLength;
        }
        
        public struct SegmentSample
        {
            public float DistanceAlongPath;
            public float Alpha;
            public Vector3 Position;
            public Vector3 DirectionBetweenWaypoints;
            public bool OnLeftSideOfSegment;
        }
        
        
        public SegmentSample GetSampleFromPosition(Vector3 position)
        {
            float closestDistance = float.MaxValue;
            SegmentSample closestSample = new SegmentSample();

            for (int i = 0; i < Waypoints.Count - 1; i++)
            {
                var fromWaypointPosition = Waypoints[i].Position;
                var toWaypointPosition = Waypoints[i + 1].Position;
                var directionBetweenWaypoints = (toWaypointPosition - fromWaypointPosition).normalized;
                var distanceBetweenWaypoints = Vector3.Distance(fromWaypointPosition, toWaypointPosition);
                var alphaBetweenWaypoints = Vector3.Dot(position - fromWaypointPosition, directionBetweenWaypoints) / distanceBetweenWaypoints;
                var positionBetweenWaypoints = Vector3.Lerp(fromWaypointPosition, toWaypointPosition, alphaBetweenWaypoints);
                var distanceToClosestPosition = Vector3.Distance(position, positionBetweenWaypoints);
                    
                if(distanceToClosestPosition < closestDistance)
                {
                    closestDistance = distanceToClosestPosition;
                    closestSample.Alpha  = (cumulativeLengths[i] + alphaBetweenWaypoints * distanceBetweenWaypoints) / TotalLength;
                    closestSample.DistanceAlongPath = closestSample.Alpha * TotalLength;
                    closestSample.Position = positionBetweenWaypoints;
                    closestSample.DirectionBetweenWaypoints = directionBetweenWaypoints;
                    closestSample.OnLeftSideOfSegment = Vector3.Dot(position - fromWaypointPosition, Vector3.Cross(directionBetweenWaypoints, Vector3.up)) > 0;
                }
            }

            return closestSample;
        }
    }
}
