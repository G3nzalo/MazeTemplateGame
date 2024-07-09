using UnityEngine;

namespace Maze.Runtime.Entities
{
    [CreateAssetMenu(menuName = "Create PlayerToSpawnConfiguration", fileName = "PlayerToSpawnConfiguration", order = 0)]
    public class PlayerToSpawnConfiguration : ScriptableObject
    {
        [SerializeField] private PlayerID _playerID;
        [SerializeField] private float _torque;
        [SerializeField] private Vector3 _position;
        [SerializeField] private Quaternion _rotation;

        public PlayerID PlayerID => _playerID;
        public float Torque => _torque;
        public Vector3 Position => _position;
        public Quaternion Rotation => _rotation;
    }
}
