using UnityEngine;

public class MoveUp : MonoBehaviour
{
    [SerializeField] private bool _isMovingUp = false;
    
    void Update()
    {
        if (_isMovingUp)
        {
            Vector3 currentPosition = transform.position;
            currentPosition.z = 3.0f;
            transform.position = currentPosition;
        }
        else
        {
            Vector3 currentPosition = transform.position;
            currentPosition.z = -3.0f;
            transform.position = currentPosition;
        }
    }
}
