using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrafficSimulation
{
    public class Intersection : MonoBehaviour
    {
        [SerializeField] private TrafficSystem _trafficSystem;
        [SerializeField] private PriorityType _priorityType = PriorityType.FirstComeFirstServe;
        [SerializeField] private bool _allowTurnWhenOccupied = false;
        [SerializeField] private bool _earlyExitAllowed = false;
        
        private List<Turn> _turns = new();
        List<IntersectionAgent> _agentsInIntersecion = new();

        public bool EarlyExitAllowed => _earlyExitAllowed;

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

        
        private enum PriorityType
        {
            FirstComeFirstServe,
            RightHand,
            LeftHand,
        }

        public enum TurnState
        {
            Clear,
            Occupied,
            Blocked
        }
        
        private void Start()
        {
            SetupTurns();
        }

        private void OnDrawGizmos()
        {

            foreach (var turn in _turns)
            {
                Gizmos.color = turn.State switch
                {
                    TurnState.Clear => Color.green,
                    TurnState.Occupied => Color.blue,
                    TurnState.Blocked => Color.red,
                    _ => throw new ArgumentOutOfRangeException()
                };

                // Draw turn
                {
                    float heightOffset = 0.5f;
                    Vector3 fromPosition = turn.From.Waypoints.Last().transform.position;
                    Vector3 toPosition = turn.To.Waypoints.First().transform.position;
                    fromPosition.y += heightOffset;
                    toPosition.y += heightOffset;
                    Gizmos.DrawLine(fromPosition, toPosition);
                    var arrowCenter = Vector3.Lerp(fromPosition, toPosition, 0.5f);
                    MathExtensions.DrawArrowTip(arrowCenter, toPosition - fromPosition);
                }

                Gizmos.color = Color.black;
                // Draw Direction
                {
                    float heightOffset = 1.0f;
                    Vector3 fromPosition = turn.From.Waypoints.Last().transform.position;
                    fromPosition.y += heightOffset;
                    MathExtensions.DrawArrowTip(fromPosition, turn.Direction);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TrafficAgent agent = other.gameObject.GetComponentInParent<TrafficAgent>();
            if (agent != null)
                OnAgentEnterIntersection(agent);
        }
        
        private void OnTriggerExit(Collider other)
        {
            TrafficAgent agent = other.gameObject.GetComponentInParent<TrafficAgent>();
            if (agent != null)
                OnAgentExitIntersection(agent);
        }
        
        
        private void UpdateTurnStates()
        {
            var movingAgents = _agentsInIntersecion.Where(c => c.Agent.IntersectionState == TrafficAgent.IntersectionStateType.Moving).ToList();

            foreach (var turn in _turns)
            {
                turn.State = TurnState.Clear;
                
                foreach (var movingAgent in movingAgents)
                {
                    // When the moving car is making the same turn
                    if(turn.Equals(movingAgent.Turn))
                    {
                        turn.State = TurnState.Occupied;
                        break;
                    }
                    
                    // When the moving car is crossing the turn
                    if(turn.CrossingTurns.Contains(movingAgent.Turn))
                    {
                        turn.State = TurnState.Blocked;
                        break;
                    }
                }
            }
        }
        
        
        public void RemoveAgent(TrafficAgent agent)
        {
            if (_agentsInIntersecion.Any(c => c.Agent.Equals(agent)))
            {
                _agentsInIntersecion.Remove(_agentsInIntersecion.First(c => c.Agent.Equals(agent)));
                UpdateTurnStates();
            }
            
            TryMoveAgents();
        }
        
        private void TryMoveAgents()
        {
            UpdateTurnStates();

            var waitingAgents = _agentsInIntersecion.Where(agent => agent.Agent.IntersectionState == TrafficAgent.IntersectionStateType.Waiting).ToList();

            if (_priorityType == PriorityType.RightHand)
            {
                waitingAgents.Sort((a, b) =>
                {
                    float signedAngle = Vector3.SignedAngle(a.Turn.Direction, b.Turn.Direction, Vector3.up);
                    return signedAngle < 0 ? 1 : -1;
                });
            }
            else if (_priorityType == PriorityType.LeftHand)
            {
                waitingAgents.Sort((a, b) =>
                {
                    float signedAngle = Vector3.SignedAngle(a.Turn.Direction, b.Turn.Direction, Vector3.up);
                    return signedAngle < 0 ? -1 : 1;
                });
            }

            foreach (var waitingCar in waitingAgents)
            {
                bool canMove;
                if(_allowTurnWhenOccupied)
                    canMove = waitingCar.Turn.State != TurnState.Blocked;
                else
                    canMove = waitingCar.Turn.State == TurnState.Clear;

                if (canMove)
                {
                    waitingCar.Agent.IntersectionState = TrafficAgent.IntersectionStateType.Moving;
                    UpdateTurnStates();
                }
                else
                {
                    // When the intersection has priority force all other cars to wait
                    if(_priorityType != PriorityType.FirstComeFirstServe)
                        break;
                }
            }
            
            //      if(_priorityType == PriorityType.FirstComeFirstServe)
            // {
            //     var waitingAgents = _agentsInIntersecion.Where(agent => agent.Agent.IntersectionState == TrafficAgent.IntersectionStateType.Waiting).ToList();
            //
            //     foreach (var waitingCar in waitingAgents)
            //     {
            //         bool canMove;
            //         if(_allowTurnWhenOccupied)
            //             canMove = waitingCar.Turn.State != TurnState.Blocked;
            //         else
            //             canMove = waitingCar.Turn.State == TurnState.Clear;
            //
            //         if (canMove)
            //         {
            //             waitingCar.Agent.IntersectionState = TrafficAgent.IntersectionStateType.Moving;
            //             UpdateTurnStates();
            //         }
            //     }
            // }
            // else if(_priorityType is PriorityType.RightHand or PriorityType.LeftHand)
            // {
            //     var waitingAgents = _agentsInIntersecion.Where(agent => agent.Agent.IntersectionState == TrafficAgent.IntersectionStateType.Waiting).ToList();
            //
            //     if(_priorityType == PriorityType.RightHand)
            //     {
            //         waitingAgents.Sort((a, b) =>
            //         {
            //             float signedAngle = Vector3.SignedAngle(a.Turn.Direction, b.Turn.Direction, Vector3.up);
            //             return signedAngle < 0 ? 1 : -1;
            //         });
            //     }
            //     else if(_priorityType == PriorityType.LeftHand)
            //     {
            //         waitingAgents.Sort((a, b) =>
            //         {
            //             float signedAngle = Vector3.SignedAngle(a.Turn.Direction, b.Turn.Direction, Vector3.up);
            //             return signedAngle < 0 ? -1 : 1;
            //         });
            //     }
            //     
            //     foreach (var waitingCar in waitingAgents)
            //     {
            //         bool canMove;
            //         if(_allowTurnWhenOccupied)
            //             canMove = waitingCar.Turn.State != TurnState.Blocked;
            //         else
            //             canMove = waitingCar.Turn.State == TurnState.Clear;
            //
            //         if (canMove)
            //         {
            //             waitingCar.Agent.IntersectionState = TrafficAgent.IntersectionStateType.Moving;
            //             UpdateTurnStates();
            //         }
            //         else
            //         {
            //             break;
            //         }
            //     }
            // }
            
          
            
            
            // while(true)
            // {
            //     // Update waiting cars
            //     List<IntersectionAgent> waitingCarsWithFreeTurn;
            //
            //     if (_allowTurnWhenOccupied)
            //     {
            //         waitingCarsWithFreeTurn = _carsInIntersection.Where(agent => agent.Turn.State != TurnState.Blocked && agent.Agent.IntersectionState == TrafficAgent.IntersectionStateType.Waiting).ToList();
            //     }
            //     else
            //     {
            //         waitingCarsWithFreeTurn = _carsInIntersection.Where(agent => agent.Turn.State == TurnState.Clear && agent.Agent.IntersectionState == TrafficAgent.IntersectionStateType.Waiting).ToList();
            //     }
            //     
            //     if(waitingCarsWithFreeTurn.Count == 0)
            //         break;
            //     
            //     if(_priorityType == PriorityType.RightHand)
            //     {
            //         waitingCarsWithFreeTurn.Sort((a, b) =>
            //         {
            //             float signedAngle = Vector3.SignedAngle(a.Turn.Direction, b.Turn.Direction, Vector3.up);
            //             return signedAngle < 0 ? 1 : -1;
            //         });
            //     }
            //     else if(_priorityType == PriorityType.LeftHand)
            //     {
            //         waitingCarsWithFreeTurn.Sort((a, b) =>
            //         {
            //             float signedAngle = Vector3.SignedAngle(a.Turn.Direction, b.Turn.Direction, Vector3.up);
            //             return signedAngle < 0 ? -1 : 1;
            //         });
            //     }
            //     
            //     waitingCarsWithFreeTurn.First().Agent.IntersectionState = TrafficAgent.IntersectionStateType.Moving;
            //     UpdateTurnStates();
            // }
        }
        
        private void OnAgentEnterIntersection(TrafficAgent agent)
        {
            var intersectionAgent = new IntersectionAgent
            {
                Agent = agent,
                Turn = _turns.FirstOrDefault(turn => turn.From.Equals(agent.CurrentSegment) && turn.To.Equals(agent.NextSegment))
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
            agent.IntersectionState = TrafficAgent.IntersectionStateType.Waiting;
            _agentsInIntersecion.Add(intersectionAgent);
            
            TryMoveAgents();
        }
        
        private void OnAgentExitIntersection(TrafficAgent agent)
        {
            // Ignore the agent if it is not in the intersection
            if (agent.CurrentIntersection != this)
                return;
            
            agent.IntersectionState = TrafficAgent.IntersectionStateType.None;
            agent.CurrentIntersection = null;
            _agentsInIntersecion.Remove(_agentsInIntersecion.First(c => c.Agent.Equals(agent)));
            
            TryMoveAgents();
        }

        
        private void SetupTurns()
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
                    
                    var fromSegmentDirection = fromSegment.Waypoints[^1].transform.position - fromSegment.Waypoints[^2].transform.position;
                    var connectedSegmentDirection = connectedSegment.Waypoints[1].transform.position - connectedSegment.Waypoints[0].transform.position;
                    var angle = Vector3.Angle(fromSegmentDirection, connectedSegmentDirection);
                    
                    var turn = new Turn
                    {
                        From = fromSegment,
                        To = connectedSegment,
                        Angle = angle,
                        Distance = Vector3.Distance(fromSegment.Waypoints.Last().transform.position, connectedSegment.Waypoints.First().transform.position),
                        // Direction = (connectedSegment.Waypoints.First().transform.position - fromSegment.Waypoints.Last().transform.position).normalized
                        Direction = (fromSegment.Waypoints[^1].Position - fromSegment.Waypoints[^2].Position).normalized
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
                    // Ignore self
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
                    var line2Start = ToXZ(otherTurn.From.Waypoints.Last().transform.position);
                    var line2End = ToXZ(otherTurn.To.Waypoints.First().transform.position);
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
