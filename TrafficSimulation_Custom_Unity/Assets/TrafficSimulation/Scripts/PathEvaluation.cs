using System;
using System.Collections.Generic;
using System.IO;
using TrafficSimulation;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEngine;
using UnityEngine.Serialization;

public class PathEvaluation : MonoBehaviour
{
    [SerializeField] private Segment segment;
    [SerializeField] private CarBehaviour agent;

    private bool moveSegment = true;
    
    [DebugGUIGraph(min: -1.0f, max: 1.0f, r: 0, g: 0, b: 0, autoScale: false, group: 2)]
    float _distanceFromPath;

    [DebugGUIGraph(min: 0, max: 1, r: 0, g: 0, b: 0, autoScale: true, group: 3)]
    float _distanceAlongPath;

    [SerializeField] private float _sampleRate = 1.0f / 60.0f ;
    
    private bool finished = false;
    private TextWriter tw;


    private List<Vector3> _pastPoints = new List<Vector3>();
    
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

    private void OnDestroy()
    {
        tw.Close();
        
    }

    private void Update()
    {
        if (!moveSegment)
            return;
        
        var sample = segment.GetSampleFromPosition(agent.Position);
        if (sample.AlphaAlongSegment > 0.5f)
        {
            moveSegment = false;
            segment.transform.position += Vector3.forward * 5.0f;
        }
    }


    private void OnDrawGizmos()
    {
        if(isActiveAndEnabled == false)
            return;
        
        if (!Application.isPlaying)
            return;

        
        
        
        // var sample = segment.GetSampleFromPosition(agent.Position);
        //
        // for (int i = 0; i < 6; i++)
        // {
        //     float distance = i * 2.0f + sample.DistanceAlongSegment;
        //     Vector3 position = segment.SamplePositionFromDistanceAlongPath(distance);
        //     Gizmos.DrawWireSphere(position, 1.0f);
        // }
        //
        //
        //
        // Gizmos.color = Color.red;
        // Gizmos.DrawSphere(sample.Position, 0.5f);
        //
        //
        Gizmos.color = Color.red;
        foreach (var point in _pastPoints)
            Gizmos.DrawSphere(point, 0.2f);
        
        // // Draw line between points
        // Gizmos.color = new Color(1,0,0,0.2f);
        // for (int i = 0; i < _pastPoints.Count - 1; i++)
        // {
        //     Gizmos.DrawLine(_pastPoints[i], _pastPoints[i + 1]);
        // }
        
    }

    private void WritePoint()
    {
        if (!Application.isPlaying)
            return;
        
        if(finished)
            return;
        
        _pastPoints.Add(agent.Position);
        
        var sample = segment.GetSampleFromPosition(agent.Position);
        
        if (sample.AlphaAlongSegment == 1.0f)
        {
            finished = true;
            tw.Close();
            Debug.Log("Finished writing to file");
            
            //pause the editor
            EditorApplication.isPaused = true;
            segment.transform.position = new Vector3(0, 0, 1000);
            return;
        }
        
        Vector3 agentPositionAlongPath = sample.Position;
        agentPositionAlongPath.y = 0.0f;
        
        Vector3 agentPosition = agent.Position;
        agentPosition.y = 0.0f;
        
        _distanceFromPath = Vector3.Distance(agentPosition, agentPositionAlongPath);
        _distanceAlongPath = sample.DistanceAlongSegment;
        tw.WriteLine($"{_distanceAlongPath},{Time.time},{agent.ForwardSpeed},{agent.ThrottleInput},{agent.BreakInput},{agent.SteerWheelInput},{_distanceFromPath}");
    }
}
