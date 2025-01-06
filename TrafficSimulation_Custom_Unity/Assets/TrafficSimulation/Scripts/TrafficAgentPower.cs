using UnityEngine;

namespace TrafficSimulation
{
    [RequireComponent( typeof(TrafficAgent))]
    public class TrafficAgentPower : MonoBehaviour
    {
        private TrafficAgent _agent;
        
        [SerializeField] private bool _startAtSpeedLimitSpeed = false;

        private float _stopPointDistance;
        private PIDController _stopPointPIDController;

        
        [DebugGUIGraph(min: -1, max: 1, r: 0, g: 0, b: 0, autoScale: false, group: 2)]
        private float _finalInput;
        
        [DebugGUIGraph(min: -1, max: 1, r: 0, g: 0, b: 0, autoScale: false, group: 3)]
        private float _speedLimitInput = 0.0f;
        
        [DebugGUIGraph(min: -1, max: 1, r: 0, g: 0, b: 0, autoScale: false, group: 4)]
        private float _stopPointInput = 0.0f;
        

        private void Start()
        {
            _agent = GetComponent<TrafficAgent>();

            // Force speed limit
            if (_startAtSpeedLimitSpeed)
                _agent.CarBehaviour.ForceSpeed(_agent.CurrentSample.SpeedLimit);
            
            _stopPointPIDController = new PIDController(_agent.Settings.StopPointPIDSettings);
        }
        
        private void Update()
        {
            if(_agent.CurrentSegment == null)
                return;
            
            UpdateStopPointInput();
            UpdateSpeedLimitInput();
            ApplyInput();
        }

        private void ApplyInput()
        {
            // Blend stop point input and speed limit input
            // Always pick the lowest throttle input
            float combinedInput = Mathf.Clamp01(Mathf.Min(_speedLimitInput, _stopPointInput)) - Mathf.Clamp01(Mathf.Max(-_speedLimitInput, -_stopPointInput));

            _agent.CarBehaviour.CombinedInput = combinedInput;
            _finalInput = combinedInput;
        }

        private void UpdateSpeedLimitInput()
        {
            // Calculate the desire input to reach the target speedLimit
            float speedAlongDirection = Vector3.Dot(_agent.CarBehaviour.Velocity, _agent.CurrentSample.DirectionForward);
            float gain = (speedAlongDirection > _agent.CurrentSample.SpeedLimit ? _agent.Settings.SpeedLimitBrakeProportionalGain : _agent.Settings.SpeedLimitThrottleProportionalGain);
            float targetSpeedLimitInput = (_agent.CurrentSample.SpeedLimit - speedAlongDirection) * gain;
            _speedLimitInput = Mathf.MoveTowards(_speedLimitInput, targetSpeedLimitInput, _agent.Settings.SpeedLimitInputMaxCangeRate * Time.deltaTime);
        }
        
        
        private void UpdateStopPointInput()
        {
            float sensorStoppingDistanceOffset = _agent.AgentSize.z * _agent.Settings.StoppingDistanceVehicleLengthMultiplier + _agent.Settings.DefaultStoppingDistance;
            float sensorStopDistance = _agent.FrontSensorResult.Distance - sensorStoppingDistanceOffset;
            
            float segmentStopDistance = _agent.CurrentSample.DistanceToSegmentEnd - _agent.AgentSize.z * 0.5f;
            
            // Igunore the segment stop distance if the agent is moving
            if(_agent.IntersectionState != TrafficAgent.IntersectionStateType.Waiting)
                segmentStopDistance = float.MaxValue;
            
            
            _stopPointDistance = Mathf.Min(sensorStopDistance, segmentStopDistance);
            
            // Calculate the error
            float error = _stopPointDistance;
            float errorRate = -_agent.CarBehaviour.ForwardSpeed;
            
            // Evaluate the PID controller
            _stopPointInput = _stopPointPIDController.Evaluate(error, errorRate, Time.deltaTime).Total;
        }

        private void OnDrawGizmos()
        {
            if(Application.isPlaying == false)
                return;

            if (_agent.Settings.DebugStopPoint)
            {
                Vector3 stopPoint = _agent.CarBehaviour.Position + _agent.CarBehaviour.Forward * _stopPointDistance;
                Gizmos.color = _agent.Settings.DebugStopPointColor;
                Gizmos.DrawSphere(stopPoint, 1.0f);
                Gizmos.DrawLine(_agent.CarBehaviour.Position, stopPoint);
            }
        }
    }
}

