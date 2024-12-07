using UnityEditor;
using UnityEngine;

public class CarWheel : MonoBehaviour
{
    [SerializeField] private Rigidbody _connectedRigidbody;

    [Header("Spring")] [SerializeField] private float _springStrength = 16000.0f;

    [SerializeField] private float _springDamping = 1200.0f;
    [SerializeField] private float _restLength = 0.38f;
    [SerializeField] private float _breakingForce = 0.38f;

    [Header("Wheel")] [SerializeField] private bool _flipForward;

    [SerializeField] private float _wheelRadius = 0.26f;
    [SerializeField] private Transform _wheelVisual;

    [Header("Grip")] [SerializeField] private float _wheelMass = 15.0f;

    [SerializeField] private float _wheelGrip = 1.0f;
    [SerializeField] private float _wheelBreakingGrip = 0.15f;
    [SerializeField] private float _defaultForwardGrip = 0.1f;


    [Header("Steering")] [SerializeField] private AnimationCurve _steerCurve;

    [SerializeField] private float _maxSteerAngle = 40.0f;
    private bool _breaking;

    private GroundCheckResult _groundCheckStatus;

    private Vector3 _springFinalPoint;
    private float _startYAngle;

    private float _steerAlpha;

    private Vector3 WheelForward => _flipForward ? -transform.right : transform.right;

    private Vector3 WheelOutwards => transform.forward;

    private Vector3 SpringDirection => -transform.up;

    private Vector3 SpringStartPoint => transform.position;

    private Vector3 WheelVelocity => _connectedRigidbody.GetPointVelocity(_wheelVisual.position);

    private float SpringVelocity => Vector3.Dot(-SpringDirection, WheelVelocity);

    private void Awake()
    {
        _startYAngle = transform.localEulerAngles.y;
    }

    private void FixedUpdate()
    {
        UpdateGroundCheckStatus();
        HandleSteering();
        HandleSpring();
        HandleGrip();
        HandleWheelVisual();
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            var axisLength = 0.2f;

            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, SpringDirection * (axisLength + _wheelRadius));
            Handles.Label(transform.position + SpringDirection * (axisLength + _wheelRadius), "Spring Direction");

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, WheelOutwards * (axisLength + _wheelRadius));
            Handles.Label(transform.position + WheelOutwards * (axisLength + _wheelRadius), "Outwards");

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, WheelForward * (axisLength + _wheelRadius));
            Handles.Label(transform.position + WheelForward * (axisLength + _wheelRadius), "Forward");
        }
    }
#endif

    public void SetSteerAlpha(float steerAlpha)
    {
        _steerAlpha = Mathf.Clamp(steerAlpha, -1.0f, 1.0f);
    }

    public void ApplyTorque(float torque)
    {
        // TODO: Torque should be applied based on the ground normal
        if (!_groundCheckStatus.IsOnGround)
            return;

        _connectedRigidbody.AddForceAtPosition(WheelForward * torque, _wheelVisual.transform.position);
    }

    public void SetBreaking(bool breaking)
    {
        _breaking = breaking;
    }

    private void UpdateGroundCheckStatus()
    {
        var isOnGround = Physics.SphereCast(SpringStartPoint, _wheelRadius, SpringDirection, out var hit,
            _restLength);

        var hitDistance = hit.distance;

        // Check if not inside ground
        if (isOnGround == false &&
            Physics.OverlapSphere(SpringStartPoint, _wheelRadius, _connectedRigidbody.gameObject.layer).Length >
            0)
        {
            isOnGround = true;
            hitDistance = 0.0f;
        }

        _groundCheckStatus = new GroundCheckResult(isOnGround, hitDistance);
    }

    private void HandleSpring()
    {
        if (_groundCheckStatus.IsOnGround)
        {
            var springOffset = 1.0f - _groundCheckStatus.HitDistance / _restLength;

            if (springOffset >= 1.0f)
            {
                // Stop body from moving further
                var acceleration = Mathf.Min(SpringVelocity / Time.fixedDeltaTime, 0.0f);
                _connectedRigidbody.AddForceAtPosition(_breakingForce * acceleration * SpringDirection,
                    _wheelVisual.position);
            }

            var force = springOffset * _springStrength - SpringVelocity * _springDamping;
            _connectedRigidbody.AddForceAtPosition(-SpringDirection * force, _wheelVisual.position);
            _springFinalPoint = SpringStartPoint + _groundCheckStatus.HitDistance * SpringDirection;
        }
        else
        {
            _springFinalPoint = SpringStartPoint + SpringDirection * _restLength;
        }
    }

    private void HandleGrip()
    {
        if (_groundCheckStatus.IsOnGround == false)
            return;

        var wheelVelocity = _connectedRigidbody.GetPointVelocity(_wheelVisual.position);

        // Sideways grip
        var wheelSidewaysVelocity = Vector3.Dot(WheelOutwards, wheelVelocity);
        var wheelSidewaysGripResistanceAcceleration = -wheelSidewaysVelocity * _wheelGrip / Time.fixedDeltaTime;
        _connectedRigidbody.AddForceAtPosition(WheelOutwards * (_wheelMass * wheelSidewaysGripResistanceAcceleration),
            _wheelVisual.position);

        // Forward grip break
        var wheelForwardsVelocity = Vector3.Dot(WheelForward, wheelVelocity);
        var wheelForwardsGripResistanceAcceleration =
            -wheelForwardsVelocity * (_breaking ? _wheelBreakingGrip : 0.0f) / Time.fixedDeltaTime;
        _connectedRigidbody.AddForceAtPosition(WheelForward * (_wheelMass * wheelForwardsGripResistanceAcceleration),
            _wheelVisual.position);

        // Forward grip default
        var wheelForwardsDefaultGripResistanceAcceleration =
            -wheelForwardsVelocity * _defaultForwardGrip / Time.fixedDeltaTime;
        _connectedRigidbody.AddForceAtPosition(
            WheelForward * (_wheelMass * wheelForwardsDefaultGripResistanceAcceleration),
            _wheelVisual.position);
    }


    private void HandleSteering()
    {
        var targetSteerAngle = (_steerAlpha > 0.0f ? 1.0f : -1.0f) * _maxSteerAngle *
                               _steerCurve.Evaluate(Mathf.Abs(_steerAlpha));
        transform.localRotation = Quaternion.Euler(0, _startYAngle + targetSteerAngle, 0);
    }

    private void HandleWheelVisual()
    {
        var wheelVelocity = _connectedRigidbody.GetPointVelocity(_wheelVisual.position);
        var wheelForwardVelocity = -Vector3.Dot(transform.right, wheelVelocity);
        var rollSpeed = wheelForwardVelocity * 360.0f / (2.0f * Mathf.PI * _wheelRadius);
        _wheelVisual.Rotate(Vector3.forward, Time.fixedDeltaTime * rollSpeed);
        _wheelVisual.position = _springFinalPoint;
    }

    private struct GroundCheckResult
    {
        public readonly bool IsOnGround;
        public readonly float HitDistance;

        public GroundCheckResult(bool isOnGround, float hitDistance)
        {
            IsOnGround = isOnGround;
            HitDistance = hitDistance;
        }
    }
}