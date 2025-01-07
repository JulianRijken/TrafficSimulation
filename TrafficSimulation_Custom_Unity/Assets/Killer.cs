using System;
using UnityEngine;

namespace TrafficSimulation
{
    public class Killer : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            CarBehaviour carBehaviour = other.GetComponentInParent<CarBehaviour>();
            if(carBehaviour != null)
            {
                Destroy(carBehaviour.gameObject);
            }
        }
    }
}

