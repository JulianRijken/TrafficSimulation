using System;
using System.Linq;
using UnityEngine;

namespace TrafficSimulation
{

    public class CarControllerAI : MonoBehaviour
    {
        [SerializeField] private CarBehaviour _carBehaviour;
        [SerializeField] private TrafficSystem _trafficSystem;

        [Header("Custom")] 
        [SerializeField] private float _streetMaxSpeed = 60.0f;

        [Header("Steering")] 
        [SerializeField] private float _waypointDetectionThreshold = 2.0f;
        [SerializeField] private float _directionLerpDistance = 3.0f;
        [SerializeField] private float _steerOverAngle = 45.0f;
        [SerializeField] private float _steerSmoothingSpeed = 10.0f;
        
        [Header("Power")] 
        [SerializeField] private float _minSpeed = 5.0f;
        [SerializeField] private float _slowdownDistanceMultiplier = 5.0f;
        
        [Header("Danger")] 
        [SerializeField] private float _upcomingTurnDangerScale = 1.0f;
        [SerializeField] private float _alignmentDangerScale = 1.0f;
        [SerializeField] private float _elevationDangerScale = 1.0f;

        [Header("Collision Detection")]
        [SerializeField] private float _secondsFromCarInFront = 2.0f;
        [SerializeField] private float _minDistanceCheck = 1.0f;
        [SerializeField] private Sensor _frontSensor;
        
        [Header("Advanced")]
        [SerializeField] private float _dangerLevelReductionSpeed = 1.0f;
        
        
        private Segment _nextSegment;
        private Waypoint _currentWaypoint;
        private Waypoint _lastWaypoint;
        private Vector3 _steerDirection;
        private Target _target;
        
        private CarControllerAI _carInFront;
        
        private float _upcomingTurnDanger = 0.0f;
        private float _alignmentDanger = 0.0f;
        private float _elevationDanger = 0.0f;
        private float _intersectionDanger = 0.0f;
        private float _dangerLevel = 0.0f;
        
        private float _slowdownDistance = 0.0f;
        private float _targetSpeed = 0.0f;
        
        public IntersectionStateType IntersectionState = IntersectionStateType.None;
        
        public event Action OnCarCollision;
        
        private struct Target
        {
            public Vector3 Position;
            public Vector3 Direction;
        }
        
        public enum IntersectionStateType
        {
            None,
            Waiting,
            Moving
        }

        
        public Vector3 CarForwardPlaner => new Vector3(_carBehaviour.transform.forward.x, 0, _carBehaviour.transform.forward.z).normalized;
        public Vector3 CarPosition => _carBehaviour.transform.position;
        public float DistanceFromPath => Vector3.Distance(new Vector3(CarPosition.x,0,CarPosition.z), new Vector3(_target.Position.x,0,_target.Position.z));
        
        public Segment NextSegment => _nextSegment;
        public Segment CurrentSegment => _currentWaypoint.Segment;
        
        public float TargetSpeed => _targetSpeed;
        public float CurrentSpeed => _carBehaviour.ForwardSpeed;
        public Vector3 Velocity => _carBehaviour.Velocity;
        
        private void Awake()
        {
            if (_trafficSystem == null)
                _trafficSystem = FindFirstObjectByType<TrafficSystem>();
        }

        private void Start()
        {
            ForceSetWaypointToClosest();
        }

        private void Update()
        {
            UpdateWaypoint();
            UpdateTargetPosition();
            UpdateSteering();
            UpdateDanger();
            UpdateCollisionDetection();
            UpdateTargetSpeed();
            UpdatePower();
        }
        


        private void OnCollisionEnter(Collision other)
        {
            if(other.gameObject.GetComponent<CarBehaviour>() != null)
                OnCarCollision?.Invoke();
        }

        private void OnDrawGizmos()
        {
            // Only when the game is running
            if (!Application.isPlaying)
                return;
            
            Gizmos.color = Color.cyan;
            Vector3 from = _target.Position + Vector3.up;
            MathExtensions.DrawArrow(from, _target.Direction, 1.0f);
            Gizmos.DrawLine(CarPosition + Vector3.up, from);

            Gizmos.color = Color.red;
            MathExtensions.DrawArrow(from,_steerDirection, 1.0f);
            
            Gizmos.color = Color.green;
            MathExtensions.DrawArrow(from,CarForwardPlaner, 1.0f);
            
            Gizmos.color = Color.Lerp(Color.white, Color.black, _dangerLevel);
            MathExtensions.DrawCircle(CarPosition + Vector3.up, _slowdownDistance);

            Gizmos.color = IntersectionState switch
            {
                IntersectionStateType.Waiting => Color.red,
                IntersectionStateType.Moving => Color.yellow,
                _ => Color.green
            };

            Gizmos.DrawSphere(CarPosition + Vector3.up * 2, 0.4f);
            
            // Draw car in front line
            if (_carInFront != null)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawLine(CarPosition + Vector3.up, _carInFront.CarPosition + Vector3.up);
            }
        }

        


