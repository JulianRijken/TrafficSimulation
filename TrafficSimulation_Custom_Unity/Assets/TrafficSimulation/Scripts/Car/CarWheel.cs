using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class CarWheel : MonoBehaviour
{
    [SerializeField] private Rigidbody _connectedRigidbody;

    [Header("Spring")] 
    [SerializeField] private float _springStrength = 16000.0f;
    [SerializeField] private float _springDamping = 1200.0f;
    [SerializeField] private float _restLength = 0.38f;
    [SerializeField] private float _springBreakingFoce = 0.38f;

    [Header("Wheel")]
    [SerializeField] private bool _flipForward;
    [SerializeField] private float _wheelRadius = 0.26f;
    [SerializeField] private Transform _wheelVisual;

    [Header("Grip")] 
    [SerializeField] private float _wheelMass = 20.0f;
    [SerializeField] private float _sidewaysGrip = 10.0f;
    [SerializeField] private float _straightGrip = 1.0f;

    [Header("Steering")] 
    [SerializeField] private AnimationCurve _steerCurve;
    [SerializeField] private float _maxSteerAngle = 40.0f;
    
    [FormerlySerializedAs("_breakingForce")]
    [Header("Breaking")] 
    [SerializeField] private float _maxBreakingForce = 10.0f;

    private float _startYAngle;
    private float _steerAlpha;
    private float _breakInput;
    private Vector3 _springFinalPoint;
    private GroundCheckResult _groundCheckStatus;

    private Vector3 WheelForward => _flipForward ? -transform.right : transform.right;
    
    private Vector3 WheelOutwards => transform.forward;

    private Vector3 SpringDirection => -transform.up;

    private Vector3 SpringStartPoint => transform.position;

    private Vector3 WheelVelocity => _connectedRigidbody.GetPointVelocity(_wheelVisual.position);

    private float SpringVelocity => Vector3.Dot(-SpringDirection, WheelVelocity);

    
    public float SteerAngle => (_steerAlpha > 0.0f ? 1.0f : -1.0f) * _maxSteerAngle *
                               _steerCurve.Evaluate(Mathf.Abs(_steerAlpha));
    
    public float BreakInput
    {
        get => _breakInput;
        set => _breakInput = Mathf.Clamp01(value);
    }

    
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
        HandleBreaking();
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

            Gizmos.color = Color.white;
            MathExtensions.DrawCircle(transform.position,WheelOutwards, _wheelRadius);
        }
    }
#endif

    public void SetSteerAlpha(float steerAlpha)
    {
        _steerAlpha = Mathf.Clamp(steerAlpha, -1.0f, 1.0f);
    }




    private void UpdateGroundCheckStatus()
    {
        var isOnGround = Physics.SphereCast(SpringStartPoint, _wheelRadius, SpringDirection, out var hit,
            _restLength);

        var hitDistance = hit.distance;

        // // Check if not inside ground
        // if (isOnGround == false &&
        //     Physics.OverlapSphere(SpringStartPoint, _wheelRadius, _connectedRigidbody.gameObject.layer).Length >
        //     0)
        // {
        //     isOnGround = true;
        //     hitDistance = 0.0f;
        // }

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
                _connectedRigidbody.AddForceAtPosition(_springBreakingFoce * acceleration * SpringDirection,
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

        // Sideways Grip
        var wheelSidewaysVelocity = Vector3.Dot(WheelOutwards, WheelVelocity);
        var wsvAcceleration = -wheelSidewaysVelocity * _sidewaysGrip / Time.fixedDeltaTime;
        _connectedRigidbody.AddForceAtPosition(WheelOutwards * (_wheelMass * wsvAcceleration),
            _wheelVisual.position);

       // Straight Grip
       var wheelForwardVelocity = Vector3.Dot(WheelForward, WheelVelocity);
       var wfvAcceleration = -wheelForwardVelocity * _straightGrip / Time.fixedDeltaTime;
       _connectedRigidbody.AddForceAtPosition(WheelForward * (_wheelMass * wfvAcceleration),
           _wheelVisual.position);
    }
    
    
    private void HandleBreaking()
    {
        if (_groundCheckStatus.IsOnGround == false)
            return;
        
        float brakingForce = _maxBreakingForce * _breakInput;
        float wheelBackwardsVelocity = Vector3.Dot(-WheelForward, WheelVelocity);
        _connectedRigidbody.AddForceAtPosition(WheelForward * (brakingForce * Mathf.Sign(wheelBackwardsVelocity)), _wheelVisual.position);
    }
    
    public void ApplyTorque(float torque)
    {
        // TODO: Torque should be applied based on the ground normal
        if (!_groundCheckStatus.IsOnGround)
            return;

        _connectedRigidbody.AddForceAtPosition(WheelForward * torque, _wheelVisual.position);
    }


    private void HandleSteering()
    {
        transform.localRotation = Quaternion.Euler(0, _startYAngle + SteerAngle, 0);
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