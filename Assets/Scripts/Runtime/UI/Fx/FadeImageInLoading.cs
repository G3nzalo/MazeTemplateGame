using Maze.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Maze.Runtime.UI
{
    public class FadeImageInLoading : MonoBehaviour
    {
        [SerializeField] private Image _screenFadeImageBackground;
        [SerializeField] private FadeImage _fadeImage;

        void Start()
        {
            _fadeImage.SetFadeInLoading(_screenFadeImageBackground);
        }
    }
}
