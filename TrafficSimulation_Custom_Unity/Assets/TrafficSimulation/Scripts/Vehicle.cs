using System;
using UnityEngine;

namespace TrafficSimulation.Scripts
{
    [Serializable]
    public enum DriveType
    {
        RearWheelDrive,
        FrontWheelDrive,
        AllWheelDrive
    }

    [Serializable]
    public enum UnitType
    {
        KMH,
        MPH
    }

    public class Vehicle : MonoBehaviour
    {
        [Tooltip("Downforce applied to the vehicle")]
        public float DownForce = 100f;

        [Tooltip("Maximum steering angle of the wheels")]
        public float MaxAngle = 70f;

        [Tooltip("Speed at which we will reach the above steering angle (lerp)")]
        public float SteeringLerp = 10f;

        [Tooltip("Max speed (in unit chosen below) when the vehicle is about to steer")]
        public float SteeringSpeedMax = 20f;

        [Tooltip("Maximum torque applied to the driving wheels")]
        public float MaxTorque = 300f;

        [Tooltip("Maximum brake torque applied to the driving wheels")]
        public float BrakeTorque = 100000f;

        [Tooltip("Unit Type")] public UnitType UnitType;

        [Tooltip("Min Speed - when driving (not including stops/brakes), in the unit chosen above. Should be > 0.")]
        public float MinSpeed = 2;

        [Tooltip("Max Speed in the unit chosen above")]
        public float MaxSpeed = 13;

        [Tooltip("Drag the wheel shape here.")]
        public GameObject LeftWheelShape;

        public GameObject RightWheelShape;

        [Tooltip("Whether you want to animate the wheels")]
        public bool AnimateWheels = true;

        [Tooltip("The vehicle's drive type: rear-wheels drive, front-wheels drive or all-wheels drive.")]
        public DriveType DriveType;

        private float _currentSteering;

        private Rigidbody _rigidbody;

        private WheelCollider[] _wheels;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _wheels = GetComponentsInChildren<WheelCollider>();

            foreach (var wheel in _wheels)
            {
                // Create wheel shapes only when needed.
                if (LeftWheelShape != null && wheel.transform.localPosition.x < 0)
                    Instantiate(LeftWheelShape, wheel.transform, true);
                else if (RightWheelShape != null && wheel.transform.localPosition.x > 0)
                    Instantiate(RightWheelShape, wheel.transform, true);

                wheel.ConfigureVehicleSubsteps(10, 1, 1);
            }
        }


        public void Move(float acceleration, float steering, float brake)
        {
            var nSteering = Mathf.Lerp(_currentSteering, steering, Time.deltaTime * SteeringLerp);
            _currentSteering = nSteering;

            var angle = MaxAngle * nSteering;
            var torque = MaxTorque * acceleration;

            var handBrake = brake > 0 ? BrakeTorque : 0;

            foreach (var wheel in _wheels)
            {
                // Steer front wheels only
                if (wheel.transform.localPosition.z > 0)
                    wheel.steerAngle = angle;

                if (wheel.transform.localPosition.z < 0)
                    wheel.brakeTorque = handBrake;

                if (wheel.transform.localPosition.z < 0 && DriveType != DriveType.FrontWheelDrive)
                    wheel.motorTorque = torque;

                if (wheel.transform.localPosition.z >= 0 && DriveType != DriveType.RearWheelDrive)
                    wheel.motorTorque = torque;


                if (AnimateWheels)
                {
                    Quaternion q;
                    Vector3 p;
                    wheel.GetWorldPose(out p, out q);

                    var shapeTransform = wheel.transform.GetChild(0);
                    shapeTransform.position = p;
                    shapeTransform.rotation = q;
                }
            }


            //Apply speed
            var s = GetSpeedUnit(_rigidbody.linearVelocity.magnitude);
            if (s > MaxSpeed) _rigidbody.linearVelocity = GetSpeedMS(MaxSpeed) * _rigidbody.linearVelocity.normalized;


            //Apply downforce
            _rigidbody.AddForce(-transform.up * DownForce * _rigidbody.linearVelocity.magnitude);
        }

        public float GetSpeedMS(float s)
        {
            return UnitType == UnitType.KMH ? s / 3.6f : s / 2.237f;
        }

        public float GetSpeedUnit(float s)
        {
            return UnitType == UnitType.KMH ? s * 3.6f : s * 2.237f;
        }

        public float GetSpeed()
        {
            return _rigidbody.linearVelocity.magnitude;
        }
    }
}