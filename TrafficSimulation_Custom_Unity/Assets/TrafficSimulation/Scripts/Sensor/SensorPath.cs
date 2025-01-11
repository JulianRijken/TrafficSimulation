using System.Collections.Generic;
using UnityEngine;

namespace TrafficSimulation
{
    public class SensorPath : Sensor
    {
        [SerializeField] private float _samplePerUnit = 1;
        [SerializeField] private float _minAngle = 10;

        // TODO: make list a better data structure to reuse like how a vector works in c++
        private List<Vector3> _samples = new List<Vector3>();
        
        private void OnDrawGizmos()
        {
            if(Application.isPlaying == false)
                return;

            if(_agent.CurrentSegment == null)
                return;
            
            if(_agent.Settings.DebugPathSensor == false)
                return;
            
            foreach (var sample in _samples)
            {
                Gizmos.color = _agent.Settings.DebugPathSensorColor;
                Gizmos.DrawSphere(sample, 0.5f);
            }
            
            Gizmos.color = _agent.Settings.DebugPathCastSensorColor;
            for (int i = 0; i < _samples.Count - 1; i++)
            {
                Vector3 fromSample = _samples[i];
                Vector3 toSample = _samples[i + 1];
                Vector3 direction = (toSample - fromSample).normalized;
                float distance = Vector3.Distance(fromSample, toSample);
                Quaternion rotation = Quaternion.LookRotation(direction);

                // Set up the transformation matrix for the box cast visualization
                Matrix4x4 boxMatrix = Matrix4x4.TRS(
                    fromSample + direction * distance * 0.5f,
                    rotation, 
                    new Vector3(_size.x * 2.0f, _size.y * 2.0f, distance));
                Gizmos.matrix = boxMatrix;

                // Draw the wireframe box
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
            }

            // Reset the Gizmos matrix
            Gizmos.matrix = Matrix4x4.identity;
        }

        public override Result Sense(float senseDistance)
        {
            var result = new Result
            {
                Distance = float.MaxValue
            };
            
            int sampleCount = Mathf.CeilToInt(Mathf.Max(senseDistance * _samplePerUnit,1));
            
            // Update samples
            _samples.Clear();
            _samples.Add(transform.position);
            for (int i = 0; i < sampleCount; i++)
            {
                float alpha = i / (float)sampleCount;
                float distanceBetweenSamples = senseDistance / sampleCount;
                float distanceAlongPath = alpha * senseDistance + _agent.CurrentSample.DistanceAlongSegment + distanceBetweenSamples;
                Segment.Sample sample = _agent.SampleFromDistanceExtended(distanceAlongPath);
                _samples.Add(sample.Position);
            }
            
            while (true)
            {
                int removedSamples = 0;
                for (int i = 1; i < _samples.Count - 1; i++)
                {
                    Vector3 directionToCurrentSample = _samples[i] - _samples[i - 1];
                    Vector3 directionToNextSample = _samples[i + 1] - _samples[i];
                    float angle = Vector3.Angle(directionToCurrentSample, directionToNextSample);
                    
                    if (angle < _minAngle)
                    {
                        _samples.RemoveAt(i);
                        removedSamples++;
                    }
                }
                
                if (removedSamples == 0)
                    break;
            }

            float totalDistance = 0.0f;
            for (int i = 0; i < _samples.Count - 1; i++)
            {
                Vector3 fromSample = _samples[i];
                Vector3 toSample = _samples[i + 1];
                Vector3 direction = (toSample - fromSample).normalized;
                Quaternion rotation = Quaternion.LookRotation(direction);
                float distance = Vector3.Distance(fromSample, toSample);
                
                bool didHit = Physics.BoxCast(fromSample, _size, direction, out var hit, rotation, distance, _layerMask);

                if (didHit)
                {
                    result.Distance = totalDistance + hit.distance;
                    if(hit.rigidbody != null)
                        result.Velocity = hit.rigidbody.linearVelocity;
                    
                    break;
                }
                
                totalDistance += distance;
            }
            
            return result;
        }
    }
}
