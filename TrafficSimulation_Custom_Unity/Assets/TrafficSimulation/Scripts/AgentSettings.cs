using TrafficSimulation;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "AgentSettings", menuName = "Scriptable Objects/AgentSettings")]
public class AgentSettings : ScriptableObject
{
    [Header("Speed")]
    public float MaxSpeed = 10.0f;

    [Header("Steering")]
    public PIDController.PIDSettings SteeringPIDSettings;
    public float DirectionError_Gain = 5.0f;
    public float DirectionErrorTriggerDistance = 5.0f;
    
    [Header("Lookahead")]
    public float LookaheadDistance = 5.0f;
    public float LookaheadDistanceOverTime = 0.5f;
}
