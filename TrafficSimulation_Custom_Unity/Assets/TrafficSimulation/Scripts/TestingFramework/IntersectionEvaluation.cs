using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TrafficSimulation
{
    public class IntersectionEvaluation : MonoBehaviour
    {
        [SerializeField] private string _fileSuffix = "null";
        [SerializeField] private bool _writeToFile = true;
        [SerializeField] private float _sampleRate = 0.1f;
        [SerializeField] private float _sampleCount = 100;
        
        private int _sampleIndex = 0;
        private TextWriter _textWriter;
        private List<EvaluationData> _trafficAgents = new List<EvaluationData>();

        private class EvaluationData
        {
            public TrafficAgent Agent;
            public float DistanceMoved;
        }
        
        private void OnEnable()
        {
            string path = $"Data/IntersectionEvaluation_{0}_{_fileSuffix}.csv";
            int i = 0;
            while (File.Exists(path))
            {
                i++;
                path = $"Data/PathEvaluation_{i}_{_fileSuffix}.csv";
            }

            _textWriter = new StreamWriter(path);

            var agents = FindObjectsByType<TrafficAgent>(FindObjectsSortMode.None);

            _textWriter.Write("Time, ");
            foreach (var agent in agents)
            {
                _trafficAgents.Add(new EvaluationData
                {
                    Agent = agent,
                    DistanceMoved = 0
                });
            }

            _trafficAgents.Sort((a, b) => string.Compare(a.Agent.name, b.Agent.name, StringComparison.Ordinal));
            foreach (var agent in _trafficAgents)
                _textWriter.Write(agent.Agent.name + ", ");

            _textWriter.Write("\n");


            // Start writing points
            InvokeRepeating(nameof(WritePoint), 0.0f, _sampleRate);
        }

        private void OnDisable()
        {
            if(_writeToFile == false)
                return;
            
            _textWriter.Close();
            Debug.Log("Finished writing to file");
        }

        private void Update()
        {
            foreach (var agent in _trafficAgents)
            {
                if(agent.Agent == null)
                    continue;
                
                agent.DistanceMoved += agent.Agent.CarBehaviour.ForwardSpeed * Time.deltaTime;
            }
        }
        
        private void WritePoint()
        {
            if(_writeToFile == false)
                return;
         
            _textWriter.Write(Time.time + ", ");
            
            foreach (var agent in _trafficAgents)
            {
                _textWriter.Write(agent.DistanceMoved + ", ");
            }
            
            _textWriter.Write("\n");
            
            _sampleIndex++;
            
            if (_sampleIndex >= _sampleCount)
            {
                CancelInvoke(nameof(WritePoint));
                Debug.Log("Intersection Evaluation Finished");
            }
        }
    }
}
