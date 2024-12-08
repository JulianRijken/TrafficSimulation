using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrafficSimulation
{
    public class Intersection : MonoBehaviour
    {
        [Serializable]
        public struct Connection
        {
            public Segment From;
            public Segment To;
        }
        
        [Serializable]
        public struct Turn
        {
            public Connection Connection;
            public List<Connection> CrossingConnections;
        }
        
        [Serializable]
        public struct IntersectionCar
        {
            public CarControllerAI Car;
            public Turn Turn;
        }
        

        [SerializeField] private TrafficSystem _trafficSystem;
        
        private List<Turn> _turns = new();
        List<IntersectionCar> _carsInIntersection = new();

        // TODO: Could be optimized in to two lists, one for waiting and one for moving cars
        public List<IntersectionCar> CarsWaiting => _carsInIntersection.Where(c => c.Car.IntersectionState == CarControllerAI.IntersectionStateType.Waiting).ToList();
        public List<IntersectionCar> CarsMoving => _carsInIntersection.Where(c => c.Car.IntersectionState == CarControllerAI.IntersectionStateType.Moving).ToList();
        
        private void Start()
        {
            SetupTurn();
        }

        private void OnDrawGizmos()
        {
            float heightOffset = 0.5f;
            Gizmos.color = Color.blue;
            
            foreach (var turn in _turns)
            {
                if(turn.Connection.From == null || turn.Connection.To == null)
                    continue;
                
                Vector3 fromPosition = turn.Connection.From.Waypoints.Last().transform.position;
                Vector3 toPosition = turn.Connection.To.Waypoints.First().transform.position;
                fromPosition.y += heightOffset;
                toPosition.y += heightOffset;
                Gizmos.DrawLine(fromPosition,toPosition);
                var arrowCenter = Vector3.Lerp(fromPosition, toPosition,0.5f);
                MathExtensions.DrawArrowTip(arrowCenter, toPosition - fromPosition);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            CarControllerAI carController = other.gameObject.GetComponentInParent<CarControllerAI>();
            if (carController != null)
                OnCarEnterIntersection(carController);
        }
        
        private void OnTriggerExit(Collider other)
        {
            CarControllerAI carController = other.gameObject.GetComponentInParent<CarControllerAI>();
            if (carController != null)
                OnCarExitIntersection(carController);
        }


        private void TryMoveCars()
        {
            // Simple, no cars in intersection
            if(_carsInIntersection.Count == 0)
                return;
            
            // Only one car in intersection, just move it
            if(_carsInIntersection.Count == 1)
            {
                _carsInIntersection[0].Car.IntersectionState = CarControllerAI.IntersectionStateType.Moving;
                return;
            }

            foreach (var waitingCar in CarsWaiting)
            {
                foreach (var movingCar in CarsMoving)
                {
                    // Wait for moving car to pass
                    if (waitingCar.Turn.CrossingConnections.Contains(movingCar.Turn.Connection))
                        continue;
                    
                    waitingCar.Car.IntersectionState = CarControllerAI.IntersectionStateType.Moving;
                    return;
                }
            }
            
        }
        
        
        private void OnCarEnterIntersection(CarControllerAI car)
        {
            // Add car to the list of cars that entered the intersection
            // Debug.Log("Vehicle entered intersection");
            _carsInIntersection.Add(new IntersectionCar
            {
                Car = car,
                Turn = _turns.FirstOrDefault(t => IsCarMakingTurn(t, car))
            });
            car.IntersectionState = CarControllerAI.IntersectionStateType.Waiting;
            
            TryMoveCars();
        }
        
        private void OnCarExitIntersection(CarControllerAI car)
        {
            // Remove car to the list of cars that exited the intersection
            // Debug.Log("Vehicle exited intersection");
            car.IntersectionState = CarControllerAI.IntersectionStateType.None;
            _carsInIntersection.Remove(_carsInIntersection.First(c => c.Car.Equals(car)));
            
            TryMoveCars();
        }

        private bool IsCarMakingTurn(Turn turn, CarControllerAI car)
        {
            return turn.Connection.From.Equals(car.CurrentSegment) && turn.Connection.To.Equals(car.NextSegment);
        }

        private void SetupTurn()
        {
            var boxCollider = GetComponent<BoxCollider>();

            // Create turns
            foreach (var segment in _trafficSystem.Segments)
            {
                foreach (var connectedSegment in segment.ConnectedSegments)
                {
                    if (!boxCollider.bounds.Contains(connectedSegment.Waypoints.First().transform.position))
                        continue;
                    
                    if (!boxCollider.bounds.Contains(segment.Waypoints.Last().transform.position))
                        continue;
                    
                    var turn = new Turn
                    {
                        Connection = new Connection
                        {
                            From = segment,
                            To = connectedSegment
                        },
                        CrossingConnections = new List<Connection>()
                    };
                    
                    _turns.Add(turn);
                }  
            }
            
            // Find crossing connections using line overlap
            foreach (var turn in _turns)
            {
                Vector2 ToXZ(Vector3 point)
                {
                    return new Vector2(point.x, point.z);
                }
                
                Vector2 line1Start = ToXZ(turn.Connection.From.Waypoints.Last().transform.position);
                Vector2 line1End = ToXZ(turn.Connection.To.Waypoints.First().transform.position);
                
                foreach (var otherTurn in _turns)
                {
                    if (turn.Equals(otherTurn))
                        continue;
                    
                    // We also ignore turns that start from the same segment
                    if(turn.Connection.From.Equals(otherTurn.Connection.From))
                        continue;

                    Vector2 line2Start = ToXZ(otherTurn.Connection.From.Waypoints.Last().transform.position);
                    Vector2 line2End = ToXZ(otherTurn.Connection.To.Waypoints.First().transform.position);

                    if (MathExtensions.LineIntersect2D(line1Start, line1End, line2Start, line2End))
                    {
                        turn.CrossingConnections.Add(otherTurn.Connection);
                    }
                }
            }
        }
    }
}
