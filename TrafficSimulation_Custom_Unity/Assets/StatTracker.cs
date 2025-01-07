using System;
using UnityEngine;

namespace TrafficSimulation
{
    public class StatTracker : MonoBehaviour
    {
        public event Action OnCollision;
  
        private void OnCollisionEnter(Collision other)
        {
            if(other.gameObject.GetComponent<CarBehaviour>() != null)
                OnCollision?.Invoke();
        }
    }
}