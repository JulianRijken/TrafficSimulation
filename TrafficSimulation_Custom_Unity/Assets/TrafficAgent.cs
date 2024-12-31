using TrafficSimulation;
using UnityEngine;

public class TrafficAgent : MonoBehaviour
{
    [SerializeField] private TrafficSystem _trafficSystem;
    [SerializeField] private CarBehaviour _carBehaviour;
    [SerializeField] private AgentSettings _settings;

    private Segment _currentSegment;
    private Segment _nextSegment;
    
    public CarBehaviour CarBehaviour => _carBehaviour;
    
    
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

    private void Update()
    {
        if(_currentSegment == null)
            return;
        
        CurrentSample = _currentSegment.SampleFromPosition(_carBehaviour.Position);
        if(CurrentSample.IsAtEndOfSegment)
            UpdateSegment();
    }
    
    private void UpdateSegment()
    {
        _currentSegment = _nextSegment;
        if(_currentSegment != null)
            _nextSegment = _trafficSystem.GetNextSegmentRandom(_currentSegment);
    }
}
