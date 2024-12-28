using UnityEngine;

namespace TrafficSimulation
{
    public class Waypoint : MonoBehaviour
    {
        [HideInInspector] public Segment Segment;
        [HideInInspector] public Waypoint NextWaypoint;

        public Vector3 Position => transform.position;
        
        public void Refresh(int newId, Segment newSegment)
        {
            Segment = newSegment;
            name = "Waypoint-" + newId;
            tag = "Waypoint";
            gameObject.layer = 0;
        }
    }
}