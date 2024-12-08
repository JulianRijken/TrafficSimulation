using System;
using UnityEngine;

namespace TrafficSimulation
{
    public class Sensor : MonoBehaviour
    {
        // TODO: implement angle, can still use box cast but just ignore based on angle
        
        [SerializeField] private Vector2 _sensorSize = new Vector2(1.0f, 1.0f);

        public bool Sense(float distance, out RaycastHit hit)
        {
            bool hasHit = Physics.BoxCast(
                transform.position,
                _sensorSize,
                transform.forward,
                out hit,
                transform.rotation,
                distance
            );

            // TODO: This assumes the parent is the car!!
            if (! hasHit || hit.collider.gameObject.transform.IsChildOf(transform.parent))
            {
                hit = default;
                return false;
            }

            return hasHit;
        }
    }
}
