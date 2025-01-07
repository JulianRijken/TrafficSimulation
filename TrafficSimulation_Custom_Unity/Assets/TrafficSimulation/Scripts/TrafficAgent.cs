using TrafficSimulation;
using UnityEngine;

public class TrafficAgent : MonoBehaviour
{
    [SerializeField] private TrafficSystem _trafficSystem;
    [SerializeField] private CarBehaviour _carBehaviour;
    [SerializeField] private AgentSettings _settings;
    [SerializeField] private Sensor _frontSensor;
    [SerializeField] private Vector3 _agentSize = new Vector3(1.0f, 1.0f, 1.0f);
    [SerializeField] private Vector3 _agentCenter = new Vector3(0.0f, 0.5f, 0.0f);

    private float _timeSpentInIntersection = 0.0f;
    private float _timeSpendWaitingInIntersection = 0.0f;
    
    private Segment _currentSegment;
    private Segment _nextSegment;
    private Sensor.Result _frontSensorResult;


    private float _senseReactionTimer = 0.0f;
    private float _senseReactionSpeed = 0.2f;


    public Intersection CurrentIntersection { get; set; }

    public Transform FrontSensorTransform => _frontSensor.transform;
    public Vector3 AgentSize => _agentSize;
    public Vector3 AgentCenter => _agentCenter;
    public CarBehaviour CarBehaviour => _carBehaviour;

    public Sensor.Result FrontSensorResult => _frontSensorResult;

    public Segment CurrentSegment => _currentSegment;
    public Segment NextSegment => _nextSegment;
    public AgentSettings Settings => _settings;

    private Segment.Sample _currentSample;
    private Segment.Sample _currentExtendedSample;

    public Segment.Sample CurrentSample => _currentSample;
    public Segment.Sample CurrentExtendedSample => _currentExtendedSample;

    public IntersectionStateType IntersectionState = IntersectionStateType.None;


    public enum IntersectionStateType
    {
        None,
        Waiting,
        Moving
    }


    private void Awake()
    {
        if (_trafficSystem == null)
            _trafficSystem = FindAnyObjectByType<TrafficSystem>();

        if (_carBehaviour == null)
            _carBehaviour = GetComponent<CarBehaviour>();
    }

    private void Start()
    {
        _nextSegment = _trafficSystem.GetClosestSegment(_carBehaviour.Position);

        if (_nextSegment == null)
            Debug.LogError("No segment found for agent.");

        UpdateSegment();
        UpdateSamples();
    }

    private void Update()
    {
        if (_currentSegment == null)
            return;

        UpdateSamples();

        if (CurrentExtendedSample.IsAtEndOfSegment)
            UpdateSegment();


        if (_settings.UseFrontSensor)
        {
            _senseReactionTimer += Time.deltaTime;
            if (_senseReactionTimer >= _senseReactionSpeed)
            {
                float senseDistance = _settings.FrontSensorDefaultDistance +
                                      _settings.FrontSensorDistanceOverSpeed * _carBehaviour.ForwardSpeed;
                _frontSensorResult = _frontSensor.Sense(senseDistance);
                _senseReactionTimer = 0.0f;
                _senseReactionSpeed = Random.Range(_settings.SensingReactionSpeed.x, _settings.SensingReactionSpeed.y);
            }
        }
        else
        {
            _frontSensorResult = new Sensor.Result
            {
                Distance = Mathf.Infinity,
                Velocity = Vector3.zero
            };
        }
        
        if (IntersectionState == IntersectionStateType.Waiting)
            _timeSpendWaitingInIntersection += Time.deltaTime;

        if (IntersectionState == IntersectionStateType.Moving || CurrentIntersection == null)
            _timeSpentInIntersection = 0.0f;
        
        _timeSpentInIntersection += Time.deltaTime;

        if (_timeSpentInIntersection >= _settings.MaxTimeInIntersection)
        {
            Debug.LogWarning("Agent spent too much time in intersection");
            CurrentIntersection.RemoveAgent(this);
            CurrentIntersection = null;
            IntersectionState = IntersectionStateType.None;
        }
    }


    private void UpdateSamples()
    {
        UpdateCurrentSample(_carBehaviour.Position);
        UpdateCurrentExtendedSample(_carBehaviour.Position);
    }


