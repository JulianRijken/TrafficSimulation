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

        private int _collisionCount;

        private void Start()
        {
            _timeScaleInfo.SetTypeText("Time Scale");
            _collisionsInfo.SetTypeText("Collisions");
            _carCountInfo.SetTypeText("Car Count");
            _averageSpeedInfo.SetTypeText("Avr Speed");

            //Reserve message from CarControllerAI.OnVehicleCollision
            // Use unity messages from   BroadcastMessage("OnCarCollision", SendMessageOptions.DontRequireReceiver);
            
            
            
        }

        private void Update()
        {
            _timeScaleInfo.SetInfoText(Time.timeScale.ToString("F2"));
            _collisionsInfo.SetInfoText(_collisionCount.ToString());

            var carCount = FindObjectsByType<CarBehaviour>(FindObjectsSortMode.None).Length;
            _carCountInfo.SetInfoText(carCount.ToString());

            var vehicles = FindObjectsByType<CarBehaviour>(FindObjectsSortMode.None);
            if (vehicles.Length > 0)
            {
                var averageSpeed = vehicles.Average(vehicle => vehicle.ForwardSpeedKPH);
                _averageSpeedInfo.SetInfoText(averageSpeed.ToString("F0") + "km/h");
            }
        }

        private void IncrementCollisionCount()
        {
            _collisionCount++;
        }
    }
}