using UnityEngine;
using UnityEngine.Serialization;

namespace TrafficSimulation
{
    
    [DefaultExecutionOrder(-1)]
    public class TrafficAgentSteering : MonoBehaviour
    {
        [SerializeField] private TrafficAgent _agent;
        
        
        [SerializeField] private bool useDistanceCorrection = true;
        [SerializeField] private bool useBackwardsCorrection = true;
        
        [Header("Debug")]
        [SerializeField] private bool debug = false;
        [SerializeField] float debugBallRadius = 1.0f;

        
        private SteerModeType _steerMode;
        private PIDController _steeringPID;

                    
        private Segment.Sample _interpolatedSample;
        private Segment.Sample _futureSample;
        
        public SteerModeType SteerMode => _steerMode;

        
        public float LookaheadDistance => _agent.CarBehaviour.ForwardSpeed * _agent.Settings.LookaheadSpeedDistance_Gain + _agent.Settings.LookaheadDistance;
        
        public enum SteerModeType
        {
            PID,
            BackwardsCorrection,
            DistanceCorrection
        }

        private void Start()
        {
            _steeringPID = new PIDController(_agent.Settings.SteeringPIDSettings);
        }

        private void Update()
        {
            if (_agent.CurrentSegment == null)
            {
                _agent.CarBehaviour.SteerWheelInput = 0;
                return;
            }

            
            // Interpolate between current and future sample
            if (LookaheadDistance > 0.0f)
            {
         
                _futureSample =
                    _agent.CurrentSegment.SampleFromDistance(_agent.CurrentSample.DistanceAlongSegment + LookaheadDistance);
                _interpolatedSample.Position =
                    Vector3.Lerp(_agent.CurrentSample.Position, _futureSample.Position, 0.5f);
                _interpolatedSample.Direction = (_futureSample.Position - _agent.CurrentSample.Position).normalized;
                _interpolatedSample.DirectionRight = Vector3.Cross(_interpolatedSample.Direction, Vector3.up);
                _interpolatedSample.AlphaAlongSegment = Mathf.Lerp(_agent.CurrentSample.AlphaAlongSegment,
                    _futureSample.AlphaAlongSegment, 0.5f);
                _interpolatedSample.DistanceAlongSegment = Mathf.Lerp(_agent.CurrentSample.DistanceAlongSegment,
                    _futureSample.DistanceAlongSegment, 0.5f);
            }
            else
            {
                _interpolatedSample = _agent.CurrentSample;
            }

            // Decide agent steering mode
            bool isDrivingBackwards = Vector3.Dot(_agent.CarBehaviour.Forward,_interpolatedSample.Direction) < 0;
            bool isTooFarFromPath = _interpolatedSample.GetDistanceFromPath(_agent.CarBehaviour.Position) > _agent.Settings.DirectionErrorTriggerDistance;
            if (isTooFarFromPath && useDistanceCorrection)
                _steerMode = SteerModeType.DistanceCorrection;
            else if (isDrivingBackwards && useBackwardsCorrection)
                _steerMode = SteerModeType.BackwardsCorrection;
            else
                _steerMode = SteerModeType.PID;

            

            switch (_steerMode)
            {
                case SteerModeType.PID:
                    
                    float signedDistanceFromPath = _interpolatedSample.GetSignedDistanceFromPath(_agent.CarBehaviour.Position);
                    float error = -signedDistanceFromPath;
                    float errorRate = Vector3.Dot(_agent.CarBehaviour.Velocity, _interpolatedSample.DirectionRight);
                    
                    var pidResult =  _steeringPID.Evaluate(error, errorRate, Time.deltaTime);
                    
                    _agent.CarBehaviour.SteerWheelInput = pidResult.Total;
                    
                    if (debug)
                    {
                        float scale = 10.0f;
                        Vector3 from = _agent.CarBehaviour.Position + Vector3.up;
                        Debug.DrawRay(from, _interpolatedSample.DirectionRight * (pidResult.Proportional * scale), Color.red);
                        Debug.DrawRay(from + _agent.CarBehaviour.Forward * -0.5f, _interpolatedSample.DirectionRight * (pidResult.Integral * scale), Color.blue);
                        Debug.DrawRay(from + _agent.CarBehaviour.Forward * 0.5f, _interpolatedSample.DirectionRight * (pidResult.Derivative * scale), Color.green);
                    }
                    
                    break;
                
                case SteerModeType.BackwardsCorrection:
                    _agent.CarBehaviour.SteerWheelInput = GetSteerInputToDirection(_interpolatedSample.Direction);
                    break;
                
                case SteerModeType.DistanceCorrection:
                    var directionToSampledPosition = _interpolatedSample.GetDirectionToSampledPosition(_agent.CarBehaviour.Position);
                    _agent.CarBehaviour.SteerWheelInput = GetSteerInputToDirection(directionToSampledPosition);
                    break;
            }
            
            // Make sure we clear the error when we are not using PID
            if(_steerMode != SteerModeType.PID)
                _steeringPID.Reset();
        }

        private void OnDrawGizmos()
        {
            if(Application.isPlaying == false || debug == false)
                return;

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_agent.CurrentSample.Position,debugBallRadius);

            if (LookaheadDistance > 0.0f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(_interpolatedSample.Position, debugBallRadius);

                float distance = Vector3.Distance(_agent.CurrentSample.Position, _futureSample.Position);
                Gizmos.DrawLine(_interpolatedSample.Position - _interpolatedSample.Direction * distance * 0.5f,
                    _interpolatedSample.Position + _interpolatedSample.Direction * distance * 0.5f);
                
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_futureSample.Position, debugBallRadius);
            }
        }

        private float GetSteerInputToDirection(Vector3 direction)
        {
            Vector2 directionToPath = new Vector2(direction.x, direction.z).normalized;
            Vector2 agentDirection = new Vector2(_agent.CarBehaviour.Forward.x, _agent.CarBehaviour.Forward.z).normalized;
            float angleError =  Vector2.SignedAngle(directionToPath, agentDirection) / 360.0f;
            return angleError * _agent.Settings.DirectionError_Gain;
        }
    }
}