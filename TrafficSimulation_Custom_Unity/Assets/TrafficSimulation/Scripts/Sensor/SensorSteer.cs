using UnityEngine;

namespace TrafficSimulation
{
    public class SensorSteer : Sensor
    {
        [SerializeField] private float _steeringAngleSmoothing = 10.0f;
        [SerializeField] private float _lookaheadDistance = 10.0f;
        [SerializeField] private float _steeringAnglePointCount;
        
        private Vector3 _turningCenter = Vector3.zero;
        private float _turningRadius = 0.0f;
        private float _steeringAngle;

        private Vector3 AngleToPoint(float angle)
        {
            Vector3 centerToAgent = _agent.CarBehaviour.Position - _turningCenter;
            float angleToAgent =  Mathf.Atan2(centerToAgent.z, centerToAgent.x);
            float angleOffset = angle * -Mathf.Sign(_turningRadius) + angleToAgent; 
            float radiusAbs = Mathf.Abs(_turningRadius);
        
            return new Vector3(
                _turningCenter.x + Mathf.Cos(angleOffset) * radiusAbs,
                _agent.CarBehaviour.Position.y,
                _turningCenter.z + Mathf.Sin(angleOffset) * radiusAbs);
        }
        
        private void OnDrawGizmos()
        {
            if(Application.isPlaying == false)
                return;
            
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            MathExtensions.DrawCircle(_turningCenter, Vector3.up, _turningRadius);
            Gizmos.DrawSphere(_turningCenter, 0.5f);
            
            int pointCount = 10;
            for (int i = 0; i < pointCount; i++)
            {
                float pointAlpha = i / (float)pointCount;
                float currentPointAngle = pointAlpha / Mathf.Abs(_turningRadius)  * _lookaheadDistance;
                
                Vector3 currentPoint = AngleToPoint(currentPointAngle);
            
                Gizmos.color = Color.Lerp(Color.red, new Color(1.0f,0.0f,0.0f,0.5f),pointAlpha);
                Gizmos.DrawSphere(currentPoint, 1.0f);
            }
        }

        public override Result Sense(float senseDistance)
        {
            var result = new Result
            {
                Distance = float.MaxValue
            };
            
            float steeringAngleTarget = _agent.CarBehaviour.SteeringAngleDegrees * Mathf.Deg2Rad;
            _steeringAngle = MathExtensions.LerpSmooth(_steeringAngle, steeringAngleTarget, Time.deltaTime, _steeringAngleSmoothing);
            
            Vector3 frontWheelCenter = _agent.CarBehaviour.SteerCenter;
            Vector3 backWheelCenter = _agent.CarBehaviour.BackWheelCenter;
            Vector3 backWheelSideways = -_agent.CarBehaviour.Right;
            float wheelbase = Vector3.Distance(frontWheelCenter, backWheelCenter);
            
            
            _turningRadius = wheelbase / Mathf.Tan(_steeringAngle);
            _turningCenter = backWheelCenter - _turningRadius * backWheelSideways;
            
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
