using System;
using TrafficSimulation;
using UnityEngine;

public class TrafficAgent : MonoBehaviour
{
    [SerializeField] private TrafficSystem _trafficSystem;
    [SerializeField] private CarBehaviour _carBehaviour;
    [SerializeField] private AgentSettings _settings;
    [SerializeField] private Sensor _frontSensor;
    [SerializeField] private Vector3 _agentSize = new Vector3(1.0f, 1.0f,1.0f);
    [SerializeField] private Vector3 _agentCenter = new Vector3(0.0f, 0.5f, 0.0f);
    [SerializeField] private float _frontSensorDistance = 5f;
    [SerializeField] private float _frontSensorAnglePositionOffset = 0.025f;
    
    private Segment _currentSegment;
    private Segment _nextSegment;
    private RaycastHit _frontSensorHit;
    
    
    public Transform FrontSensorTransform => _frontSensor.transform;
    public Vector3 AgentSize => _agentSize;
    public Vector3 AgentCenter => _agentCenter;
    public CarBehaviour CarBehaviour => _carBehaviour;
    
    public RaycastHit FrontSensorHit => _frontSensorHit;
    
    public Segment CurrentSegment => _currentSegment;
    public Segment NextSegment => _nextSegment;
    public AgentSettings Settings => _settings;

    public Segment.Sample CurrentSample { get; private set; }

    
    
    private void Start()
    {
        if(_trafficSystem == null)
            _trafficSystem = FindObjectOfType<TrafficSystem>();
        
        if(_carBehaviour == null)
            _carBehaviour = GetComponent<CarBehaviour>();
        
        
        _nextSegment = _trafficSystem.GetClosestSegment(_carBehaviour.Position);
        if(_nextSegment == null)
            Debug.LogError("No segment found for agent.");
        
        UpdateSegment();
    }
    
    private void OnDrawGizmos()
    {
        Matrix4x4 originalMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position + _agentCenter, transform.rotation, Vector3.one);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, _agentSize);
        Gizmos.matrix = originalMatrix;
    }

    private void Update()
    {
        if(_currentSegment == null)
            return;
        
        CurrentSample = _currentSegment.SampleFromPosition(_carBehaviour.Position);
        if(CurrentSample.IsAtEndOfSegment)
            UpdateSegment();

        UpdateSensorInformation();
    }

    private void UpdateSensorInformation()
    {
        float steeringAngle = _carBehaviour.SteeringAngleDegrees;
        _frontSensor.transform.localRotation = Quaternion.Euler(0.0f, steeringAngle, 0.0f);
        
        Vector3 target = _frontSensor.transform.localPosition;
        target.x = steeringAngle * _frontSensorAnglePositionOffset;
        _frontSensor.transform.localPosition = target;
        
        _frontSensor.Sense(_frontSensorDistance, out _frontSensorHit);
        
        // RaycastHit senseResultAngled = new RaycastHit();
        // {
        //     float steeringAngle = _carBehaviour.SteeringAngleDegrees;
        //     _frontSensor.transform.localRotation = Quaternion.Euler(0.0f, steeringAngle, 0.0f);
        //
        //     Vector3 target = _frontSensor.transform.localPosition;
        //     target.x = steeringAngle * _frontSensorAnglePositionOffset;
        //     _frontSensor.transform.localPosition = target;
        //
        //     _frontSensor.Sense(_frontSensorDistance, out senseResultAngled);
        // }
        //
        // RaycastHit senseResultStraight = new RaycastHit();
        // {
        //     _frontSensor.transform.localRotation = Quaternion.identity;
        //     _frontSensor.transform.localPosition = new Vector3(0.0f, _frontSensor.transform.localPosition.y, _frontSensor.transform.localPosition.z);
        //     
        //     _frontSensor.Sense(_frontSensorDistance, out senseResultStraight);
        // }
        //
        // if(senseResultAngled.collider == null && senseResultStraight.collider == null)
        //     return;
        //
        // if(senseResultStraight.collider == null)
        // {
        //     _frontSensorHit = senseResultAngled;
        //     return;
        // }
        //
        // if(senseResultAngled.collider == null)
        // {
        //     _frontSensorHit = senseResultStraight;
        //     return;
        // }
        //
        // _frontSensorHit = (senseResultAngled.distance < senseResultStraight.distance ? senseResultAngled : senseResultStraight);
    }


    private void UpdateSegment()
    {
        _currentSegment = _nextSegment;
        if(_currentSegment != null)
            _nextSegment = _trafficSystem.GetNextSegmentRandom(_currentSegment);
    }
}
