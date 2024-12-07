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
        private float _maxSpeed = 0;
        private Waypoint _currentWaypoint;
        private Waypoint _lastWaypoint;


        private void Start()
        {
            SetWaypointToClosest();
        }

        private void Update()
        {
            UpdateWaypoint();

            Vector3 targetPosition = _currentWaypoint.transform.position;

            bool farFromPath = false;
            
            // Interpolate between waypoints
            if (_lastWaypoint != null)
            {
                // If angle between car and waypoint is too big, interpolate between last and current waypoint
                
                
                var distanceToLastWaypoint = Vector3.Distance(_carBehaviour.transform.position, _lastWaypoint.transform.position);
                var distanceBetweenWaypoints = Vector3.Distance(_lastWaypoint.transform.position, _currentWaypoint.transform.position);
                var interpolationFactor = distanceToLastWaypoint / distanceBetweenWaypoints;
                var interpolatedTargetPosition = Vector3.Lerp(_lastWaypoint.transform.position, _currentWaypoint.transform.position, interpolationFactor);
                
                var interpolatedTargetPositionDistance = Vector3.Distance(_carBehaviour.transform.position, interpolatedTargetPosition);
                if (interpolatedTargetPositionDistance > _maxDistanceFromPath)
                {
                    targetPosition = interpolatedTargetPosition;
                    farFromPath = true;
                }
            }
            
            Debug.DrawLine(_carBehaviour.transform.position, targetPosition, Color.blue);
            
            
            // Steer car 
            var directionToTarget = targetPosition - _carBehaviour.transform.position;
            var angleToWaypoint = Vector3.SignedAngle(_carBehaviour.transform.forward, directionToTarget, Vector3.up);
            float steerInputOverAngle = Mathf.Abs(angleToWaypoint) / _steerOverAngle;
            
            float steerSign = angleToWaypoint > 0 ? 1.0f : -1.0f;
            float targetSteerWheelInput = steerSign * steerInputOverAngle;
            float smoothSteerWheelInput = MathExtentions.LerpSmooth(_carBehaviour.SteerWheelInput, targetSteerWheelInput, Time.deltaTime,_steerSpeed);
            
            _carBehaviour.SteerWheelInput = smoothSteerWheelInput;
            
            
            _maxSpeed = GetMaxSpeed();
            if(farFromPath)
                _maxSpeed = _slowdownSpeed;

            // Handle max speed
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

        private float GetMaxSpeed()
        {
            var distanceToWaypoint = Vector3.Distance(_carBehaviour.transform.position, _currentWaypoint.transform.position);

            if (distanceToWaypoint < _slowdownDistance)
            {
                if (_currentWaypoint.NextWaypoint == null)
                    return _slowdownSpeed;

                var directionFromCurrentToNextCheckpoint = _currentWaypoint.NextWaypoint.transform.position - _currentWaypoint.transform.position;
                var directionFromCarToCurrentWaypoint = _currentWaypoint.transform.position - _carBehaviour.transform.position;
                var angleToNextWaypoint = Vector3.Angle(directionFromCurrentToNextCheckpoint,directionFromCarToCurrentWaypoint);
                
                if(angleToNextWaypoint > 45.0f)
                    return _slowdownSpeed;
                
            }
            
            return _streetMaxSpeed;
        }
    }
}