        private void UpdateTargetPosition()
        {
            // No checkpoint found
            if (_currentWaypoint == null)
            {
                _target.Position = CarPosition;
                _target.Direction = CarForwardPlaner;
                return;
            }
            
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
                var carPosition = CarPosition;
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
        
        private void UpdateSteering()
        {
            // Get steer direction
            var directionToTarget = _target.Position - CarPosition;
            var distanceToTarget = directionToTarget.magnitude;
            _steerDirection = Vector3.Lerp(_target.Direction, directionToTarget, distanceToTarget / _directionLerpDistance);
            _steerDirection.y = 0.0f;
            _steerDirection.Normalize();
            
            // Steer to direction
            var angleToTarget = Vector3.SignedAngle(CarForwardPlaner, _steerDirection, Vector3.up);
            float steerInputOverAngle = angleToTarget / _steerOverAngle;
            _carBehaviour.SteerWheelInput = Mathf.MoveTowards(_carBehaviour.SteerWheelInput, steerInputOverAngle, Time.deltaTime * _steerSmoothingSpeed);
        }


        private void UpdateDanger()
        {
            _intersectionDanger = 0.0f;
            _upcomingTurnDanger = 0.0f;
            _elevationDanger = 0.0f;
            _alignmentDanger = 0.0f;
            _slowdownDistance = 0.0f;

            if (_currentWaypoint != null)
            {
                _slowdownDistance = _carBehaviour.ForwardSpeed * _slowdownDistanceMultiplier;

                // Check if the car is driving away from the waypoint
                _alignmentDanger = Mathf.Abs(_carBehaviour.SteerWheelInput) * _alignmentDangerScale;

                // Add danger level based on distance to next waypoint
                var distanceToNextWaypoint = Vector3.Distance(CarPosition, _currentWaypoint.transform.position);
                if (distanceToNextWaypoint < _slowdownDistance)
                {
                    // Check if the current waypoint is the last one
                    if (_currentWaypoint.NextWaypoint == null)
                    {
                        // Calculate intersection danger
                        _intersectionDanger = 1.0f;
                    }
                    else
                    {
                        // Calculate upcoming turn danger
                        var directionToNextCheckpoint = _currentWaypoint.NextWaypoint.transform.position -
                                                        _currentWaypoint.transform.position;
                        var angleToNextCheckpoint = Vector3.Angle(CarForwardPlaner, directionToNextCheckpoint);
                        _upcomingTurnDanger = Mathf.Abs(angleToNextCheckpoint / 90.0f) * _upcomingTurnDangerScale;


                        // Calculate elevation danger
                        if (_lastWaypoint != null)
                        {
                            var elevation = _currentWaypoint.NextWaypoint.transform.position.y -
                                            _lastWaypoint.transform.position.y;
                            _elevationDanger = Mathf.Abs(elevation) * _elevationDangerScale;
                        }
                    }
                }
            }

            var targetDangerLevel = Mathf.Clamp01(_upcomingTurnDanger + _alignmentDanger + _elevationDanger + _intersectionDanger);
            if(targetDangerLevel > _dangerLevel)
                _dangerLevel = targetDangerLevel;
            else
                _dangerLevel = Mathf.MoveTowards(_dangerLevel, targetDangerLevel, Time.deltaTime * _dangerLevelReductionSpeed);
        }
        
        private void UpdateCollisionDetection()
        {
            // TODO: This whole method is very basic
            _carInFront = null;
            
            if(_currentWaypoint == null)
                return;

            
            float checkDistance = _carBehaviour.ForwardSpeed * _secondsFromCarInFront;
            checkDistance = Mathf.Max(checkDistance, _minDistanceCheck);
            
            // Rotate sensor to match target rotation
            _frontSensor.transform.rotation = Quaternion.LookRotation(_target.Direction, Vector3.up);
            
            if (_frontSensor.Sense(checkDistance, out var hit))
            {
                
                if (!hit.collider.tag.Equals("AutonomousVehicle"))
                    return;
                
                _carInFront = hit.collider.GetComponentInParent<CarControllerAI>();
            }
        }
        
        private void UpdateTargetSpeed()
        {
            if (_currentWaypoint == null)
            {
                _targetSpeed = 0.0f;
                return;
            }

            if (IntersectionState == IntersectionStateType.Waiting)
            {
                _targetSpeed = 0.0f;
                return;
            }
            
            if(_carInFront != null)
            {
                float relativeSpeed = Vector3.Dot(_carInFront.Velocity, CarForwardPlaner);
                _targetSpeed = relativeSpeed;
                return;
            }
            
            _targetSpeed = Mathf.Lerp(_streetMaxSpeed,_minSpeed, _dangerLevel);
        }

        private void UpdatePower()
        {
            if (_currentWaypoint == null)
            {
                _carBehaviour.ThrottleInput = 0.0f;
                _carBehaviour.BreakInput = 0.0f;
                _carBehaviour.IsHandBrakeEngaged = true;
                return;
            }
            
            // Set car power
            if (_carBehaviour.ForwardSpeedKPH < _targetSpeed)
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
        
        
        private void ForceSetWaypointToClosest()
        {
            _currentWaypoint = _trafficSystem.Waypoints
                .OrderBy(waypoint => Vector3.Distance(CarPosition, waypoint.transform.position))
                .FirstOrDefault();
            
            _nextSegment = _trafficSystem.GetNextSegmentRandom(_currentWaypoint.Segment);
        }
        
        
        private void UpdateWaypoint()
        {
            if (_currentWaypoint == null)
                return;
            
            // When on checkpoint
            if (IsOnWaypoint(_currentWaypoint))
            {
                _lastWaypoint = _currentWaypoint;
             
                // If there is no next waypoint, get a random one
                if (_currentWaypoint.NextWaypoint != null)
                {
                    _currentWaypoint = _currentWaypoint.NextWaypoint;
                }
                else
                {
                    if (_nextSegment == null)
                    {
                        Debug.LogWarning("Car has no next segment");
                        return;
                    }
                    
                    // Move to next segment
                    _currentWaypoint = _nextSegment.Waypoints.First();
                    _nextSegment = _trafficSystem.GetNextSegmentRandom(_currentWaypoint.Segment);
                }
            }
        }
        
        private bool IsOnWaypoint(Waypoint waypoint)
        {
            var distance = Vector3.Distance(CarPosition, waypoint.transform.position);
            return distance < _waypointDetectionThreshold;
        }
    }
}