using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrafficSimulation.Scripts
{
    public enum IntersectionType
    {
        Stop,
        TrafficLight
    }

    public class Intersection : MonoBehaviour
    {
        public IntersectionType _intersectionType;
        public int _id;

        //For stop only
        public List<Segment> _prioritySegments;

        //For traffic lights only
        public float _lightsDuration = 8;
        public float _orangeLightDuration = 2;
        public List<Segment> _lightsNbr1;
        public List<Segment> _lightsNbr2;

        [HideInInspector] public int _currentRedLightsGroup = 1;

        private List<GameObject> _memVehiclesInIntersection = new();
        private List<GameObject> _memVehiclesQueue = new();

        private TrafficSystem _trafficSystem;
        private List<GameObject> _vehiclesInIntersection = new();
        private List<GameObject> _vehiclesQueue = new();

        private void Start()
        {
            _vehiclesQueue = new List<GameObject>();
            _vehiclesInIntersection = new List<GameObject>();

            if (_intersectionType == IntersectionType.TrafficLight)
                InvokeRepeating(nameof(SwitchLights), _lightsDuration, _lightsDuration);
        }

        private void OnTriggerEnter(Collider other)
        {
            //Check if vehicle is already in the list if yes abort
            //Also abort if we just started the scene (if vehicles inside colliders at start)
            if (IsAlreadyInIntersection(other.gameObject) || Time.timeSinceLevelLoad < 0.5f) return;

            if (other.CompareTag("AutonomousVehicle") && _intersectionType == IntersectionType.Stop)
                TriggerStop(other.gameObject);

            else if (other.CompareTag("AutonomousVehicle") && _intersectionType == IntersectionType.TrafficLight)
                TriggerLight(other.gameObject);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("AutonomousVehicle") && _intersectionType == IntersectionType.Stop)
                ExitStop(other.gameObject);

            else if (other.CompareTag("AutonomousVehicle") && _intersectionType == IntersectionType.TrafficLight)
                ExitLight(other.gameObject);
        }

        private void SwitchLights()
        {
            switch (_currentRedLightsGroup)
            {
                case 1:
                    _currentRedLightsGroup = 2;
                    break;
                case 2:
                    _currentRedLightsGroup = 1;
                    break;
            }

            //Wait few seconds after light transition before making the other car move (= orange light)
            Invoke(nameof(MoveVehiclesQueue), _orangeLightDuration);
        }

        private void TriggerStop(GameObject vehicle)
        {
            var vehicleAI = vehicle.GetComponent<VehicleAI>();

            //Depending on the waypoint threshold, the car can be either on the target segment or on the past segment
            var vehicleSegment = vehicleAI.GetSegmentVehicleIsIn();

            if (!IsPrioritySegment(vehicleSegment))
            {
                if (_vehiclesQueue.Count > 0 || _vehiclesInIntersection.Count > 0)
                {
                    vehicleAI._vehicleStatus = Status.Stop;
                    _vehiclesQueue.Add(vehicle);
                }
                else
                {
                    _vehiclesInIntersection.Add(vehicle);
                    vehicleAI._vehicleStatus = Status.SlowDown;
                }
            }
            else
            {
                vehicleAI._vehicleStatus = Status.SlowDown;
                _vehiclesInIntersection.Add(vehicle);
            }
        }

        private void ExitStop(GameObject vehicle)
        {
            vehicle.GetComponent<VehicleAI>()._vehicleStatus = Status.Go;
            _vehiclesInIntersection.Remove(vehicle);
            _vehiclesQueue.Remove(vehicle);

            if (_vehiclesQueue.Count > 0 && _vehiclesInIntersection.Count == 0)
                _vehiclesQueue[0].GetComponent<VehicleAI>()._vehicleStatus = Status.Go;
        }

        private void TriggerLight(GameObject vehicle)
        {
            var vehicleAI = vehicle.GetComponent<VehicleAI>();
            var vehicleSegment = vehicleAI.GetSegmentVehicleIsIn();

            if (IsRedLightSegment(vehicleSegment))
            {
                vehicleAI._vehicleStatus = Status.Stop;
                _vehiclesQueue.Add(vehicle);
            }
            else
            {
                vehicleAI._vehicleStatus = Status.Go;
            }
        }

        private void ExitLight(GameObject vehicle)
        {
            vehicle.GetComponent<VehicleAI>()._vehicleStatus = Status.Go;
        }

        private bool IsRedLightSegment(int vehicleSegment)
        {
            if (_currentRedLightsGroup == 1)
                return _lightsNbr1.Any(segment => segment.Id == vehicleSegment);

            return _lightsNbr2.Any(segment => segment.Id == vehicleSegment);
        }

        private void MoveVehiclesQueue()
        {
            //Move all vehicles in queue
            var nVehiclesQueue = new List<GameObject>(_vehiclesQueue);
            foreach (var vehicle in _vehiclesQueue)
            {
                var vehicleSegment = vehicle.GetComponent<VehicleAI>().GetSegmentVehicleIsIn();
                if (!IsRedLightSegment(vehicleSegment))
                {
                    vehicle.GetComponent<VehicleAI>()._vehicleStatus = Status.Go;
                    nVehiclesQueue.Remove(vehicle);
                }
            }

            _vehiclesQueue = nVehiclesQueue;
        }

        private bool IsPrioritySegment(int vehicleSegment)
        {
            foreach (var s in _prioritySegments)
            {
                if (vehicleSegment == s.Id)
                    return true;
            }

            return false;
        }

        private bool IsAlreadyInIntersection(GameObject target)
        {
            foreach (var vehicle in _vehiclesInIntersection)
            {
                if (vehicle.GetInstanceID() == target.GetInstanceID())
                    return true;
            }

            foreach (var vehicle in _vehiclesQueue)
            {
                if (vehicle.GetInstanceID() == target.GetInstanceID())
                    return true;
            }

            return false;
        }

        public void SaveIntersectionStatus()
        {
            _memVehiclesQueue = _vehiclesQueue;
            _memVehiclesInIntersection = _vehiclesInIntersection;
        }

        public void ResumeIntersectionStatus()
        {
            foreach (var v in _vehiclesInIntersection)
            {
                foreach (var v2 in _memVehiclesInIntersection)
                {
                    if (v.GetInstanceID() == v2.GetInstanceID())
                    {
                        v.GetComponent<VehicleAI>()._vehicleStatus = v2.GetComponent<VehicleAI>()._vehicleStatus;
                        break;
                    }
                }
            }

            foreach (var v in _vehiclesQueue)
            {
                foreach (var v2 in _memVehiclesQueue)
                {
                    if (v.GetInstanceID() == v2.GetInstanceID())
                    {
                        v.GetComponent<VehicleAI>()._vehicleStatus = v2.GetComponent<VehicleAI>()._vehicleStatus;
                        break;
                    }
                }
            }
        }
    }
}