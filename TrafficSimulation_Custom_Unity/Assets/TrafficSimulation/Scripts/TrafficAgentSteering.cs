using UnityEngine;

namespace TrafficSimulation
{
    
    [RequireComponent( typeof(TrafficAgent))]
    public class TrafficAgentSteering : MonoBehaviour
    {
        private TrafficAgent _agent;
        
        private SteerModeType _steerMode;
        private PIDController _steeringPID;

        private Segment.Sample _interpolatedSample;
        private Segment.Sample _futureSample;
        
        public SteerModeType SteerMode => _steerMode;

        public float Lookahead => _agent.Settings.PathSmoothingDefaultDistance + _agent.CarBehaviour.ForwardSpeed * _agent.Settings.PathSmoothingDistanceOverSpeed;
        
        public enum SteerModeType
        {
            PID,
            DirectionCorrection,
            DistanceCorrection,
        }

        private void Start()
        {
            _agent = GetComponent<TrafficAgent>();
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
            if (Lookahead > 0.0f)
            {
                // Set future
                _futureSample =
                    _agent.SampleFromDistanceExtended(_agent.CurrentExtendedSample.DistanceAlongSegment + Lookahead);
                
                // Interpolate
                _interpolatedSample.Position =
                    Vector3.Lerp(_agent.CurrentExtendedSample.Position, _futureSample.Position, 0.5f);
                _interpolatedSample.DirectionForward = (_futureSample.Position - _agent.CurrentExtendedSample.Position).normalized;
                _interpolatedSample.DirectionRight = Vector3.Cross(_interpolatedSample.DirectionForward, Vector3.up);
                _interpolatedSample.AlphaAlongSegment = Mathf.Lerp(_agent.CurrentExtendedSample.AlphaAlongSegment,
                    _futureSample.AlphaAlongSegment, 0.5f);
                _interpolatedSample.DistanceAlongSegment = Mathf.Lerp(_agent.CurrentExtendedSample.DistanceAlongSegment,
                    _futureSample.DistanceAlongSegment, 0.5f);
            }
            else
            {
                _interpolatedSample = _agent.CurrentExtendedSample;
            }

            // Decide agent steering mode
            bool isDrivingBackwards = Vector3.Dot(_agent.CarBehaviour.Forward,_interpolatedSample.DirectionForward) < 0;
            bool isTooFarFromPath = _interpolatedSample.GetSidewaysDistanceFromPath(_agent.CarBehaviour.Position) > _agent.Settings.InstabilityTriggerDistance;
            if (isTooFarFromPath && _agent.Settings.UseDistanceCorrection)
                _steerMode = SteerModeType.DistanceCorrection;
            else if (isDrivingBackwards && _agent.Settings.UseBackwardsCorrection)
                _steerMode = SteerModeType.DirectionCorrection;
            else
                _steerMode = SteerModeType.PID;

            // TODO: This is temporary for the intersection
            if (_agent.CurrentExtendedSample.IsAtStartOfSegment || _agent.CurrentExtendedSample.IsAtEndOfSegment)
                _steerMode = SteerModeType.DirectionCorrection;


            switch (_steerMode)
            {
                case SteerModeType.PID:
                    
                    float signedDistanceFromPath = _interpolatedSample.GetSidewaysSignedDistanceFromPath(_agent.CarBehaviour.Position);
                    float error = -signedDistanceFromPath;
                    float errorRate = Vector3.Dot(_agent.CarBehaviour.Velocity, _interpolatedSample.DirectionRight);
                    
                    var pidResult =  _steeringPID.Evaluate(error, errorRate, Time.deltaTime);
                    
                    _agent.CarBehaviour.SteerWheelInput = pidResult.Total;
                    
                    if (_agent.Settings.DebugSteeringPID)
                    {
                        float scale = 10.0f;
                        Vector3 from = _agent.CarBehaviour.Position + Vector3.up;
                        Debug.DrawRay(from, _interpolatedSample.DirectionRight * (pidResult.Proportional * scale), Color.red);
                        Debug.DrawRay(from + _agent.CarBehaviour.Forward * -0.5f, _interpolatedSample.DirectionRight * (pidResult.Integral * scale), Color.blue);
                        Debug.DrawRay(from + _agent.CarBehaviour.Forward * 0.5f, _interpolatedSample.DirectionRight * (pidResult.Derivative * scale), Color.green);
                    }
                    break;
                
                case SteerModeType.DirectionCorrection:
                    _agent.CarBehaviour.SteerWheelInput = GetSteerInputToDirection(_interpolatedSample.DirectionForward);
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
            if(Application.isPlaying == false)
                return;

            if (_agent.Settings.DebugPathSmoothing)
            {
                Gizmos.color = _agent.Settings.DebugPathSmoothingFromColor;
                Gizmos.DrawWireSphere(_agent.CurrentExtendedSample.Position, _agent.Settings.DebugPathSmoothingBallRadius * 0.5f);

                if (Lookahead > 0.0f)
                {
                    Gizmos.color = _agent.Settings.DebugPathSmoothingInterpolatedColor;
                    Gizmos.DrawWireSphere(_interpolatedSample.Position, _agent.Settings.DebugPathSmoothingBallRadius);
                    float distance = Vector3.Distance(_agent.CurrentExtendedSample.Position, _futureSample.Position);
                    Gizmos.DrawLine(
                        _interpolatedSample.Position - _interpolatedSample.DirectionForward * distance * 0.5f,
                        _interpolatedSample.Position + _interpolatedSample.DirectionForward * distance * 0.5f);


                    Gizmos.color = _agent.Settings.DebugPathSmoothingToColor;
                    Gizmos.DrawWireSphere(_futureSample.Position, _agent.Settings.DebugPathSmoothingBallRadius);
                }
            }
        }

        private float GetSteerInputToDirection(Vector3 direction)
        {
            Vector2 directionToPath = new Vector2(direction.x, direction.z).normalized;
            Vector2 agentDirection = new Vector2(_agent.CarBehaviour.Forward.x, _agent.CarBehaviour.Forward.z).normalized;
            float angleError =  Vector2.SignedAngle(directionToPath, agentDirection) / 360.0f;
            return angleError * _agent.Settings.DirectionSteeringProportionalGain;
        }
    }
}