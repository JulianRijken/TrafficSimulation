using TrafficSimulation;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "AgentSettings", menuName = "Scriptable Objects/AgentSettings")]
public class AgentSettings : ScriptableObject
{
    [Header("Steering")]
    public PIDController.PIDSettings SteeringPIDSettings;

    [Header("Steering Smoothing")]
    public float PathSmoothingDefaultDistance = 5.0f;
    public float PathSmoothingDistanceOverSpeed = 0.15f;
    
    [Header("Steering Correction")]
    public bool UseDistanceCorrection = true;
    public bool UseBackwardsCorrection = true;
    public float DirectionSteeringPoportionalGain = 5.0f;
    public float InstabilityTriggerDistance = 5.0f;
    
    [Header("Power Stop Point")]
    public PIDController.PIDSettings StopPointPIDSettings; //TODO: Should have a separate one for the break and throttle
    public float DefaultStoppingDistance = 2.0f;
    public float StoppingDistanceVehicleLengthMultiplier = 0.5f;

    [Header("Power Speed Limit")]
    public float SpeedLimitInputMaxCangeRate = 0.1f;
    public float SpeedLimitThrottleProportionalGain = 0.15f;
    public float SpeedLimitBrakeProportionalGain = 0.10f;
    
    [Header("Collision Avoidance")]
    public float FrontSensorDistance = 5f;
    public float FrontSensorAnglePositionOffset = 0.025f;

    
    [Header("Debug")]
    public bool DebugAgentSize = false;
    public bool DebugCurrentSample = false;
    public bool DebugClosestSample = false;
    public bool DebugAllSamples = false;
    public bool DebugSpeed = false;
    public bool DebugPathSmoothing = false;
    public bool DebugSteeringPID = false;
    public bool DebugStopPoint = false;
    public bool DebugSpeedLimit = false;
    
    [Header("Debug Colors")]
    public Color DebugAgentSizeWireColor = Color.red;
    public Color DebugAgentSizeFillColor = Color.clear;
    public Color DebugCurrentSampleColor = Color.green;
    public Color DebugClosestSampleColor = Color.blue;
    public Color DebugSpeedSphereColor = new Color(1, 0.1f, 0.1f, 0.3f);
    public Color DebugSpeedCircleColor = Color.black;
    public Color DebugPathSmoothingFromColor = Color.blue;
    public Color DebugPathSmoothingToColor = Color.cyan;
    public Color DebugPathSmoothingInterpolatedColor = Color.yellow;
    public Color DebugStopPointColor = Color.red;
    
    [Header("Debug Sizes")]
    public float DebugPathSmoothingBallRadius = 0.5f;
}
