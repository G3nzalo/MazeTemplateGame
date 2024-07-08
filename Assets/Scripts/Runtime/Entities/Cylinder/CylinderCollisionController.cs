using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CylinderCollisionController : MonoBehaviour
{
    private Rigidbody _rigidBody = null;

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag.Equals("Player"))
        {

        }

    }
}
