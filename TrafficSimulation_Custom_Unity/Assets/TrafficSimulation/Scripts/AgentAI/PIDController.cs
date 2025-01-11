using System;
using UnityEngine;

namespace TrafficSimulation
{
    public class PIDController
    {
        public PIDSettings Settings = new PIDSettings();
        
        private float _accumulatedError = 0.0f;
        
        [Serializable]
        public struct PIDSettings
        {
            [Tooltip("Proportional acts as a spring, pulling the car back to the path")]
            public float ProportionalGain;
            
            [Tooltip("Integral acts as a memory, reducing the steady state error")]
            public float IntegralGain;

            [Tooltip("Derivative acts as a damper, reducing the oscillation of the car")]
            public float DerivativeGain;
            
            [Tooltip("Integral saturation limit, to prevent windup")]
            public float Integral_Limit;
        }
        
        public struct PIDResult
        {
            public float Proportional;
            public float Integral;
            public float Derivative;
            public float Total;
        }
        
        public PIDController(PIDSettings settings)
        {
            Settings = settings;
        }

        public void Reset()
        {
            _accumulatedError = 0.0f;
        }
        public PIDResult Evaluate(float error, float errorRate, float deltaTime)
        {
            float p = Settings.ProportionalGain * error;
            float d = Settings.DerivativeGain * errorRate;
            
            _accumulatedError += error * Settings.IntegralGain * deltaTime;
            _accumulatedError = Mathf.Clamp(_accumulatedError, -Settings.Integral_Limit, Settings.Integral_Limit);
            float i = _accumulatedError;
            
            return new PIDResult
            {
                Proportional = p,
                Integral = i,
                Derivative = d,
                Total = p + i + d
            };
        }
    }
}