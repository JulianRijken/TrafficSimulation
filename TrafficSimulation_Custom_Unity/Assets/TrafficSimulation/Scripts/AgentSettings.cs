using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "AgentSettings", menuName = "Scriptable Objects/AgentSettings")]
public class AgentSettings : ScriptableObject
{
    [Header("Speed")]
    public float MaxSpeed = 10.0f;

    [FormerlySerializedAs("P_Gain")]
    [Header("Steering")]
    
    [Tooltip("Proportional acts as a spring, pulling the car back to the path")]
    public float Proportional_Gain = 1.0f;
    [Tooltip("Derivative acts as a damper, reducing the oscillation of the car")]
    public float Derivative_Gain = 1.0f;
    [Tooltip("Integral acts as a memory, reducing the steady state error")]
    public float Integral_Gain = 1.0f;
    [FormerlySerializedAs("Integral_Saturation")]
    [Tooltip("Integral saturation limit, to prevent windup")]
    public float Integral_Limit = 1.0f;
    [Space]
    public float DirectionError_Gain = 1.0f;
    public float DirectionError_Distance = 3.0f;

    // public AnimationCurve SteeringDirectionErrorStrengthOverDistanceCurve;
}
