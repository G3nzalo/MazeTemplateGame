using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(MovementController))]
[RequireComponent(typeof(PlayerCollisionController))]
public class PlayerMediator : MonoBehaviour , EventObserver , IPlayer
{
    [SerializeField] GameObject ViewCamera = null;
    [SerializeField] private MovementController _movementController;

    private IInput _input;

    [SerializeField] private PlayerID _playerId;

    public string Id => _playerId.Value;
    public void LeavePlayer() => Release();

    public void Process(EventData eventData)
    {

    }

    private void Init()
    {
        var eventQueue = ServiceLocator.Instance.GetService<EventQueue>();
        eventQueue.Subscribe(EventIds.Victory, this);
    }

    private void Release()
    {
        var eventQueue = ServiceLocator.Instance.GetService<EventQueue>();
        eventQueue.Unsubscribe(EventIds.Victory, this);
    }

    public void Configure(PlayerConfiguration configuration)
    {
        _input = configuration.Input;
        _movementController.Configure(this, configuration.Torque);

        ViewCamera = GameObject.FindWithTag("MainCamera");
        Init();
    }

    private void FixedUpdate()
    {

        if (_input.GetDirectionHorizontal() != 0)
        {
            var direction = _input.GetDirectionHorizontal();

            _movementController.MoveHorizontal(direction);
        }

        if (_input.GetDirectionVertical() != 0)
        {
            var direction = _input.GetDirectionVertical();
            _movementController.MoveVertical(direction);

        }

        CameraMovement();
    }

    private void CameraMovement()
    {
        if (ViewCamera != null)
        {
            Vector3 direction = (Vector3.up * 2 + Vector3.back) * 2;
            RaycastHit hit;
            Debug.DrawLine(transform.position, transform.position + direction, Color.red);
            if (Physics.Linecast(transform.position, transform.position + direction, out hit))
            {
                ViewCamera.transform.position = hit.point;
            }
            else
            {
                ViewCamera.transform.position = transform.position + direction;
            }
            ViewCamera.transform.LookAt(transform.position);
        }
    }
}
