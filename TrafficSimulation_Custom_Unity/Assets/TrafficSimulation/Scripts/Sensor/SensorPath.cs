using UnityEngine;

namespace TrafficSimulation
{
    public class SensorPath : Sensor
    {
        [SerializeField] private float _lookaheadDistance = 10.0f;

        
        private void OnDrawGizmos()
        {
            if(Application.isPlaying == false)
                return;

            if(_agent.CurrentSegment == null)
                return;
            
            int sampleCount = 10;
            for (int i = -sampleCount; i < sampleCount; i++)
            {
                float alpha = i / (float)sampleCount;
                float distance = alpha * _lookaheadDistance + _agent.CurrentSample.DistanceAlongSegment;
                var sample = _agent.CurrentSegment.SampleFromDistance(distance);
                
                Gizmos.color = Color.Lerp(Color.red, new Color(1.0f,0.0f,0.0f,0.5f),alpha);
                Gizmos.DrawSphere(sample.Position, 0.5f);
            }
        }

        public override Result Sense()
        {
            var result = new Result
            {
                Distance = float.MaxValue
            };
            

            
            return result;

            
            // ///////////////////////
            //
            // bool didHit = Physics.BoxCast(transform.position, _size, transform.forward, out hit, transform.rotation, distance,_layerMask );
            //
            // if (didHit == false)
            //     hit.distance = float.MaxValue;
            //
            // return didHit;
        }
    }
}
