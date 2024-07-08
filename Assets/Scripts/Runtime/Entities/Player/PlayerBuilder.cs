using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

public class PlayerBuilder : MonoBehaviour
{
    public enum InputMode
    {
        Unity
    }

    private Vector3 _position = Vector3.zero;
    private Quaternion _rotation = Quaternion.identity;
    private IInput _input;
    private InputMode _inputMode;
    private PlayerToSpawnConfiguration _playerConfiguration;

    public PlayerBuilder WithPosition(Vector3 position)
    {
        _position = position;
        return this;
    }

    public PlayerBuilder WithRotation(Quaternion rotation)
    {
        _rotation = rotation;
        return this;
    }

    public PlayerBuilder WithConfiguration(PlayerToSpawnConfiguration playerConfiguration)
    {
        _playerConfiguration = playerConfiguration;
        return this;
    }

    public PlayerBuilder WithInput(IInput input)
    {
        _input = input;
        return this;
    }

    public PlayerBuilder WithInputMode(InputMode inputMode)
    {
        _inputMode = inputMode;
        return this;
    }

    private IInput GetInput(PlayerMediator playerMediator)
    {
        if (_input != null)
        {
            return _input;
        }

        else if(_inputMode == InputMode.Unity)
        {
            return new UnityInputAdapter();
        }

        else
        {
            throw new ArgumentOutOfRangeException();
        }
    }

    public PlayerMediator Build(GameObject prefab)
    {
        PlayerMediator player = Spawn<PlayerMediator>(prefab, _position, _rotation);

        var playerConfiguration = new PlayerConfiguration(GetInput(player),
                                                      _playerConfiguration.Torque);
        player.Configure(playerConfiguration);
        return player;
    }

    public T Spawn<T>(GameObject prefab ,Vector3 position, Quaternion rotation)
    {
        var instance = UnityEngine.Object.Instantiate(prefab, position, rotation);

        return instance.GetComponent<T>();
    }

}
