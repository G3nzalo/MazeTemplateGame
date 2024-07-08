using System.Numerics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private PlayerMediator player;

    void FixedUpdate()
    {
        MoveCamera();
    }

    private void MoveCamera()
    {
        if (player != null)
        {
            Vector3 direction = (Vector3.up * 2 + Vector3.back) * 2;
            RaycastHit hit;
            Debug.DrawLine(player.transform.position, player.transform.position + direction, Color.red);

            if (Physics.Linecast(transform.position, transform.position + direction, out hit))
            {
                transform.position = hit.point;
            }
            else
            {
                transform.position = transform.position + direction;
            }

            transform.LookAt(player.transform.position);
        }
    }
}
