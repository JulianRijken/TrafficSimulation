using System;
using System.Linq;
using UnityEngine;

namespace TrafficSimulation
{
    
    public class CarControllerAI : MonoBehaviour
    {
        [SerializeField] private CarBehaviour _carBehaviour;
        [SerializeField] private TrafficSystem _trafficSystem;


        [Header("Steering")] 
        [SerializeField] private float _waypointDetectionThreshold = 2.0f;
        [SerializeField] private float _directionLerpDistance = 3.0f;
        [SerializeField] private float _steerOverAngle = 45.0f;
        [SerializeField] private float _steerSmoothingSpeed = 10.0f;

        
        [Header("Power")] 
        [SerializeField] private float _slowdownDistance = 5.0f;
        [SerializeField] private float _slowdownSpeed = 5.0f;

        
        private float _streetMaxSpeed = 60.0f;
        private float _maxSpeed = 30.0f;
        
        private Waypoint _currentWaypoint;
        private Waypoint _lastWaypoint;
        private Vector3 _steerDirection;
        private Target _target;
        
        private struct Target
        {
            public Vector3 Position;
            public Vector3 Direction;
        }
        
        public Vector3 CarForwardPlaner => new Vector3(_carBehaviour.transform.forward.x, 0, _carBehaviour.transform.forward.z).normalized;
        
        private void Start()
        {
            SetWaypointToClosest();
        }

        private void Update()
        {
            UpdateWaypoint();
            UpdateTargetPosition();

            
            // Get steer direction
            var directionToTarget = _target.Position - _carBehaviour.transform.position;
            var distanceToTarget = directionToTarget.magnitude;
            _steerDirection = Vector3.Lerp(_target.Direction, directionToTarget, distanceToTarget / _directionLerpDistance);
            _steerDirection.y = 0.0f;
            _steerDirection.Normalize();
            
            // Steer to direction
            var angleToTarget = Vector3.SignedAngle(CarForwardPlaner, _steerDirection, Vector3.up);
            float steerInputOverAngle = angleToTarget / _steerOverAngle;
            _carBehaviour.SteerWheelInput = Mathf.MoveTowards(_carBehaviour.SteerWheelInput, steerInputOverAngle, Time.deltaTime * _steerSmoothingSpeed);
            
            
            // UpdateMaxSpeed();
            UpdateThrottleAndBreak();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Vector3 from = _target.Position + Vector3.up;
            MathExtensions.DrawArrow(from, _target.Direction, 1.0f);
            
            Gizmos.color = Color.red;
            MathExtensions.DrawArrow(from,_steerDirection, 1.0f);
            
            Gizmos.color = Color.green;
            MathExtensions.DrawArrow(from,CarForwardPlaner, 1.0f);
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
                
                if(_currentWaypoint.NextWaypoint != null)
                    _target.Direction = _currentWaypoint.NextWaypoint.transform.position - _currentWaypoint.transform.position;
                else
                    throw new Exception("No next or previous waypoint found");
            }
            else
            {            
                // Project car position on the line between the last and current waypoint
                var carPosition = _carBehaviour.transform.position;
                var lastWaypointPosition = _lastWaypoint.transform.position;
                var currentWaypointPosition = _currentWaypoint.transform.position;
                
                var direction = currentWaypointPosition - lastWaypointPosition;
                var distance = Vector3.Distance(lastWaypointPosition, currentWaypointPosition);
                var time = Mathf.Clamp01(Vector3.Dot(carPosition - lastWaypointPosition, direction.normalized) / distance);
                _target.Position = lastWaypointPosition + time * direction;
                _target.Direction = direction;
            }

            _target.Direction.y = 0;
            _target.Direction.Normalize();
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