using Maze.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Maze.Runtime.UI
{
    public class FadeImageInMenu : MonoBehaviour
    {
        [SerializeField] private Image _screenFadeImageBackground;
        [SerializeField] private FadeImage _fadeImage;

        void Start()
        {
            _fadeImage.SetFadeInMenu(_screenFadeImageBackground);
        }

    }
}
