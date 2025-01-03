using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace TrafficSimulation
{
    [DefaultExecutionOrder(-1)]
    public class TrafficAgentPower : MonoBehaviour
    {
        [SerializeField] private TrafficAgent _agent;
        
        [SerializeField] private float _lookAheadDistance = 10.0f;
        [SerializeField] private bool _forceSpeedAtSpeedLimit = false;
        
        [FormerlySerializedAs("_defaultStoppingDIstance")] [SerializeField] private float _defaultStoppingDistance = 2.0f;
        [SerializeField] private float _stoppingDistanceVehicleLengthMultiplier = 0.5f;
        

        private Vector3 _stopPoint;
        private PIDController _stopPointPIDController;

        private float _stopPointInput = 0.0f;
        private float _speedLimitInput = 0.0f;

        [DebugGUIGraph(min: -1, max: 1, r: 0, g: 0, b: 0, autoScale: false, group: 2)]
        private float result;


        private void Start()
        {
            if(_agent == null)
                _agent = GetComponent<TrafficAgent>();

            // Force speed limit
            if (_forceSpeedAtSpeedLimit)
                _agent.CarBehaviour.ForceSpeed(_agent.CurrentSample.SpeedLimit);
            
            _stopPointPIDController = new PIDController(_agent.Settings.PositionSpeedPIDSettings);
        }
        
        private void Update()
        {
            UpdateStopPointInput();
            UpdateSpeedLimitInput();
            ApplyInput();
        }

        private void ApplyInput()
        {
            // Blend stop point input and speed limit input
            // Always pick the lowest throttle input
            float combinedInput = Mathf.Clamp01(Mathf.Min(_speedLimitInput, _stopPointInput)) - Mathf.Clamp01(Mathf.Max(-_speedLimitInput, -_stopPointInput));

            result = _agent.CarBehaviour.CombinedInput = combinedInput;
        }

        private void UpdateSpeedLimitInput()
        {
            // Calculate the desire input to reach the target speedLimit
            float speedAlongDirection = Vector3.Dot(_agent.CarBehaviour.Velocity, _agent.CurrentSample.DirectionForward);
            float gain = speedAlongDirection > _agent.CurrentSample.SpeedLimit ? _agent.Settings.SpeedLimitBrakeProportionalGain : _agent.Settings.SpeedLimitThrottleProportionalGain;
            float targetSpeedLimitInput = (_agent.CurrentSample.SpeedLimit - speedAlongDirection) * gain;
            _speedLimitInput = Mathf.MoveTowards(_speedLimitInput, targetSpeedLimitInput, _agent.Settings.SpeedLimitInputMaxCangeRate * Time.deltaTime);
        }

        float MoveTowards(float current, float target, float maxDelta) 
        {
            // Define the change
            float change = target - current;
            
            // When under maxDelta, return target
            if (Mathf.Abs(change) - maxDelta < 0) 
                return target;
            
            // Otherwise, move towards target clamped by maxDelta
            return current + Mathf.Sign(change) * maxDelta;
        }
        
        private void UpdateStopPointInput()
        {
            // float stopPosition = _lookAheadDistance + _agent.CurrentSample.DistanceAlongSegment;
            // Segment.Sample stopPointSample = _agent.CurrentSegment.SampleFromDistance(stopPosition);
            // _stopPoint = stopPointSample.Position;
            
            
            // _stopPoint = _agent.transform.position + _agent.transform.forward * _lookAheadDistance;


            if (_agent.FrontSensorHit.collider == null)
            {
                // We want full throttle when there is nothing to stop for
                _stopPointInput = 1.0f;
                return;
            }


  
            
            float stoppingDistanceOffset = _agent.AgentSize.z * _stoppingDistanceVehicleLengthMultiplier + _defaultStoppingDistance;
            
            // // Move stopping distance based on hit agent speed
            // if (_agent.FrontSensorHit.collider != null)
            // {
            //     Rigidbody hitRigidbody = _agent.FrontSensorHit.collider.attachedRigidbody;
            //     if (hitRigidbody != null)
            //         stoppingDistanceOffset -= Vector3.Dot(hitRigidbody.linearVelocity, _agent.CarBehaviour.ForwardPlanner);
            // }
            
            _stopPoint = _agent.CarBehaviour.Position + _agent.CarBehaviour.ForwardPlanner * (_agent.FrontSensorHit.distance - stoppingDistanceOffset);
            
            // Calculate the input to reach the stop point
            Vector3 fromPosition = new Vector3(_agent.CarBehaviour.Position.x, 0.0f, _agent.CarBehaviour.Position.z);
            Vector3 toPosition = new Vector3(_stopPoint.x, 0.0f, _stopPoint.z);
            Vector3 directionToStopPoint = (toPosition - fromPosition).normalized;
            
            // Calculate the error
            float directionRelativeToAgentForward = Vector3.Dot(_agent.CarBehaviour.ForwardPlanner, directionToStopPoint);
            float distanceToStopPoint = Vector3.Distance(MathExtensions.FlatVector(_agent.CarBehaviour.Position), MathExtensions.FlatVector(_stopPoint));
            float signedDistanceToStopPoint = distanceToStopPoint * Mathf.Sign(directionRelativeToAgentForward);
            float error = signedDistanceToStopPoint;


            /// TEMP SOLUTION 
            if (error < 0.0f && _agent.CarBehaviour.ForwardSpeed > 0.0f)
            {
                _stopPointInput = -1.0f;
                return;
            }
            
            // Calculate the error rate
            float velocityAlongDirectionToStopPoint = Vector3.Dot(_agent.CarBehaviour.Velocity, directionToStopPoint);
            float errorRate = -velocityAlongDirectionToStopPoint;
            
            
            // Evaluate the PID controller
            _stopPointInput = _stopPointPIDController.Evaluate(error, errorRate, Time.deltaTime).Total;
        }

        private void OnDrawGizmos()
        {
            if(_agent.FrontSensorHit.collider == null)
                return;
            
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_stopPoint, 1.0f);
            Gizmos.DrawLine(_agent.CarBehaviour.Position, _stopPoint);
        }
    }
}