    private void UpdateSegment()
    {
        _currentSegment = _nextSegment;

        if (_currentSegment != null)
        {
            _nextSegment = _trafficSystem.GetNextSegmentRandom(_currentSegment);
            
            // Force remove from intersection
            if (CurrentIntersection != null && CurrentIntersection.EarlyExitAllowed)
            {
                CurrentIntersection.RemoveAgent(this);
                CurrentIntersection = null;
            }   
        }
    }


    public Segment.Sample SampleFromDistanceExtended(float distanceAlongPath)
    {
        if (distanceAlongPath <= _currentSegment.TotalLength || _nextSegment == null)
            return _currentSegment.SampleFromDistanceClamped(distanceAlongPath);


        float distanceToNextSegment = Vector3.Distance(_currentSegment.EndPosition, _nextSegment.StartPosition);

        // Interpolate between current and next segment
        if (distanceAlongPath <= _currentSegment.TotalLength + distanceToNextSegment)
        {
            Segment.Sample fromSample = _currentSegment.SampleFromDistanceClamped(_currentSegment.TotalLength);
            Segment.Sample toSample = _nextSegment.SampleFromDistanceClamped(0.0f);
            return Segment.Sample.Interpolate(fromSample, toSample, distanceAlongPath - _currentSegment.TotalLength);
        }

        return _nextSegment.SampleFromDistanceClamped(distanceAlongPath - _currentSegment.TotalLength -
                                                      distanceToNextSegment);
    }

    private void UpdateCurrentSample(Vector3 position)
    {
        _currentSample = _currentSegment.SampleFromPositionClamped(position);
    }

    private void UpdateCurrentExtendedSample(Vector3 position)
    {
        _currentExtendedSample = _currentSample;
        if (_nextSegment == null)
            return;

        Vector3 directionToNextSegment = (_nextSegment.StartPosition - _currentSegment.EndPosition).normalized;
        float distanceToNextSegment = Vector3.Distance(_currentSegment.EndPosition, _nextSegment.StartPosition);
        float distanceAwayFromEnd = Vector3.Dot(directionToNextSegment, position - _currentSegment.EndPosition);

        if (_currentExtendedSample.IsAtEndOfSegment)
        {
            float alpha = distanceAwayFromEnd / distanceToNextSegment;
            _currentExtendedSample.Position =
                Vector3.Lerp(_currentSegment.EndPosition, _nextSegment.StartPosition, alpha);
            _currentExtendedSample.DirectionForward = directionToNextSegment;
            _currentExtendedSample.DirectionRight = Vector3.Cross(_currentExtendedSample.DirectionForward, Vector3.up);
            _currentExtendedSample.DistanceAlongSegment += distanceAwayFromEnd;
            _currentExtendedSample.AlphaAlongSegment = Mathf.Clamp01(_currentExtendedSample.DistanceAlongSegment /
                                                                     (_currentSegment.TotalLength +
                                                                      distanceToNextSegment));
        }

        _currentExtendedSample.DistanceToSegmentEnd = Mathf.Max(0.0f,
            _currentSegment.TotalLength + distanceToNextSegment - _currentExtendedSample.DistanceAlongSegment);
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying == false)
            return;

        if (_carBehaviour == null)
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
                var sample = closestSegment.SampleFromPositionClamped(_carBehaviour.Position);
                Gizmos.color = _settings.DebugClosestSampleColor;
                Gizmos.DrawSphere(sample.Position, 0.5f);
                MathExtensions.DrawArrow(_carBehaviour.Position, sample.Position);
            }
        }

        if (_settings.DebugAllSamples)
        {
            _trafficSystem.Segments.ForEach(segment =>
            {
                var sample = segment.SampleFromPositionClamped(_carBehaviour.Position);

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
            Gizmos.DrawSphere(_carBehaviour.Position + Vector3.up, _carBehaviour.ForwardSpeed);
        }

        if (_settings.DebugIntersectionState)
        {
            Gizmos.color = IntersectionState switch
            {
                IntersectionStateType.Waiting => Color.red,
                IntersectionStateType.Moving => Color.blue,
                _ => Color.green
            };

            Gizmos.DrawSphere(_carBehaviour.Position + Vector3.up * 2, 0.4f);
        }
    }
}
   