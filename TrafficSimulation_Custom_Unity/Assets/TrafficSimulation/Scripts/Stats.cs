using System.Linq;
using TrafficSimulation.Scripts;
using UnityEngine;

public class Stats : MonoBehaviour
{
    [SerializeField] private InfoTextUI _timeScaleInfo;
    [SerializeField] private InfoTextUI _collisionsInfo;
    [SerializeField] private InfoTextUI _carCountInfo;
    [SerializeField] private InfoTextUI _avrageSpeedInfo;

    private int _collisionCount;


    private void Start()
    {
        _timeScaleInfo.SetTypeText("Time Scale");
        _collisionsInfo.SetTypeText("Collisions");
        _carCountInfo.SetTypeText("Car Count");
        _avrageSpeedInfo.SetTypeText("Average Speed");

        VehicleAI.OnVehicleCollision += IncrementCollisionCount;
    }

    private void Update()
    {
        _timeScaleInfo.SetInfoText(Time.timeScale.ToString("F2"));
        _collisionsInfo.SetInfoText(_collisionCount.ToString());

        var carCount = FindObjectsByType<VehicleAI>(FindObjectsSortMode.None).Length;
        _carCountInfo.SetInfoText(carCount.ToString());

        var vehicles = FindObjectsByType<Vehicle>(FindObjectsSortMode.None);
        if (vehicles.Length > 0)
        {
            var averageSpeed = vehicles.Sum(vehicle => vehicle.GetSpeed()) / vehicles.Length;
            _avrageSpeedInfo.SetInfoText(averageSpeed.ToString("F2"));
        }
    }

    private void IncrementCollisionCount()
    {
        _collisionCount++;
    }
}