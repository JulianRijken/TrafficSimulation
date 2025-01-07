using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrafficSimulation
{
    public class Intersection : MonoBehaviour
    {
        public enum PriorityType
        {
            RightHand,
            LeftHand,
            FirstComeFirstServe
        }
        
        [Serializable]
        public class Turn
        {
            public float Angle;
            public float Distance;
            
            public Segment From;
            public Segment To;

            public Vector3 Direction;
            
            public List<Turn> CrossingTurns = new();
            public TurnState State = TurnState.Clear;
        }
        
        [Serializable]
        public class IntersectionAgent
        {
            public TrafficAgent Agent;
            public Turn Turn;
        }

        public enum TurnState
        {
            Clear,
            Occupied,
            Blocked
        }
        

        [SerializeField] private TrafficSystem _trafficSystem;
        
        
        private List<Turn> _turns = new();
        List<IntersectionAgent> _carsInIntersection = new();

        // TODO: Could be optimized in to two lists, one for waiting and one for moving cars
        public List<IntersectionAgent> CarsWaiting => _carsInIntersection.Where(c => c.Agent.IntersectionState == TrafficAgent.IntersectionStateType.Waiting).ToList();
        public List<IntersectionAgent> CarsMoving => _carsInIntersection.Where(c => c.Agent.IntersectionState == TrafficAgent.IntersectionStateType.Moving).ToList();
        
        private void Start()
        {
            SetupTurn();
        }

        private void OnDrawGizmos()
        {
            float heightOffset = 0.5f;
            
            foreach (var turn in _turns)
            {
                Gizmos.color = turn.State switch
                {
                    TurnState.Clear => Color.green,
                    TurnState.Occupied => Color.blue,
                    TurnState.Blocked => Color.red,
                    _ => throw new ArgumentOutOfRangeException()
                };

                Vector3 fromPosition = turn.From.Waypoints.Last().transform.position;
                Vector3 toPosition = turn.To.Waypoints.First().transform.position;
                fromPosition.y += heightOffset;
                toPosition.y += heightOffset;
                Gizmos.DrawLine(fromPosition,toPosition);
                var arrowCenter = Vector3.Lerp(fromPosition, toPosition,0.5f);
                MathExtensions.DrawArrowTip(arrowCenter, toPosition - fromPosition);
            }
        }

        
        private void UpdateTurnStates()
        {
            foreach (Turn turn in _turns)
            {
                turn.State = TurnState.Clear;
        
                foreach (var movingCar in CarsMoving)
                {
                    if(turn.Equals(movingCar.Turn))
                    {
                        turn.State = TurnState.Occupied;
                        break;
                    }
                    
                    if(turn.CrossingTurns.Contains(movingCar.Turn))
                    {
                        turn.State = TurnState.Blocked;
                        break;
                    }
                }
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            TrafficAgent agent = other.gameObject.GetComponentInParent<TrafficAgent>();
            if (agent != null)
                OnCarEnterIntersection(agent);
        }
        
        private void OnTriggerExit(Collider other)
        {
            TrafficAgent agent = other.gameObject.GetComponentInParent<TrafficAgent>();
            if (agent != null)
                OnCarExitIntersection(agent);
        }
        
        public void RemoveAgent(TrafficAgent agent)
        {
            if (_carsInIntersection.Any(c => c.Agent.Equals(agent)))
            {
                _carsInIntersection.Remove(_carsInIntersection.First(c => c.Agent.Equals(agent)));
                UpdateTurnStates();
            }
            
            agent.CurrentIntersection = null;
        }
        
        private void TryMoveCars()
        {
            // Get all agents with free turn
            List<(Turn turn, IntersectionAgent agent)> agentsWithFreeTurn = new();
            foreach (var waitingCar in CarsWaiting)
            {
                if (waitingCar.Turn.State == TurnState.Clear)
                    agentsWithFreeTurn.Add((waitingCar.Turn, waitingCar));
            }
            
            // If no agents with free turn, return
            if (agentsWithFreeTurn.Count == 0)
                return;
            
            // Sort agents by right hand priority
            agentsWithFreeTurn.Sort((a, b) =>
            {
                float signedAngle = Vector3.SignedAngle(a.turn.Direction, b.turn.Direction, Vector3.up);
                return signedAngle < 0 ? 1 : -1;
            });
            
            // Move agent with priority
            agentsWithFreeTurn.First().agent.Agent.IntersectionState = TrafficAgent.IntersectionStateType.Moving;
            UpdateTurnStates();
        }
        
        private void OnCarEnterIntersection(TrafficAgent agent)
        {
            
            
            var intersectionAgent = new IntersectionAgent
            {
                Agent = agent,
                Turn = _turns.FirstOrDefault(turn => IsCarMakingTurn(turn, agent))
            };

            // Ignore the agent if it is not making a turn
            if (intersectionAgent.Turn == null)
                return;

            // Remove from old intersection
            if (agent.CurrentIntersection != null)
            {
                agent.CurrentIntersection.RemoveAgent(agent);
                Debug.LogWarning("Agent moved to new intersection without leaving the old one");
            }

            agent.CurrentIntersection = this;
            _carsInIntersection.Add(intersectionAgent);
            
            
            agent.IntersectionState = TrafficAgent.IntersectionStateType.Waiting;
            TryMoveCars();
        }
        
        private void OnCarExitIntersection(TrafficAgent agent)
        {
            // Ignore the agent if it is not in the intersection
            if(!_carsInIntersection.Any(c => c.Agent.Equals(agent)))
                return;
            
            if (agent.CurrentIntersection == this)
            {
                agent.IntersectionState = TrafficAgent.IntersectionStateType.None;
                agent.CurrentIntersection = null;
            }
            
            _carsInIntersection.Remove(_carsInIntersection.First(c => c.Agent.Equals(agent)));
            UpdateTurnStates();
            
            TryMoveCars();
        }

        private bool IsCarMakingTurn(Turn turn, TrafficAgent car)
        {
            return turn.From.Equals(car.CurrentSegment) && turn.To.Equals(car.NextSegment);
        }

        private void SetupTurn()
        {
            var boxCollider = GetComponent<BoxCollider>();

            // Create turns
            _turns.Clear();
            foreach (var fromSegment in _trafficSystem.Segments)
            {
                foreach (var connectedSegment in fromSegment.ConnectedSegments)
                {
                    if (!boxCollider.bounds.Contains(connectedSegment.StartPosition))
                        continue;
                    
                    if (!boxCollider.bounds.Contains(fromSegment.EndPosition))
                        continue;
                    
                    // TODO: Assumes segments are at least 2 waypoints long
                    Vector3 fromSegmentDirection = fromSegment.Waypoints[^1].transform.position - fromSegment.Waypoints[^2].transform.position;
                    Vector3 connectedSegmentDirection = connectedSegment.Waypoints[1].transform.position - connectedSegment.Waypoints[0].transform.position;
                    float angle = Vector3.Angle(fromSegmentDirection, connectedSegmentDirection);
                    
                    var turn = new Turn
                    {
                        From = fromSegment,
                        To = connectedSegment,
                        Angle = angle,
                        Distance = Vector3.Distance(fromSegment.Waypoints.Last().transform.position, connectedSegment.Waypoints.First().transform.position),
                        Direction = (connectedSegment.Waypoints.First().transform.position - fromSegment.Waypoints.Last().transform.position).normalized
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
                
                Vector2 line1Start = ToXZ(turn.From.Waypoints.Last().transform.position);
                Vector2 line1End = ToXZ(turn.To.Waypoints.First().transform.position);
                
                foreach (var otherTurn in _turns)
                {
                    if (turn.Equals(otherTurn))
                        continue;
                    
                    // We also ignore turns that start from the same segment
                    if(turn.From.Equals(otherTurn.From))
                        continue;
                    
                    // When the end of the turn is the same they are crossing
                    if(turn.To.Equals(otherTurn.To))
                    {
                        turn.CrossingTurns.Add(otherTurn);
                        continue;
                    }

                    // Check if lines intersect
                    Vector2 line2Start = ToXZ(otherTurn.From.Waypoints.Last().transform.position);
                    Vector2 line2End = ToXZ(otherTurn.To.Waypoints.First().transform.position);
                    if (MathExtensions.LineIntersect2D(line1Start, line1End, line2Start, line2End))
                    {
                        turn.CrossingTurns.Add(otherTurn);
                        continue;
                    }
                }
            }
        }
    }
}
