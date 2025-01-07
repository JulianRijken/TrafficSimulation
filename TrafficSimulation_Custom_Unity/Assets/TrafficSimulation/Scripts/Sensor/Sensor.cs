using UnityEngine;

namespace TrafficSimulation
{
    public abstract class Sensor : MonoBehaviour
    {
        protected TrafficAgent _agent;
        
        [SerializeField] protected Vector2 _size = new Vector2(0.7f, 0.3f);
        [SerializeField] protected LayerMask _layerMask = 0;

        public struct Result
        {
            public float Distance;
            public Vector3 Velocity;
        }

        protected void Awake()
        {
            _agent = GetComponentInParent<TrafficAgent>();
        }

        public abstract Result Sense(float distance);
    }
}