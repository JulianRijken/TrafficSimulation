using System.Collections.Generic;
using UnityEngine;

namespace TrafficSimulation
{
    public class CarBehaviour : MonoBehaviour
    {
        [Header("Wheels")] [SerializeField] private List<CarWheel> _steerWheels = new();

        [SerializeField] private List<CarWheel> _powerWheels = new();
        [SerializeField] private List<CarWheel> _breakingWheels = new();

        [Header("Steering")] [SerializeField] private float _steerSpeed = 10.0f;

        [SerializeField] private AnimationCurve _steerOverSpeed;

        [Header("Engine")] [SerializeField] [Range(0.0f, 30000.0f)]
        private float _torqueScale = 10000.0f;

        [SerializeField] private float _topForwardsKPH = 20.0f;
        [SerializeField] private float _topBackwardsKPH = 5.0f;
        [SerializeField] private AnimationCurve _torqueOverSpeed;

        [Header("Drag")] [SerializeField] private float _maxDrag = 1.0f;

        [SerializeField] private AnimationCurve _dragCurve;
        private float _breakInput; // 0 to 1 - Less to More

        private Rigidbody _rigidbody;

        private float _steerAlpha;
        private float _steerWheelInput; // -1 to 1 - Left to Right
        private float _throttleInput; // 0 to 1 - Less to More


        public float Velocity => _rigidbody.linearVelocity.magnitude;

        public float SteerWheelInput
        {
            get => _steerWheelInput;
            set => _steerWheelInput = Mathf.Clamp(value, -1.0f, 1.0f);
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


        private float NormalizedSpeed => Mathf.Abs(Vector3.Dot(_rigidbody.linearVelocity, transform.forward)) /
                                         (_topForwardsKPH / 3.6f);

        private float NormalizedForwardSpeed =>
            Mathf.Clamp01(Vector3.Dot(_rigidbody.linearVelocity, transform.forward) / (_topForwardsKPH / 3.6f));

        private float NormalizedBackwardsSpeed =>
            Mathf.Clamp01(Vector3.Dot(_rigidbody.linearVelocity, -transform.forward) / (_topBackwardsKPH / 3.6f));

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

            // Simple method no gears
            var simpleInput = _throttleInput - _breakInput;
            var availableTorque =
                _torqueOverSpeed.Evaluate(simpleInput > 0.0f ? NormalizedForwardSpeed : NormalizedBackwardsSpeed) /
                _powerWheels.Count * _torqueScale * simpleInput;
            foreach (var wheel in _powerWheels)
                wheel.ApplyTorque(availableTorque);
        }

        private void HandleBreaking()
        {
            foreach (var wheel in _breakingWheels)
            {
                var simpleInput = _throttleInput - _breakInput;
                var autoBreak = (simpleInput < 0.0f && NormalizedForwardSpeed > 0.0f) ||
                                (simpleInput > 0.0f && NormalizedBackwardsSpeed > 0.0f);
                wheel.SetBreaking(IsHandBrakeEngaged || autoBreak);
            }
        }

        private void HandleDrag()
        {
            _rigidbody.linearDamping = _dragCurve.Evaluate(NormalizedSpeed) * _maxDrag;
        }
    }
}