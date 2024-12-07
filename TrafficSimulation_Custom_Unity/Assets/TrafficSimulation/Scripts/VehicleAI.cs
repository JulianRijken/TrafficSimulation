using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TrafficSimulation.Scripts
{
    public struct Target
    {
        public int segmentIndex;
        public int waypointIndex;
    }

    public enum Status
    {
        Go,
        Stop,
        SlowDown
    }

    [RequireComponent(typeof(Vehicle))]
    public class VehicleAI : MonoBehaviour
    {
        [Header("Traffic System")] [Tooltip("Current active traffic system")]
        public TrafficSystem _trafficSystem;

        [Tooltip(
            "Determine when the vehicle has reached its target. Can be used to \"anticipate\" earlier the next waypoint (the higher this number his, the earlier it will anticipate the next waypoint)")]
        public float _waypointThresh = 2.5f;

        [Header("Radar")] [Tooltip("Empty gameobject from where the rays will be casted")]
        public Transform _raycastAnchor;

        [Tooltip("Length of the casted rays")] public float _raycastLength = 3;

        [Tooltip("Spacing between each rays")] public int _raySpacing = 3;

        [Tooltip("Number of rays to be casted")]
        public int _raysNumber = 8;

        [Tooltip("If detected vehicle is below this distance, ego vehicle will stop")]
        public float _emergencyBrakeThresh = 1.5f;

        [Tooltip("If detected vehicle is below this distance (and above, above distance), ego vehicle will slow down")]
        public float _slowDownThresh = 4.0f;

        [HideInInspector] public Status _vehicleStatus = Status.Go;

        private Target _currentTarget;
        private Target _futureTarget;


        private float _initMaxSpeed;
        private int _pastTargetSegment = -1;
        private Vehicle _vehicle;

        private void Start()
        {
            if (_trafficSystem == null)
                _trafficSystem = FindFirstObjectByType<TrafficSystem>();

            _vehicle = GetComponent<Vehicle>();

            _initMaxSpeed = _vehicle.MaxSpeed;
            UpdateWaypoint();
        }

        private void Update()
        {
            if (_trafficSystem == null)
                return;

            WaypointChecker();
            MoveVehicle();
        }

        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("AutonomousVehicle"))
                OnVehicleCollision?.Invoke();
        }

        private void OnDrawGizmos()
        {
            // Draw line from car to target
            Gizmos.color = Color.blue;

            if (_trafficSystem == null)
                return;

            var segment = _trafficSystem.Segments[_currentTarget.segmentIndex];
            var waypoint = segment.Waypoints[_currentTarget.waypointIndex];
            Gizmos.DrawLine(transform.position, waypoint.transform.position);
        }

        public static event Action OnVehicleCollision;

        private void WaypointChecker()
        {
            var waypoint = _trafficSystem.Segments[_currentTarget.segmentIndex].Waypoints[_currentTarget.waypointIndex]
                .gameObject;

            //Position of next waypoint relative to the car
            var wpDist = transform.InverseTransformPoint(new Vector3(waypoint.transform.position.x,
                transform.position.y, waypoint.transform.position.z));

            //Go to next waypoint if arrived to current
            if (wpDist.magnitude < _waypointThresh)
            {
                //Get next target
                _currentTarget.waypointIndex++;
                if (_currentTarget.waypointIndex >=
                    _trafficSystem.Segments[_currentTarget.segmentIndex].Waypoints.Count)
                {
                    _pastTargetSegment = _currentTarget.segmentIndex;
                    _currentTarget.segmentIndex = _futureTarget.segmentIndex;
                    _currentTarget.waypointIndex = 0;
                }

                //Get future target
                _futureTarget.waypointIndex = _currentTarget.waypointIndex + 1;
                if (_futureTarget.waypointIndex >= _trafficSystem.Segments[_currentTarget.segmentIndex].Waypoints.Count)
                {
                    _futureTarget.waypointIndex = 0;
                    _futureTarget.segmentIndex = GetNextSegmentId();
                }
            }
        }

        private void MoveVehicle()
        {
            //Default, full acceleration, no break and no steering
            float acc = 1;
            float brake = 0;
            float steering = 0;
            _vehicle.MaxSpeed = _initMaxSpeed;

            //Calculate if there is a planned turn
            var targetTransform = _trafficSystem.Segments[_currentTarget.segmentIndex]
                .Waypoints[_currentTarget.waypointIndex]
                .transform;
            var futureTargetTransform = _trafficSystem.Segments[_futureTarget.segmentIndex]
                .Waypoints[_futureTarget.waypointIndex]
                .transform;
            var futureVel = futureTargetTransform.position - targetTransform.position;
            var futureSteering = Mathf.Clamp(transform.InverseTransformDirection(futureVel.normalized).x, -1, 1);

            //Check if the car has to stop
            if (_vehicleStatus == Status.Stop)
            {
                acc = 0;
                brake = 1;
                _vehicle.MaxSpeed = Mathf.Min(_vehicle.MaxSpeed / 2f, 5f);
            }
            else
            {
                //Not full acceleration if have to slow down
                if (_vehicleStatus == Status.SlowDown)
                {
                    acc = .3f;
                    brake = 0f;
                }

                //If planned to steer, decrease the speed
                if (futureSteering > .3f || futureSteering < -.3f)
                    _vehicle.MaxSpeed = Mathf.Min(_vehicle.MaxSpeed, _vehicle.SteeringSpeedMax);

                //2. Check if there are obstacles which are detected by the radar
                float hitDist;
                var obstacle = GetDetectedObstacles(out hitDist);

                //Check if we hit something
                if (obstacle != null)
                {
                    Vehicle otherVehicle = null;
                    otherVehicle = obstacle.GetComponent<Vehicle>();

                    ///////////////////////////////////////////////////////////////
                    //Differenciate between other vehicles AI and generic obstacles (including controlled vehicle, if any)
                    if (otherVehicle != null)
                    {
                        //Check if it's front vehicle
                        var dotFront = Vector3.Dot(transform.forward, otherVehicle.transform.forward);

                        //If detected front vehicle max speed is lower than ego vehicle, then decrease ego vehicle max speed
                        if (otherVehicle.MaxSpeed < _vehicle.MaxSpeed && dotFront > .8f)
                        {
                            var ms = Mathf.Max(_vehicle.GetSpeedMS(otherVehicle.MaxSpeed) - .5f, .1f);
                            _vehicle.MaxSpeed = _vehicle.GetSpeedUnit(ms);
                        }

                        //If the two vehicles are too close, and facing the same direction, brake the ego vehicle
                        if (hitDist < _emergencyBrakeThresh && dotFront > .8f)
                        {
                            acc = 0;
                            brake = 1;
                            _vehicle.MaxSpeed = Mathf.Max(_vehicle.MaxSpeed / 2f, _vehicle.MinSpeed);
                        }

                        //If the two vehicles are too close, and not facing same direction, slight make the ego vehicle go backward
                        else if (hitDist < _emergencyBrakeThresh && dotFront <= .8f)
                        {
                            acc = -.3f;
                            brake = 0f;
                            _vehicle.MaxSpeed = Mathf.Max(_vehicle.MaxSpeed / 2f, _vehicle.MinSpeed);

                            //Check if the vehicle we are close to is located on the right or left then apply according steering to try to make it move
                            var dotRight = Vector3.Dot(transform.forward, otherVehicle.transform.right);
                            //Right
                            if (dotRight > 0.1f) steering = -.3f;
                            //Left
                            else if (dotRight < -0.1f) steering = .3f;
                            //Middle
                            else steering = -.7f;
                        }

                        //If the two vehicles are getting close, slow down their speed
                        else if (hitDist < _slowDownThresh)
                        {
                            acc = .5f;
                            brake = 0f;
                            //wheelDrive.maxSpeed = Mathf.Max(wheelDrive.maxSpeed / 1.5f, wheelDrive.minSpeed);
                        }
                    }

                    ///////////////////////////////////////////////////////////////////
                    // Generic obstacles
                    else
                    {
                        //Emergency brake if getting too close
                        if (hitDist < _emergencyBrakeThresh)
                        {
                            acc = 0;
                            brake = 1;
                            _vehicle.MaxSpeed = Mathf.Max(_vehicle.MaxSpeed / 2f, _vehicle.MinSpeed);
                        }

                        //Otherwise if getting relatively close decrease speed
                        else if (hitDist < _slowDownThresh)
                        {
                            acc = .5f;
                            brake = 0f;
                        }
                    }
                }

                //Check if we need to steer to follow path
                if (acc > 0f)
                {
                    var desiredVel =
                        _trafficSystem.Segments[_currentTarget.segmentIndex].Waypoints[_currentTarget.waypointIndex]
                            .transform
                            .position - transform.position;
                    steering = Mathf.Clamp(transform.InverseTransformDirection(desiredVel.normalized).x, -1f, 1f);
                }
            }

            //Move the car
            _vehicle.Move(acc, steering, brake);
        }


        private GameObject GetDetectedObstacles(out float hitDistance)
        {
            GameObject detectedObstacle = null;
            var minDist = 1000f;

            var initRay = _raysNumber / 2f * _raySpacing;
            var hitDist = -1f;
            for (var a = -initRay; a <= initRay; a += _raySpacing)
            {
                CastRay(_raycastAnchor.transform.position, a, transform.forward, _raycastLength, out detectedObstacle,
                    out hitDist);

                if (detectedObstacle == null)
                    continue;

                var dist = Vector3.Distance(transform.position, detectedObstacle.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    break;
                }
            }

            hitDistance = hitDist;
            return detectedObstacle;
        }


        private void CastRay(Vector3 anchor, float angle, Vector3 dir, float length, out GameObject outObstacle,
            out float outHitDistance)
        {
            outObstacle = null;
            outHitDistance = -1f;

            //Draw raycast
            Debug.DrawRay(anchor, Quaternion.Euler(0, angle, 0) * dir * length, new Color(1, 0, 0, 0.5f));

            //Detect hit only on the autonomous vehicle layer
            var layer = 1 << LayerMask.NameToLayer("AutonomousVehicle");
            var finalMask = layer;

            foreach (var layerName in _trafficSystem.CollisionLayers)
            {
                var id = 1 << LayerMask.NameToLayer(layerName);
                finalMask = finalMask | id;
            }

            if (Physics.Raycast(anchor, Quaternion.Euler(0, angle, 0) * dir, out var hit, length, finalMask))
            {
                outObstacle = hit.collider.gameObject;
                outHitDistance = hit.distance;
            }
        }

        private int GetNextSegmentId()
        {
            if (_trafficSystem.Segments[_currentTarget.segmentIndex].ConnectedSegments.Count == 0)
                return 0;
            var c = Random.Range(0, _trafficSystem.Segments[_currentTarget.segmentIndex].ConnectedSegments.Count);
            return _trafficSystem.Segments[_currentTarget.segmentIndex].ConnectedSegments[c].Id;
        }

        private void UpdateWaypoint()
        {
            //Find current target
            foreach (var segment in _trafficSystem.Segments)
            {
                if (!segment.IsOnSegment(transform.position))
                    continue;

                _currentTarget.segmentIndex = segment.Id;

                //Find nearest waypoint to start within the segment
                var minDist = float.MaxValue;
                for (var j = 0; j < _trafficSystem.Segments[_currentTarget.segmentIndex].Waypoints.Count; j++)
                {
                    var waypoint = _trafficSystem.Segments[_currentTarget.segmentIndex].Waypoints[j];

                    var distance = Vector3.Distance(transform.position, waypoint.transform.position);

                    //Only take points in front
                    var localSpace = transform.InverseTransformPoint(waypoint.transform.position);
                    if (localSpace.z < 0)
                        continue;

                    if (distance >= minDist)
                        continue;

                    minDist = distance;
                    _currentTarget.waypointIndex = j;
                }

                break;
            }

            //Get future target
            _futureTarget.waypointIndex = _currentTarget.waypointIndex + 1;
            _futureTarget.segmentIndex = _currentTarget.segmentIndex;

            if (_futureTarget.waypointIndex >= _trafficSystem.Segments[_currentTarget.segmentIndex].Waypoints.Count)
            {
                _futureTarget.waypointIndex = 0;
                _futureTarget.segmentIndex = GetNextSegmentId();
            }
        }

        public int GetSegmentVehicleIsIn()
        {
            var vehicleSegment = _currentTarget.segmentIndex;
            var isOnSegment = _trafficSystem.Segments[vehicleSegment].IsOnSegment(transform.position);
            if (!isOnSegment)
            {
                var isOnPSegement = _trafficSystem.Segments[_pastTargetSegment].IsOnSegment(transform.position);
                if (isOnPSegement)
                    vehicleSegment = _pastTargetSegment;
            }

            return vehicleSegment;
        }
    }
}