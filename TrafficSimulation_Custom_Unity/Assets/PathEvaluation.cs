using System;
using System.IO;
using TrafficSimulation;
using UnityEngine;

public class PathEvaluation : MonoBehaviour
{
    [SerializeField] private CarControllerAI _carControllerAI;

    [DebugGUIGraph(min: -1.0f, max: 1.0f, r: 0, g: 0, b: 0, autoScale: false, group: 2)]
    float _distanceFromPath;

    [DebugGUIGraph(min: 0, max: 1, r: 0, g: 0, b: 0, autoScale: true, group: 3)]
    float _distanceAlongPath;

    [SerializeField] private float _sampleRate = 1.0f / 60.0f ;
    
    private bool finished = false;
    private TextWriter tw;

    
    private void Start()
    {
        string path = "Data/PathEvaluation.csv";

        // if file exists, delete it
        if (File.Exists(path))
            File.Delete(path);
        
        
        tw = new StreamWriter(path);
        tw.WriteLine("Distance Along Path,Time,Speed,Throttle Input,Break Input,Steering Steer Wheel Input,Deviation From Path");
        Debug.Log("Writing to " + path);
        
        
        InvokeRepeating(nameof(WritePoint), 0.0f, _sampleRate);
    }


    private void OnDrawGizmos()
    {
        
        if (_carControllerAI == null)
            return;
        
        if(_carControllerAI.CurrentSegment == null)
            return;
        
        float currentDistance = _carControllerAI.CurrentSegment.SampleDistanceAlongPathFromPosition(_carControllerAI.transform.position);

        for (int i = 0; i < 6; i++)
        {
            float distance = i * 2.0f + currentDistance;
            Vector3 position = _carControllerAI.CurrentSegment.SamplePositionFromDistanceAlongPath(distance);
            Gizmos.DrawWireSphere(position, 1.0f);
        }
        
    }

    private void WritePoint()
    {
        if(finished)
            return;
        
        if (_carControllerAI == null)
            return;

        if (_carControllerAI.CurrentSegment == null)
        {
            finished = true;
            tw.Close();
            Debug.Log("Finished writing to file");
            return;
        }
        
        
        Segment.SegmentSample segmentSample = _carControllerAI.CurrentSegment.GetSampleFromPosition(_carControllerAI.transform.position);
        
        Vector3 agentPositionAlongPath = segmentSample.Position;
        agentPositionAlongPath.y = 0.0f;
        
        Vector3 agentPosition = _carControllerAI.transform.position;
        agentPosition.y = 0.0f;
        
        _distanceFromPath = Vector3.Distance(agentPosition, agentPositionAlongPath) * (segmentSample.OnLeftSideOfSegment ? 1.0f : -1.0f);
        _distanceAlongPath = segmentSample.DistanceAlongPath;
        
        // tw.WriteLine("
        // Distance Along Path,
        // Time,
        // Speed,
        // Throttle Input,
        // Break Input,
        // Steering Steer Wheel Input,
        // Deviation From Path");
        tw.WriteLine($"{_distanceAlongPath},{Time.time},{_carControllerAI.CurrentSpeed},{_carControllerAI.CarBehaviour.ThrottleInput},{_carControllerAI.CarBehaviour.BreakInput},{_carControllerAI.CarBehaviour.SteerWheelInput},{_distanceFromPath}");
    }
}
