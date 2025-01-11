using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficSimulation
{
    public class Segment : MonoBehaviour
    {
        public List<Segment> ConnectedSegments;

        [HideInInspector] public int Id;
        [HideInInspector] public List<Waypoint> Waypoints = new();

        private List<float> cumulativeLengths;
        
        public float TotalLength => cumulativeLengths[^1];
        public Vector3 StartPosition => Waypoints[0].Position;
        public Vector3 EndPosition => Waypoints[^1].Position;
        
        
        [Serializable]
        public struct Sample
        {
            public int WaypointIndex;
            public float SpeedLimitKph;
            public float DistanceAlongSegment;
            public float AlphaAlongSegment;
            public float DistanceToSegmentEnd;
            public Vector3 Position;
            public Vector3 DirectionForward;
            public Vector3 DirectionRight;
            public Segment Segment;
            
            public bool IsAtEndOfSegment => AlphaAlongSegment >= 1.0f - 1e-6f;
            public bool IsAtStartOfSegment => AlphaAlongSegment <= 0.0f + 1e-6f;
            
            // Meters / second
            public float SpeedLimit => SpeedLimitKph / 3.6f;

            public static Sample Interpolate(Sample from, Sample to, float distance)
            {
                float distanceBetweenSamples = Vector3.Distance(from.Position, to.Position);
                float alpha = distance / distanceBetweenSamples;
                
                Sample interpolatedSample = from;
                interpolatedSample.SpeedLimitKph = Mathf.Lerp(from.SpeedLimitKph, to.SpeedLimitKph, alpha);
                interpolatedSample.DistanceAlongSegment = from.DistanceAlongSegment + distance;

                float totalLength = from.Segment.TotalLength + distanceBetweenSamples;
                
                interpolatedSample.AlphaAlongSegment = interpolatedSample.DistanceAlongSegment  / totalLength;
                interpolatedSample.DistanceToSegmentEnd = totalLength - interpolatedSample.DistanceAlongSegment;
                interpolatedSample.Position = Vector3.Lerp(from.Position, to.Position, alpha);
                interpolatedSample.DirectionForward = (to.Position - from.Position).normalized;
                interpolatedSample.DirectionRight = Vector3.Cross(interpolatedSample.DirectionForward, Vector3.up);

                return interpolatedSample;
            }
    
            
            public Vector3 GetDirectionToSampledPosition(Vector3 originalPosition)
            {
                return (Position - originalPosition).normalized;
            }
            
            public float GetSidewaysSignedDistanceFromPath(Vector3 originalPosition)
            {
                var directionToSampledPosition = GetDirectionToSampledPosition(originalPosition);
                return Vector3.Dot(directionToSampledPosition, DirectionRight) * Vector3.Distance(originalPosition, Position);
            }
            public float GetForwardSignedDistanceFromPath(Vector3 originalPosition)
            {
                var directionToSampledPosition = GetDirectionToSampledPosition(originalPosition);
                return Vector3.Dot(directionToSampledPosition, DirectionForward) * Vector3.Distance(originalPosition, Position);
            }
            
            public float GetSidewaysDistanceFromPath(Vector3 originalPosition)
            {
                return Mathf.Abs(GetSidewaysSignedDistanceFromPath(originalPosition));
            }
            
            public float GetForwardDistanceFromPath(Vector3 originalPosition)
            {
                return Mathf.Abs(GetForwardSignedDistanceFromPath(originalPosition));
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
        
        public Sample SampleFromDistanceClamped(float distanceAlongPath)
        {
            float clampedDistance = Mathf.Clamp(distanceAlongPath, 0, TotalLength);
            
            // Find the two waypoints that the target length falls between
            int waypointIndex = 0;
            while (waypointIndex < cumulativeLengths.Count - 1 && cumulativeLengths[waypointIndex + 1] < clampedDistance)
                waypointIndex++;

            // Find the distance between the two waypoints
            float fromWaypointDistance = cumulativeLengths[waypointIndex];
            float toWaypointDistance = cumulativeLengths[waypointIndex + 1];
            float waypointDistance = toWaypointDistance - fromWaypointDistance;
            float waypointAlpha = (clampedDistance - fromWaypointDistance) / waypointDistance;

            // Interpolate between waypoints
            var fromWaypointPosition = Waypoints[waypointIndex].Position;
            var toWaypointPosition = Waypoints[waypointIndex + 1].Position;

            var sample = new Sample();
            sample.WaypointIndex = waypointIndex;
            sample.SpeedLimitKph = Waypoints[waypointIndex].SpeedLimitKph;
            sample.DistanceAlongSegment = clampedDistance; 
            sample.AlphaAlongSegment = Mathf.Clamp01(clampedDistance / TotalLength);
            sample.DistanceToSegmentEnd = TotalLength - clampedDistance;
            sample.Position = Vector3.Lerp(fromWaypointPosition, toWaypointPosition, waypointAlpha);    
            sample.DirectionForward = (toWaypointPosition - fromWaypointPosition).normalized;
            sample.DirectionRight = Vector3.Cross(sample.DirectionForward, Vector3.up);
            sample.Segment = this;
            return sample;
        }
        
        public Sample SampleFromPositionClamped(Vector3 originalPosition)
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
                closestSample.SpeedLimitKph = Waypoints[i].SpeedLimitKph;
                closestSample.AlphaAlongSegment  = (cumulativeLengths[i] + alphaBetweenWaypoints * distanceBetweenWaypoints) / TotalLength;
                closestSample.DistanceAlongSegment = closestSample.AlphaAlongSegment * TotalLength;
                closestSample.DistanceToSegmentEnd = TotalLength - closestSample.DistanceAlongSegment;
                closestSample.Position = sampledPosition;
                closestSample.DirectionForward = directionBetweenWaypoints;
                closestSample.DirectionRight = Vector3.Cross(directionBetweenWaypoints, Vector3.up);
                closestSample.Segment = this;
            }

            return closestSample;
        }
    }
}
