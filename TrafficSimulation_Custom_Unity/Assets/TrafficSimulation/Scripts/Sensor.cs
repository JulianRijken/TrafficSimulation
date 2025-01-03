using UnityEngine;

namespace TrafficSimulation
{
    public class Sensor : MonoBehaviour
    {
        [SerializeField] private Vector2 _size = new Vector2(1.0f, 1.0f);
        [SerializeField] private LayerMask _layerMask = 0;
        
        public bool Sense(float distance, out RaycastHit hit)
        {
            return Physics.BoxCast(transform.position, _size, transform.forward, out hit, transform.rotation, distance,_layerMask );
        }
    }
}
