using System;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace TrafficSimulation
{

    public class SteerTesting : MonoBehaviour
    {
        [SerializeField] private Segment _segment; 
        [SerializeField] private CarBehaviour _agent;
        [SerializeField] private AgentSettings _settings;

        private float integrationStored = 0.0f;
        
        private void Update()
        {
            _agent.ThrottleInput = _agent.ForwardSpeed < _settings.MaxSpeed ? 1.0f : 0.0f;
            

            var sample = _segment.GetSampleFromPosition(_agent.Position);
            float error = sample.SignedDistanceFromPath;
            
            
            // P - Proportional acts as a spring, pulling the car back to the path 
            float p = _settings.Proportional_Gain * error;
            
            // D - Derivative acts as a damper, reducing the oscillation of the car
            float errorRate = Vector3.Dot(_agent.Velocity, sample.DirectionRight);
            float d = _settings.Derivative_Gain * errorRate;
            
            // I - Integral acts as a memory, reducing the steady state error
            integrationStored += error * Time.deltaTime;
            integrationStored = Mathf.Clamp(integrationStored, -_settings.Integral_Limit,_settings.Integral_Limit);
            float i = _settings.Integral_Gain * integrationStored;
            

            float scale = 10.0f;

            Vector3 from = _agent.Position + Vector3.up;
            // Debug.DrawRay(from, sample.DirectionRight * p * scale, Color.red);
            // Debug.DrawRay(from + _agent.Forward * -0.5f, sample.DirectionRight * i * scale, Color.blue);
            // Debug.DrawRay(from + _agent.Forward * 0.5f, sample.DirectionRight * d * scale, Color.green);

            _agent.SteerWheelInput = p + i + d;


        }
    }
}