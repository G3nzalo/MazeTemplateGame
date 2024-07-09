using UnityEngine;

namespace Maze.Runtime.Entities
{
    [CreateAssetMenu(menuName = "Create PlayerID", fileName = "PlayerID", order = 0)]
    public class PlayerID : ScriptableObject
    {
        [SerializeField] private string _value;
        public string Value => _value;
    }
}

