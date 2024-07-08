using UnityEngine;

public class PlayerCollisionController : MonoBehaviour
{
    private Rigidbody _rigidBody = null;

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag.Equals("Coin"))
        {
            Destroy(other.gameObject);
        }

    }

}
