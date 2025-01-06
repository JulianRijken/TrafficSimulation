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

    
    private Segment _currentSegment;
    private Segment _nextSegment;
    private Sensor.Result _frontSensorResult;
    
    
    public Transform FrontSensorTransform => _frontSensor.transform;
    public Vector3 AgentSize => _agentSize;
    public Vector3 AgentCenter => _agentCenter;
    public CarBehaviour CarBehaviour => _carBehaviour;
    
    public Sensor.Result FrontSensorResult => _frontSensorResult;
    
    public Segment CurrentSegment => _currentSegment;
    public Segment NextSegment => _nextSegment;
    public AgentSettings Settings => _settings;

    public Segment.Sample CurrentSample { get; private set; }

    public IntersectionStateType IntersectionState = IntersectionStateType.None;

    
    public enum IntersectionStateType
    {
        None,
        Waiting,
        Moving
    }


    private void Awake()
    {
        if(_trafficSystem == null)
            _trafficSystem = FindAnyObjectByType<TrafficSystem>();
        
        if(_carBehaviour == null)
            _carBehaviour = GetComponent<CarBehaviour>();
    }

    private void Start()
    {
        _nextSegment = _trafficSystem.GetClosestSegment(_carBehaviour.Position);
        
        if(_nextSegment == null)
            Debug.LogError("No segment found for agent.");
        
        UpdateSegment();
        UpdateSample();
    }


    private void OnDrawGizmos()
    {
        if(Application.isPlaying == false)
            return;
        
        if(_carBehaviour == null)
            return;
        
        if (_settings.DebugAgentSize)
        {
            var originalMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position + _agentCenter, transform.rotation, Vector3.one);
            Gizmos.color = _settings.DebugAgentSizeWireColor;
            Gizmos.DrawWireCube(Vector3.zero, _agentSize);
            Gizmos.color = _settings.DebugAgentSizeFillColor;
            Gizmos.DrawCube(Vector3.zero, _agentSize);
            Gizmos.matrix = originalMatrix;
        }


        if (_settings.DebugCurrentSample || _settings.DebugAllSamples)
        {
            Gizmos.color = _settings.DebugCurrentSampleColor;
            Gizmos.DrawSphere(CurrentSample.Position, 0.5f);
            MathExtensions.DrawArrow(_carBehaviour.Position, CurrentSample.Position);
        }
        
        if (_settings.DebugClosestSample || _settings.DebugAllSamples)
        {
            var closestSegment = _trafficSystem.GetClosestSegment(_carBehaviour.Position);
            if (closestSegment != null)
            {
                var sample = closestSegment.SampleFromPosition(_carBehaviour.Position);
                Gizmos.color = _settings.DebugClosestSampleColor;
                Gizmos.DrawSphere(sample.Position, 0.5f);
                MathExtensions.DrawArrow(_carBehaviour.Position, sample.Position);
            }
        }
        
        if (_settings.DebugAllSamples)
        {
            _trafficSystem.Segments.ForEach(segment =>
            {
                var sample = segment.SampleFromPosition(_carBehaviour.Position);
                
                // Random color seed based on segment index
                int segmentIndex = _trafficSystem.Segments.IndexOf(segment);
                Gizmos.color = new Color(
                    Mathf.PerlinNoise(segmentIndex, 0.0f),
                    Mathf.PerlinNoise(segmentIndex, 1.0f),
                    Mathf.PerlinNoise(segmentIndex, 2.0f)
                );
                Gizmos.DrawSphere(sample.Position, 0.5f);
                MathExtensions.DrawArrow(_carBehaviour.Position, sample.Position);
            });
        }

        if (_settings.DebugSpeed)
        {
            
            // Gizmos.color = Color.Lerp(Color.white, Color.black, _dangerLevel);
            Gizmos.color = _settings.DebugSpeedCircleColor;
            MathExtensions.DrawCircle(_carBehaviour.Position + Vector3.up, _carBehaviour.ForwardSpeed);

            Gizmos.color = _settings.DebugSpeedSphereColor;
            Gizmos.DrawSphere(_carBehaviour.Position + Vector3.up,  _carBehaviour.ForwardSpeed);
        }
        
    }
    
    
    private void Update()
    {
        if(_currentSegment == null)
            return;

        UpdateSample();

        if(CurrentSample.IsAtEndOfSegment)
            UpdateSegment();

        _frontSensorResult = _frontSensor.Sense();
    }

    private void UpdateSample()
    {
        CurrentSample = _currentSegment.SampleFromPosition(_carBehaviour.Position);
    }
    


    private void UpdateSegment()
    {
        _currentSegment = _nextSegment;
        if(_currentSegment != null)
            _nextSegment = _trafficSystem.GetNextSegmentRandom(_currentSegment);
    }
}
