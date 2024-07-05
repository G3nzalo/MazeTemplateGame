using Maze.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Maze.Runtime.UI
{
    public class LoadingScreen : MonoBehaviour
    {
        [SerializeField] private Image _screenFadeImageBackground;
        [SerializeField] private Image _screenFadeImageText;
        [SerializeField] private FadeImage _fadeImage;

        public void Show()
        {
            _fadeImage.SetFadeInLoading(_screenFadeImageBackground);
            _fadeImage.SetFadeInLoading(_screenFadeImageText);
        }

        public void Hide()
        {
            _fadeImage.SetFadeOut(_screenFadeImageText);
            _fadeImage.SetFadeOut(_screenFadeImageBackground);
        }
    }
}
