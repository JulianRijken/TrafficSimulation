using UnityEngine;

public class TeleportBox : MonoBehaviour
{
    [SerializeField] private Transform _teleportTarget;
    
    private void OnTriggerEnter(Collider other)
    {
        other.transform.position = _teleportTarget.position;
    }
}
