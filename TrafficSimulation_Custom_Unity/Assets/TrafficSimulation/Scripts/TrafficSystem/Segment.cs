using System;
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
        
        
        [Serializable]
        public struct Sample
        {
            public int WaypointIndex;
            public float DistanceAlongSegment;
            public float AlphaAlongSegment;
            public Vector3 Position;
            public Vector3 Direction;
            public Vector3 DirectionRight;
            
            public bool IsAtEndOfSegment => AlphaAlongSegment >= 1.0f - 1e-6f;
            public bool IsAtStartOfSegment => AlphaAlongSegment <= 0.0f + 1e-6f;
            
            public Vector3 GetDirectionToSampledPosition(Vector3 originalPosition)
            {
                return (Position - originalPosition).normalized;
            }
            
            public float GetSignedDistanceFromPath(Vector3 originalPosition)
            {
                var directionToSampledPosition = GetDirectionToSampledPosition(originalPosition);
                return Vector3.Dot(directionToSampledPosition, DirectionRight) * Vector3.Distance(originalPosition, Position);
            }
            
            public float GetDistanceFromPath(Vector3 originalPosition)
            {
                return Mathf.Abs(GetSignedDistanceFromPath(originalPosition));
            }
        }
        
        
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


        public Sample SampleFromAlpha(float alphaAlongPath)
        {
            float targetLength = alphaAlongPath * TotalLength;
            return SampleFromDistance(targetLength);
        }
        
        public Sample SampleFromDistance(float distanceAlongPath)
        {
            distanceAlongPath = Mathf.Clamp(distanceAlongPath, 0, TotalLength);
            
            // Find the two waypoints that the target length falls between
            int waypointIndex = 0;
            while (waypointIndex < cumulativeLengths.Count - 1 && cumulativeLengths[waypointIndex + 1] < distanceAlongPath)
                waypointIndex++;

            // Find the distance between the two waypoints
            float fromWaypointDistance = cumulativeLengths[waypointIndex];
            float toWaypointDistance = cumulativeLengths[waypointIndex + 1];
            float waypointDistance = toWaypointDistance - fromWaypointDistance;
            float waypointAlpha = (distanceAlongPath - fromWaypointDistance) / waypointDistance;

            // Interpolate between waypoints
            var fromWaypointPosition = Waypoints[waypointIndex].Position;
            var toWaypointPosition = Waypoints[waypointIndex + 1].Position;

            var sample = new Sample();
            sample.WaypointIndex = waypointIndex;
            sample.DistanceAlongSegment = Mathf.Clamp(distanceAlongPath, 0, TotalLength);
            sample.AlphaAlongSegment = Mathf.Clamp01(distanceAlongPath / TotalLength);
            sample.Position = Vector3.Lerp(fromWaypointPosition, toWaypointPosition, waypointAlpha);    
            sample.Direction = (toWaypointPosition - fromWaypointPosition).normalized;
            sample.DirectionRight = Vector3.Cross(sample.Direction, Vector3.up);
            return sample;
        }
        
        public Sample SampleFromPosition(Vector3 originalPosition)
        {
            var closestDistance = float.MaxValue;
            var closestSample = new Sample();

            for (int i = 0; i < Waypoints.Count - 1; i++)
            {
                var fromWaypointPosition = Waypoints[i].Position;
                var toWaypointPosition = Waypoints[i + 1].Position;
                var directionBetweenWaypoints = (toWaypointPosition - fromWaypointPosition).normalized;
                var distanceBetweenWaypoints = Vector3.Distance(fromWaypointPosition, toWaypointPosition);
                var alphaBetweenWaypoints = Mathf.Clamp01(Vector3.Dot(originalPosition - fromWaypointPosition, directionBetweenWaypoints) / distanceBetweenWaypoints);
                var sampledPosition = Vector3.Lerp(fromWaypointPosition, toWaypointPosition, alphaBetweenWaypoints);
                var distanceToSampledPosition = Vector3.Distance(originalPosition, sampledPosition);

                if (distanceToSampledPosition > closestDistance) 
                    continue;
                closestDistance = distanceToSampledPosition;
                
                closestSample.WaypointIndex = i;
                closestSample.AlphaAlongSegment  = (cumulativeLengths[i] + alphaBetweenWaypoints * distanceBetweenWaypoints) / TotalLength;
                closestSample.DistanceAlongSegment = closestSample.AlphaAlongSegment * TotalLength;
                closestSample.Position = sampledPosition;
                closestSample.Direction = directionBetweenWaypoints;
                closestSample.DirectionRight = Vector3.Cross(directionBetweenWaypoints, Vector3.up);
            }

            return closestSample;
        }
    }
}
