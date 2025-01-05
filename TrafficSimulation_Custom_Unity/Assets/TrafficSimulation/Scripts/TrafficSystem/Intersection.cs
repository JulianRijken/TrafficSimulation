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
            public float Angle;
            public float Distance;
        }
        
        [Serializable]
        public struct IntersectionCar
        {
            public TrafficAgent Car;
            public Turn Turn;
        }
        

        [SerializeField] private TrafficSystem _trafficSystem;
        
        private List<Turn> _turns = new();
        List<IntersectionCar> _carsInIntersection = new();

        // TODO: Could be optimized in to two lists, one for waiting and one for moving cars
        public List<IntersectionCar> CarsWaiting => _carsInIntersection.Where(c => c.Car.IntersectionState == TrafficAgent.IntersectionStateType.Waiting).ToList();
        public List<IntersectionCar> CarsMoving => _carsInIntersection.Where(c => c.Car.IntersectionState == TrafficAgent.IntersectionStateType.Moving).ToList();
        
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

                foreach (var movingCar in CarsMoving)
                {
                    if(turn.Equals(movingCar.Turn))
                    {
                        Gizmos.color = Color.red;
                        break;
                    }
                }
                
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
            TrafficAgent carController = other.gameObject.GetComponentInParent<TrafficAgent>();
            if (carController != null)
                OnCarEnterIntersection(carController);
        }
        
        private void OnTriggerExit(Collider other)
        {
            TrafficAgent carController = other.gameObject.GetComponentInParent<TrafficAgent>();
            if (carController != null)
                OnCarExitIntersection(carController);
        }


        private void TryMoveCars()
        {
            // Simple, no cars in intersection
            if(_carsInIntersection.Count == 0)
                return;

            foreach (var waitingCar in CarsWaiting)
            {
                if(CarsMoving.Count == 0)
                {
                    waitingCar.Car.IntersectionState = TrafficAgent.IntersectionStateType.Moving;
                    return;
                }
                
                foreach (var movingCar in CarsMoving)
                {
                    if(waitingCar.Turn.CrossingConnections == null)
                        throw new Exception("Crossing connections is null, intersection probably does not cover all connections");
                    
                    // Wait for moving car to pass
                    if (waitingCar.Turn.CrossingConnections.Contains(movingCar.Turn.Connection))
                        continue;
                    
                    waitingCar.Car.IntersectionState = TrafficAgent.IntersectionStateType.Moving;
                    return;
                }
            }
        }
        
        private void OnCarEnterIntersection(TrafficAgent car)
        {
            // Add car to the list of cars that entered the intersection
            // Debug.Log("Vehicle entered intersection");
            _carsInIntersection.Add(new IntersectionCar
            {
                Car = car,
                Turn = _turns.FirstOrDefault(t => IsCarMakingTurn(t, car))
            });
            car.IntersectionState = TrafficAgent.IntersectionStateType.Waiting;

            TryMoveCars();
        }
        
        private void OnCarExitIntersection(TrafficAgent car)
        {
            // Remove car to the list of cars that exited the intersection
            // Debug.Log("Vehicle exited intersection");
            car.IntersectionState = TrafficAgent.IntersectionStateType.None;
            _carsInIntersection.Remove(_carsInIntersection.First(c => c.Car.Equals(car)));
            
            TryMoveCars();
        }

        private bool IsCarMakingTurn(Turn turn, TrafficAgent car)
        {
            return turn.Connection.From.Equals(car.CurrentSegment) && turn.Connection.To.Equals(car.NextSegment);
        }

        private void SetupTurn()
        {
            var boxCollider = GetComponent<BoxCollider>();

            // Create turns
            foreach (var fromSegment in _trafficSystem.Segments)
            {
                foreach (var connectedSegment in fromSegment.ConnectedSegments)
                {
                    if (!boxCollider.bounds.Contains(connectedSegment.Waypoints.First().transform.position))
                        continue;
                    
                    if (!boxCollider.bounds.Contains(fromSegment.Waypoints.Last().transform.position))
                        continue;
                    
                    // TODO: Assumes segments are at least 2 waypoints long
                    Vector3 fromSegmentDirection = fromSegment.Waypoints[^1].transform.position - fromSegment.Waypoints[^2].transform.position;
                    Vector3 connectedSegmentDirection = connectedSegment.Waypoints[1].transform.position - connectedSegment.Waypoints[0].transform.position;
                    float angle = Vector3.Angle(fromSegmentDirection, connectedSegmentDirection);
                    
                    var turn = new Turn
                    {
                        Connection = new Connection
                        {
                            From = fromSegment,
                            To = connectedSegment
                        },
                        CrossingConnections = new List<Connection>(),
                        Angle = angle,
                        Distance = Vector3.Distance(fromSegment.Waypoints.Last().transform.position, connectedSegment.Waypoints.First().transform.position)
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
