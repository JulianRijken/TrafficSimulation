using TrafficSimulation.Scripts;
using UnityEngine;

namespace TrafficSimulation.Examples.Scripts
{
    public class VehicleController : MonoBehaviour
    {
        private Vehicle _vehicle;

        private void Start()
        {
            _vehicle = GetComponent<Vehicle>();
        }

        private void Update()
        {
            var acc = Input.GetAxis("Vertical");
            var steering = Input.GetAxis("Horizontal");
            float brake = Input.GetKey(KeyCode.Space) ? 1 : 0;

            _vehicle.Move(acc, steering, brake);
        }
    }
}