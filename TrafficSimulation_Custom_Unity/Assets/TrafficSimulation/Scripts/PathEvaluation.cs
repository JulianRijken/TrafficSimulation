using System.Collections.Generic;
using System.IO;
using System.Linq;
using TrafficSimulation;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulation
{

    [DefaultExecutionOrder(-1)]
    public class PathEvaluation : MonoBehaviour
    {
        [SerializeField] private TrafficAgent _agent;
        [SerializeField] private TrafficAgentSteering _agentSteering;
        [SerializeField] private float _sampleRate = 0.1f;
        [SerializeField] private bool _writeToFile = true;
        [SerializeField] private bool _pauseOnFinish = true;
        [SerializeField] private bool _visualizePath = true;

        private bool finished = false;
        private TextWriter tw;

        [DebugGUIGraph(min: 0, max: 5, r: 0, g: 0, b: 0, autoScale: true, group: 0)]
        private float _deviationFromPath;
        
        private struct PathPoint
        {
            public Vector3 Position;
            public Color Color;
            public float Time;
            public float ForwardSpeed;
            public float ThrottleInput;
            public float BreakInput;
            public float SteeringSteerWheelInput;
            public Segment.Sample PathSample;
        }

        private List<PathPoint> _pastPoints = new List<PathPoint>();

        private void OnEnable()
        {
            if (_writeToFile)
            {
                string path = "Data/PathEvaluation.csv";
                if (File.Exists(path))
                    File.Delete(path);

                tw = new StreamWriter(path);
                tw.WriteLine(
                    "Distance Along Path,Time,Speed,Throttle Input,Break Input,Steering Steer Wheel Input,Deviation From Path");
                Debug.Log("Writing to " + path);
            }

            InvokeRepeating(nameof(WritePoint), 0.0f, _sampleRate);
        }

        private void OnDisable()
        {
            if (_writeToFile)
                tw.Close();
        }


        private void OnDrawGizmos()
        {
            if (isActiveAndEnabled == false)
                return;

            if (!Application.isPlaying)
                return;

            if (_visualizePath)
            {
                foreach (var point in _pastPoints)
                {
                    Gizmos.color = point.Color;
                    Gizmos.DrawSphere(point.Position, 0.2f);
                }
            }

            if (_agent.CurrentSegment == null)
                return;

            var sample = _agent.CurrentSegment.GetSampleFromPosition(_agent.CarBehaviour.Position);
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(sample.Position, 0.2f);
            Gizmos.DrawLine(_agent.CarBehaviour.Position, sample.Position);
        }

        private void OnFinish()
        {
            finished = true;

            if (_writeToFile)
            {
                tw.Close();
                Debug.Log("Finished writing to file");
            }

            if (_pauseOnFinish)
            {
                EditorApplication.isPaused = true;
                Debug.Log("Paused on finish");
            }
            
            float averageSpeed = _pastPoints.Sum(point => point.ForwardSpeed) / _pastPoints.Count;
            float averageThrottle = _pastPoints.Sum(point => point.ThrottleInput) / _pastPoints.Count;
            float averageBreak = _pastPoints.Sum(point => point.BreakInput) / _pastPoints.Count;
            float averageSteering = _pastPoints.Sum(point => point.SteeringSteerWheelInput) / _pastPoints.Count;
            float averageDeviation = _pastPoints.Sum(point => point.PathSample.DistanceFromPath) / _pastPoints.Count;
            
            Debug.Log($"Finished path evaluation");
            Debug.Log($"Average speed: {averageSpeed}");
            Debug.Log($"Average throttle: {averageThrottle}");
            Debug.Log($"Average break: {averageBreak}");
            Debug.Log($"Average steering: {averageSteering}");
            Debug.Log($"Average deviation: {averageDeviation}");
            
            // tw.WriteLine($"{_distanceAlongPath},{Time.time},{agent.ForwardSpeed},{agent.ThrottleInput},{agent.BreakInput},{agent.SteerWheelInput},{_distanceFromPath}");
        }
        
        private void WritePoint()
        {
            if (Application.isPlaying == false)
                return;

            if (finished)
                return;

            if (_agent.CurrentSample.IsAtEndOfSegment)
            {
                OnFinish();
                return;
            }

            _pastPoints.Add(new PathPoint
            {
                Position = _agent.CarBehaviour.Position,
                Color = _agentSteering.SteerMode switch
                {
                    TrafficAgentSteering.SteerModeType.PID => Color.red,
                    TrafficAgentSteering.SteerModeType.BackwardsCorrection => Color.blue,
                    TrafficAgentSteering.SteerModeType.DistanceCorrection => new Color(0, 0.8f, 0.5f, 1.0f)
                },
                Time = Time.time,
                ForwardSpeed = _agent.CarBehaviour.ForwardSpeed,
                ThrottleInput = _agent.CarBehaviour.ThrottleInput,
                BreakInput = _agent.CarBehaviour.BreakInput,
                SteeringSteerWheelInput = _agent.CarBehaviour.SteerWheelInput,
                PathSample = _agent.CurrentSample
            });

            _deviationFromPath = _agent.CurrentSample.DistanceFromPath;

        }
    }
}
