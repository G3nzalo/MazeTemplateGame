using UnityEngine;

public class MovementController : MonoBehaviour
{
    private Rigidbody _rigidBody = null;
    private float _torque = 0;
    private IPlayer _player;


    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody>();
    }

    public void Configure(IPlayer player, float torque)
    {
        _player = player;
        _torque = torque;
    }

    public void MoveHorizontal(float direction)
    {
        _rigidBody.AddTorque(Vector3.back * direction * _torque);
    }

    public void MoveVertical(float direction)
    {
        _rigidBody.AddTorque(Vector3.right * direction * _torque);
    }

}
