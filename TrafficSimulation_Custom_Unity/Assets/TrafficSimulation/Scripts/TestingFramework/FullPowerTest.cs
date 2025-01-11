using UnityEngine;

namespace TrafficSimulation
{
    public class FullPowerTest : MonoBehaviour
    {
        [SerializeField] private TrafficAgent _agent;

        private void Update()
        {
            if (_agent.CurrentSample.AlphaAlongSegment > 0.5f)
            {
                _agent.CarBehaviour.ThrottleInput = 0.0f;
                _agent.CarBehaviour.BreakInput = 1.0f;
            }
            else
            {
                _agent.CarBehaviour.ThrottleInput = 1.0f;
                _agent.CarBehaviour.BreakInput = 0.0f;
            }
        }
    }
}
