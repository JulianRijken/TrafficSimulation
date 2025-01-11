using UnityEngine;

namespace TrafficSimulation
{
    public class Blinker : MonoBehaviour
    {
        private TrafficAgent _trafficAgent;
        [SerializeField] private GameObject _blinkerObject;

        [SerializeField] private float _blinkInterval = 0.5f;
        [SerializeField] private float _requiredTurnAngle = 45;

        private float _blinkTimer = 0;
        private bool _isBlinking = false;

        private void Start()
        {
            _trafficAgent = GetComponentInParent<TrafficAgent>();
        }

        private void Update()
        {
            _blinkTimer += Time.deltaTime;

            if (_trafficAgent.CurrentTurn == null)
            {
                _isBlinking = false;
            }
            else
            {
                float turnAngle = _trafficAgent.CurrentTurn.Angle;

                if (Mathf.Abs(turnAngle) > Mathf.Abs(_requiredTurnAngle))
                {
                    _isBlinking = (int)Mathf.Sign(_trafficAgent.CurrentTurn.Angle) ==
                                  (int)Mathf.Sign(_requiredTurnAngle);
                }
            }

            if (_isBlinking)
            {
                if (_blinkTimer >= _blinkInterval)
                {
                    _blinkTimer = 0;
                    _blinkerObject.SetActive(!_blinkerObject.activeSelf);
                }
            }
            else
            {
                _blinkerObject.SetActive(false);
            }
        }
    }
}
