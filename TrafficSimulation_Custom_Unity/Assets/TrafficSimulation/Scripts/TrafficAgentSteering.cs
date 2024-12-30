using UnityEngine;

namespace TrafficSimulation
{
    
    [DefaultExecutionOrder(-1)]
    public class TrafficAgentSteering : MonoBehaviour
    {
        [SerializeField] private TrafficAgent _agent;
        
        private float accumulatedError = 0.0f;
        
        [SerializeField] private bool useDistanceCorrection = true;
        [SerializeField] private bool useBackwardsCorrection = true;
        [SerializeField] private bool debugPID = false;
        
        private SteerModeType _steerMode;

        public SteerModeType SteerMode => _steerMode;

        
        public enum SteerModeType
        {
            PID,
            BackwardsCorrection,
            DistanceCorrection
        }
        
        private void Update()
        {
            if (_agent.CurrentSegment == null)
            {
                _agent.CarBehaviour.SteerWheelInput = 0;
                return;
            }
            
            // Sample the path
            var pathSample = _agent.CurrentSample;
    
            // Decide agent steering mode
            bool isDrivingBackwards = Vector3.Dot(_agent.CarBehaviour.Forward, pathSample.Direction) < 0;
            bool isTooFarFromPath = pathSample.DistanceFromPath > _agent.Settings.DirectionError_Distance;

            if (isTooFarFromPath && useDistanceCorrection)
                _steerMode = SteerModeType.DistanceCorrection;
            else if (isDrivingBackwards && useBackwardsCorrection)
                _steerMode = SteerModeType.BackwardsCorrection;
            else
                _steerMode = SteerModeType.PID;

            switch (_steerMode)
            {
                case SteerModeType.PID:
                    float error = -pathSample.SignedDistanceFromPath;
                    float p = _agent.Settings.Proportional_Gain * error;

                    float errorRate = Vector3.Dot(_agent.CarBehaviour.Velocity, pathSample.DirectionRight);
                    float d = _agent.Settings.Derivative_Gain * errorRate;

                    accumulatedError += error * Time.deltaTime;
                    accumulatedError = Mathf.Clamp(accumulatedError, -_agent.Settings.Integral_Limit,
                        _agent.Settings.Integral_Limit);
                    float i = _agent.Settings.Integral_Gain * accumulatedError;

                    float pidSteerCorrection = p + i + d;
                    
                    _agent.CarBehaviour.SteerWheelInput = pidSteerCorrection;
                    
                    if (debugPID)
                    {
                        float scale = 10.0f;
                        Vector3 from = _agent.CarBehaviour.Position + Vector3.up;
                        Debug.DrawRay(from, pathSample.DirectionRight * p * scale, Color.red);
                        Debug.DrawRay(from + _agent.CarBehaviour.Forward * -0.5f, pathSample.DirectionRight * i * scale, Color.blue);
                        Debug.DrawRay(from + _agent.CarBehaviour.Forward * 0.5f, pathSample.DirectionRight * d * scale, Color.green);
                    }
                    break;
                
                case SteerModeType.BackwardsCorrection:
                    _agent.CarBehaviour.SteerWheelInput = GetSteerInputToDirection(pathSample.Direction);
                    break;
                
                case SteerModeType.DistanceCorrection:
                    _agent.CarBehaviour.SteerWheelInput = GetSteerInputToDirection(pathSample.DirectionToSampledPosition);
                    break;
            }
            
            // Make sure we clear the error when we are not using PID
            if(_steerMode != SteerModeType.PID)
                accumulatedError = 0.0f;
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