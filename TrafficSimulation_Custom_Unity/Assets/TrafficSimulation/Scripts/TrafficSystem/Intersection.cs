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
        [SerializeField] private TurnPriority _turnPriority = TurnPriority.RightTurn;
        [SerializeField] private DirectionPriority _directionPriority = DirectionPriority.RightDirection;

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
            Fill,
            FirstComeFirstServe,
            DirectionAndTurn,
        }
        
        private enum DirectionPriority
        {
            RightDirection,
            LeftDirection
        }
        
        private enum TurnPriority
        {
            LeftTurn,
            RightTurn
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
            
            if (_priorityType == PriorityType.DirectionAndTurn)
            {
                waitingAgents.Sort((a, b) =>
                {
                    // If agent is on the same segment sort by distance to the end of the segment
                    if(a.Turn.From.Equals(b.Turn.From))
                        return a.Agent.CurrentSample.DistanceToSegmentEnd < b.Agent.CurrentSample.DistanceToSegmentEnd ? -1 : 1;
                    
                    float directionDot = Vector3.Dot(a.Turn.Direction, b.Turn.Direction);
                    bool isOppositeDirection = directionDot < -0.5f;
                    
                    if (isOppositeDirection)
                    {
                        if (_turnPriority == TurnPriority.LeftTurn)
                            return  a.Turn.Angle > b.Turn.Angle ? 1 : -1;
                        else
                            return  a.Turn.Angle < b.Turn.Angle ? 1 : -1;
                    }
                    
                    // When the direction is not opposite sort by right hand rule
                    float signedAngle = Vector3.SignedAngle(a.Turn.Direction, b.Turn.Direction, Vector3.up);
                    if (_directionPriority == DirectionPriority.RightDirection)
                        return signedAngle < 0 ? 1 : -1;
                    else
                        return signedAngle > 0 ? 1 : -1;
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
                    // Don't allow more cars to move if the turn is blocked
                    if(_priorityType is PriorityType.FirstComeFirstServe or PriorityType.DirectionAndTurn)
                        break;
                }
            }
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
                    
                    var fromSegmentDirection = (fromSegment.Waypoints[^1].Position - fromSegment.Waypoints[^2].Position).normalized;
                    var connectedSegmentDirection = connectedSegment.Waypoints[1].Position - connectedSegment.Waypoints[0].Position;
                    var angle = Vector3.SignedAngle(fromSegmentDirection, connectedSegmentDirection,Vector3.up);
                    
                    var turn = new Turn
                    {
                        From = fromSegment,
                        To = connectedSegment,
                        Angle = angle,
                        Distance = Vector3.Distance(fromSegment.Waypoints.Last().transform.position, connectedSegment.Waypoints.First().transform.position),
                        Direction = fromSegmentDirection
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
