using UnityEngine;

namespace Maze.Tools
{
    [DefaultExecutionOrder(-1)]
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("A instance already exists");
                Destroy(this);
                return;
            }
            Instance = this as T;
        }
    }
}

