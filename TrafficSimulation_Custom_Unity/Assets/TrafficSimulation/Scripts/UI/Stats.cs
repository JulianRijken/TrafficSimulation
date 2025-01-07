using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrafficSimulation
{
    public class Stats : MonoBehaviour
    {
        [SerializeField] private InfoTextUI _timeScaleInfo;
        [SerializeField] private InfoTextUI _runningTimeInfo;
        [SerializeField] private InfoTextUI _collisionsInfo;
        [SerializeField] private InfoTextUI _carCountInfo;
        [SerializeField] private InfoTextUI _averageSpeedInfo;
        [SerializeField] private InfoTextUI _averageOffsetInfo;

        private int _collisionCount;
        
        List<StatTracker> _statTrackers = new List<StatTracker>();

        
        private void Start()
        {
            _statTrackers = FindObjectsByType<StatTracker>(FindObjectsSortMode.None).ToList();
            
            _timeScaleInfo.SetTypeText("Time Scale");
            _runningTimeInfo.SetTypeText("Time Running");
            _collisionsInfo.SetTypeText("Collisions");
            _carCountInfo.SetTypeText("Car Count");
            _averageSpeedInfo.SetTypeText("Avr Speed");
            _averageOffsetInfo.SetTypeText("Avr Offset");
            //Reserve message from CarControllerAI.OnVehicleCollision
            // Use unity messages from   BroadcastMessage("OnCarCollision", SendMessageOptions.DontRequireReceiver);
            
            _statTrackers.ForEach(carController => carController.OnCollision += IncrementCollisionCount);
        }
        
        [DebugGUIGraph(min: 0, max: 5, r: 0, g: 0, b: 0, autoScale: false, group: 0)]
        private float _averageDistanceFromPath;
        
        [DebugGUIGraph(min: 0, max: 80, r: 0, g: 0, b: 0, autoScale: false, group: 1)]
        private float _averageSpeed;

        private float _totalSpeed;
        
        private void Update()
        {
            
            var agents =  FindObjectsByType<TrafficAgent>(FindObjectsSortMode.None);

            if (agents.Length == 0)
            {
                Debug.Log(_totalSpeed / Time.time);
                //Pause editor
                UnityEditor.EditorApplication.isPaused = true;
                return;
            }
            
            
            _timeScaleInfo.SetInfoText(Time.timeScale.ToString("F2") + "x");
            _collisionsInfo.SetInfoText(_collisionCount.ToString());

            
            var carCount = agents.Length;
            _carCountInfo.SetInfoText(carCount.ToString());

            if (agents.Length > 0)
            {
                _averageSpeed = agents.Average(vehicle => vehicle.CarBehaviour.ForwardSpeedKPH);
                _totalSpeed += _averageSpeed * Time.deltaTime;
                _averageSpeedInfo.SetInfoText(_averageSpeed.ToString("F0") + "kph");
            }
            
            _averageDistanceFromPath = _statTrackers.Count > 0 ? agents.Average(agent => agent.CurrentSample.GetSidewaysDistanceFromPath(agent.CarBehaviour.Position)) : 0;
            _averageOffsetInfo.SetInfoText(_averageDistanceFromPath.ToString("F2") + "m");
            
            _runningTimeInfo.SetInfoText(Time.time.ToString("F2") + "s");
        }

        private void IncrementCollisionCount()
        {
            _collisionCount++;
        }
    }
}