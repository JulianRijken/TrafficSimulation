using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrafficSimulation
{
    public class CarBehaviour : MonoBehaviour
    {
        [Header("Wheels")] 
        [SerializeField] private List<CarWheel> _steerWheels = new();
        [SerializeField] private List<CarWheel> _powerWheels = new();
        [SerializeField] private List<CarWheel> _breakingWheels = new();
        [SerializeField] private List<CarWheel> _backWheels = new();

        [Header("Steering")]
        [SerializeField] private float _steerSpeed = 10.0f;
        [SerializeField] private AnimationCurve _steerOverSpeed;

        [Header("Engine")] 
        [SerializeField] [Range(0.0f, 30000.0f)] private float _torqueScale = 10000.0f;
        [SerializeField] private float _topForwardsKPH = 80.0f;
        [SerializeField] private float _topBackwardsKPH = 30.0f;
        [SerializeField] private float _engineBreakingForce = 0.1f;
        [SerializeField] private AnimationCurve _torqueOverSpeed;

        [Header("Drag")]
        [SerializeField] private float _maxDrag = 1.0f;
        [SerializeField] private AnimationCurve _dragCurve;
        
        [Header("Debug")]
        [SerializeField] private bool _debug = false;
        

        private Rigidbody _rigidbody;
        private float _breakInput; // 0 to 1 - Less to More
        private float _steerAlpha;
        private float _steerWheelInput; // -1 to 1 - Left to Right
        private float _throttleInput; // 0 to 1 - Less to More


        // Sums up the steering angle of all the wheels
        public float SteeringAngleDegrees => _steerWheels.Sum(wheel => wheel.SteerAngle) / _steerWheels.Count;
        public Vector3 SteerCenter => _steerWheels.Aggregate(Vector3.zero, (current, wheel) => current + wheel.transform.position) / _steerWheels.Count;
        public Vector3 BackWheelCenter => _backWheels.Aggregate(Vector3.zero, (current, wheel) => current + wheel.transform.position) / _backWheels.Count;
        public float LinearVelocity => _rigidbody.linearVelocity.magnitude;
        public Vector3 Velocity => _rigidbody.linearVelocity;
        public float NormalizedSpeed => Mathf.Abs(Vector3.Dot(_rigidbody.linearVelocity, transform.forward)) /
                                        (_topForwardsKPH / 3.6f);
        // Can be changed in the future to allow for custom position, like might be needed for bigger vehicles
        public Vector3 Position => transform.position;
        public float ForwardSpeed => Vector3.Dot(_rigidbody.linearVelocity, transform.forward);
        public float ForwardSpeedKPH => Vector3.Dot(_rigidbody.linearVelocity, transform.forward) * 3.6f;
        public Vector3 Forward => transform.forward;
        public Vector3 Right => transform.right;
        public Vector3 ForwardPlanner => new Vector3(Forward.x, 0, Forward.z).normalized;
        public float NormalizedForwardSpeed =>
            Mathf.Clamp01(ForwardSpeed / (_topForwardsKPH / 3.6f));
        public float NormalizedBackwardsSpeed =>
            Mathf.Clamp01(Vector3.Dot(_rigidbody.linearVelocity, -transform.forward) / (_topBackwardsKPH / 3.6f));


        public float SteerWheelInput
        {
            get => _steerWheelInput;
            set => _steerWheelInput = Mathf.Clamp(value, -1.0f, 1.0f);
        }

        public float CombinedInput
        {
            get => _throttleInput - _breakInput;
            set
            {
                _throttleInput = Mathf.Clamp01(value);
                _breakInput = Mathf.Clamp01(-value);
            }
        }
        
        public float ThrottleInput
        {
            get => _throttleInput;
            set => _throttleInput = Mathf.Clamp01(value);
        }

        public float BreakInput
        {
            get => _breakInput;
            set => _breakInput = Mathf.Clamp01(value);
        }

        public bool IsHandBrakeEngaged { get; set; }

        

        private void Awake()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            _rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            HandleSteering();
            HandlePower();
            HandleBreaking();
            HandleDrag();
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying == false || _debug == false)
                return;

            // Debug draw steering 
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(SteerCenter, Quaternion.Euler(0, SteeringAngleDegrees, 0) * Forward * 3.0f);
        }

        public void ForceSpeed(float speed)
        {
            _rigidbody.linearVelocity = transform.forward * speed;
        }
        
        private void HandleSteering()
        {
            foreach (var wheel in _steerWheels)
            {
                _steerAlpha = Mathf.MoveTowardsAngle(_steerAlpha, _steerWheelInput, Time.fixedDeltaTime / _steerSpeed);
                wheel.SetSteerAlpha(_steerAlpha * _steerOverSpeed.Evaluate(NormalizedForwardSpeed));
            }
        }

        private void HandlePower()
        {
            if (_powerWheels.Count <= 0)
                return;
            
            // Need to add a reverse gear so that it can be controlled by a player agan
            // maye add fake gears 
            
            var availableTorque = _torqueOverSpeed.Evaluate(NormalizedForwardSpeed) / _powerWheels.Count *
                                  _torqueScale * _throttleInput;
            
            foreach (var wheel in _powerWheels)
                wheel.ApplyTorque(availableTorque);
        }

        private void HandleBreaking()
        {
            foreach (var wheel in _breakingWheels)
            {
                // Again unrealistic model
                float engineBreakingForce = _throttleInput <= 0 ? _engineBreakingForce : 0.0f;
                float breakForce = engineBreakingForce + _breakInput;
                wheel.BreakInput = breakForce;
            }
        }

        private void HandleDrag()
        {
            _rigidbody.linearDamping = _dragCurve.Evaluate(NormalizedSpeed) * _maxDrag;
        }
    }
}