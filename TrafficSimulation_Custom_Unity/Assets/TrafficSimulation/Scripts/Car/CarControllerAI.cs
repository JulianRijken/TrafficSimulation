using UnityEngine;

namespace TrafficSimulation
{
    public class CarControllerAI : MonoBehaviour
    {
        [SerializeField] private CarBehaviour _carBehaviour;
        [SerializeField] private TrafficSystem _trafficSystem;

        private void Update()
        {
            // _carBehaviour.SteerWheelInput = _controls.Car.Steer.ReadValue<float>();
            _carBehaviour.ThrottleInput = 1.0f;
            // _carBehaviour.BreakInput = _controls.Car.Break.ReadValue<float>();
        }
    }
}