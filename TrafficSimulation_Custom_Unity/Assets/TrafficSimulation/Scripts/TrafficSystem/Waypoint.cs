using UnityEngine;

namespace TrafficSimulation
{
    public class Waypoint : MonoBehaviour
    {
        [HideInInspector] public Segment Segment;

        public void Refresh(int newId, Segment newSegment)
        {
            Segment = newSegment;
            name = "Waypoint-" + newId;
            tag = "Waypoint";
            gameObject.layer = 0;
        }
    }
}