using UnityEngine;
using UnityEngine.InputSystem;

namespace TrafficSimulation.Scripts.Car
{
    public class CarController : MonoBehaviour
    {
        [SerializeField] private CarBehaviour _carBehaviour;
        private Controls _controls;

        private void Awake()
        {
            _controls = new Controls();
            _controls.Enable();

            _controls.Car.HandBreak.performed += OnHandBreakInput;
            _controls.Car.HandBreak.canceled += OnHandBreakInput;
        }

        private void Update()
        {
            _carBehaviour.SteerWheelInput = _controls.Car.Steer.ReadValue<float>();
            _carBehaviour.ThrottleInput = _controls.Car.Throttle.ReadValue<float>();
            _carBehaviour.BreakInput = _controls.Car.Break.ReadValue<float>();
        }

        private void OnHandBreakInput(InputAction.CallbackContext context)
        {
            _carBehaviour.IsHandBrakeEngaged = context.ReadValueAsButton();
        }
    }
}