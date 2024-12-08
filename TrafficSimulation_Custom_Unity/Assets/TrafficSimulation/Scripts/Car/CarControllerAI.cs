using System;
using System.Linq;
using UnityEngine;

namespace TrafficSimulation
{
    
    public class CarControllerAI : MonoBehaviour
    {
        [SerializeField] private CarBehaviour _carBehaviour;
        [SerializeField] private TrafficSystem _trafficSystem;

        [SerializeField] private float _waypointDetectionThreshold = 0.5f;
        [SerializeField] private float _maxDistanceFromPath = 1.5f;

        [Header("Steering")] 
        [SerializeField] private float _steerOverAngle = 45.0f;
        [SerializeField] private float _steerSpeed = 10.0f;

        
        [Header("Power")] 
        [SerializeField] private float _slowdownDistance = 5.0f;
        [SerializeField] private float _slowdownSpeed = 5.0f;

        private float _streetMaxSpeed = 60.0f;
        private float _maxSpeed = 20.0f;
        private Waypoint _currentWaypoint;
        private Waypoint _lastWaypoint;
        private float _distanceFromPath;

        
        private Target _target;
        
        private struct Target
        {
            public Vector3 Position;
            public Vector3 Direction;
        }
        
        
        private void Start()
        {
            SetWaypointToClosest();
        }

        private void Update()
        {
            UpdateWaypoint();
            UpdateTargetPosition();

            _carBehaviour.SteerWheelInput = 0.1f;
            
            // UpdateSteering();
            // UpdateMaxSpeed();
            UpdateThrottleAndBreak();
        }

        private void OnDrawGizmos()
        {
            // Draw target rect 
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_target.Position, 0.5f);
        }

        private void UpdateSteering()
        {
            // Steer car 
            var directionToTarget = _target.Position - _carBehaviour.transform.position;
            var angleToWaypoint = Vector3.SignedAngle(_carBehaviour.transform.forward, directionToTarget, Vector3.up);
            float steerInputOverAngle = Mathf.Abs(angleToWaypoint) / _steerOverAngle;
            
            float steerSign = angleToWaypoint > 0 ? 1.0f : -1.0f;
            float targetSteerWheelInput = steerSign * steerInputOverAngle;
            float smoothSteerWheelInput = MathExtensions.LerpSmooth(_carBehaviour.SteerWheelInput, targetSteerWheelInput, Time.deltaTime,_steerSpeed);
            
            _carBehaviour.SteerWheelInput = smoothSteerWheelInput;

        }

        private void UpdateThrottleAndBreak()
        {
            // Set car power
            if (_carBehaviour.ForwardSpeedKPH < _maxSpeed)
            {
                _carBehaviour.ThrottleInput = 1.0f;
                _carBehaviour.BreakInput = 0.0f;
            }
            else
            {
                _carBehaviour.ThrottleInput = 0.0f;
                _carBehaviour.BreakInput = 1.0f;
            }
        }
        
        private void UpdateTargetPosition()
        {
            if (_lastWaypoint == null)
            {
                _target.Position = _currentWaypoint.transform.position;
                _target.Direction = _currentWaypoint.transform.forward;
            }
            else
            {            
                // Interpolate between waypoints
                var distanceToLastWaypoint = Vector3.Distance(_carBehaviour.transform.position, _lastWaypoint.transform.position);
                var distanceBetweenWaypoints = Vector3.Distance(_lastWaypoint.transform.position, _currentWaypoint.transform.position);
                var interpolationFactor = distanceToLastWaypoint / distanceBetweenWaypoints;
                _target.Position = Vector3.Lerp(_lastWaypoint.transform.position, _currentWaypoint.transform.position, interpolationFactor);
            }
        }


        private void UpdateWaypoint()
        {
            // Update waypoint
            if (_currentWaypoint == null)
            {
                SetWaypointToClosest();
                Debug.LogWarning("No waypoints found for car " + _carBehaviour.name);
            }

            if (IsOnWaypoint(_currentWaypoint))
            {
                _lastWaypoint = _currentWaypoint;
                _currentWaypoint = _trafficSystem.GetNextWaypoint(_currentWaypoint);
            }
        }

        private void SetWaypointToClosest()
        {
            _currentWaypoint = _trafficSystem.Waypoints
                .OrderBy(waypoint => Vector3.Distance(_carBehaviour.transform.position, waypoint.transform.position))
                .FirstOrDefault();
        }

        private bool IsOnWaypoint(Waypoint waypoint)
        {
            var distance = Vector3.Distance(_carBehaviour.transform.position, waypoint.transform.position);
            return distance < _waypointDetectionThreshold;
        }

        // private void UpdateMaxSpeed()
        // {
        //     _maxSpeed = _streetMaxSpeed;
        //
        //     var distanceToWaypoint = Vector3.Distance(_carBehaviour.transform.position, _currentWaypoint.transform.position);
        //
        //     if (distanceToWaypoint < _slowdownDistance)
        //     {
        //         if (_currentWaypoint.NextWaypoint == null)
        //         {
        //             _maxSpeed = _slowdownSpeed;
        //             return;
        //         }
        //
        //         var directionFromCurrentToNextCheckpoint = _currentWaypoint.NextWaypoint.transform.position - _currentWaypoint.transform.position;
        //         var directionFromCarToCurrentWaypoint = _currentWaypoint.transform.position - _carBehaviour.transform.position;
        //         var angleToNextWaypoint = Vector3.Angle(directionFromCurrentToNextCheckpoint,directionFromCarToCurrentWaypoint);
        //         
        //         if(angleToNextWaypoint > 45.0f)
        //         {
        //             _maxSpeed = _slowdownSpeed;
        //             return;
        //         }
        //         
        //         if(_distanceFromPath > _maxDistanceFromPath)
        //         {
        //             _maxSpeed = _slowdownSpeed;
        //             return;
        //         }
        //     }
        // }
    }
}