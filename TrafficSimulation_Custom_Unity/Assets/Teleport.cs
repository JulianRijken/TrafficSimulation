using System;
using UnityEngine;

public class Teleport : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.GetComponentInParent<Rigidbody>();
        if (rb != null)
        {
            rb.position = transform.position;
            rb.rotation = transform.rotation;
            rb.linearVelocity = transform.forward * rb.linearVelocity.magnitude;
        }
    }
}
