using UnityEngine;

namespace TrafficSimulation
{
    [DefaultExecutionOrder(-1)]
    public class TrafficAgentPower : MonoBehaviour
    {
        [SerializeField] private TrafficAgent _agent;
        
        [SerializeField] private float _targetSpeed = 10.0f;
        
        private void Start()
        {
            if(_agent == null)
                _agent = GetComponent<TrafficAgent>();
        }
        
        private void Update()
        {
            // var sample = _agent.CurrentSegment.GetSampleFromPosition(_agent.CarBehaviour.Position);
            // if(sample.AlphaAlongSegment >= 1.0f)
            //     return;
            
            if (_agent.CurrentSegment == null)
            {
                _agent.CarBehaviour.ThrottleInput = 0;
                return;
            }
            
            _agent.CarBehaviour.ThrottleInput = _agent.CarBehaviour.ForwardSpeed < _targetSpeed ? 1 : 0;
        }
    }
}

