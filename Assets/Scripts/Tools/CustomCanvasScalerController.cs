using UnityEngine;
using UnityEngine.UI;

namespace Maze.Tools
{
    public class CustomCanvasScalerController : CanvasScalerController
    {
        private void Start()
        {
            Recalculate();
        }
    }
}
