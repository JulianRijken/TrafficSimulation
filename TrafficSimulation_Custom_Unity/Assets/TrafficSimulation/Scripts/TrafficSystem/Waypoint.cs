using UnityEngine;
using UnityEngine.Serialization;

namespace TrafficSimulation
{
    public class Waypoint : MonoBehaviour
    {
        public Segment Segment;
        public Waypoint NextWaypoint;

        [SerializeField] public float SpeedLimitKph = 50.0f;
        
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