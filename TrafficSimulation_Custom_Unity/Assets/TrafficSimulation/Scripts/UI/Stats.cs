using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrafficSimulation
{
    public class Stats : MonoBehaviour
    {
        [SerializeField] private InfoTextUI _timeScaleInfo;
        [SerializeField] private InfoTextUI _collisionsInfo;
        [SerializeField] private InfoTextUI _carCountInfo;
        [SerializeField] private InfoTextUI _averageSpeedInfo;
        [SerializeField] private InfoTextUI _averageOffsetInfo;

        private int _collisionCount;
        
        List<CarControllerAI> _carControllers = new List<CarControllerAI>();

        private void Start()
        {
            _carControllers = FindObjectsByType<CarControllerAI>(FindObjectsSortMode.None).ToList();
            
            _timeScaleInfo.SetTypeText("Time Scale");
            _collisionsInfo.SetTypeText("Collisions");
            _carCountInfo.SetTypeText("Car Count");
            _averageSpeedInfo.SetTypeText("Avr Speed");
            _averageOffsetInfo.SetTypeText("Avr Offset");
            //Reserve message from CarControllerAI.OnVehicleCollision
            // Use unity messages from   BroadcastMessage("OnCarCollision", SendMessageOptions.DontRequireReceiver);
            
            _carControllers.ForEach(carController => carController.OnCarCollision += IncrementCollisionCount);
        }
        
        [DebugGUIGraph(min: 0, max: 5, r: 0, g: 0, b: 0, autoScale: false, group: 0)]
        private float _averageDistanceFromPath;
        
        [DebugGUIGraph(min: 0, max: 100, r: 0, g: 0, b: 0, autoScale: false, group: 1)]
        private float _averageSpeed;
        
        private void Update()
        {
            _timeScaleInfo.SetInfoText(Time.timeScale.ToString("F2") + "x");
            _collisionsInfo.SetInfoText(_collisionCount.ToString());

            var carCount = FindObjectsByType<CarBehaviour>(FindObjectsSortMode.None).Length;
            _carCountInfo.SetInfoText(carCount.ToString());

            var vehicles = FindObjectsByType<CarBehaviour>(FindObjectsSortMode.None);
            if (vehicles.Length > 0)
            {
                _averageSpeed = vehicles.Average(vehicle => vehicle.ForwardSpeedKPH);
                _averageSpeedInfo.SetInfoText(_averageSpeed.ToString("F0") + "km/h");
            }
            
            
            _averageDistanceFromPath = _carControllers.Count > 0 ? _carControllers.Average(carController => carController.DistanceFromPathPlanner) : 0;
            _averageOffsetInfo.SetInfoText(_averageDistanceFromPath.ToString("F2") + "m");
        }

        private void IncrementCollisionCount()
        {
            _collisionCount++;
        }
    }
}