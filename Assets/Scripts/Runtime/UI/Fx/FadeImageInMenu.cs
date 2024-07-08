using Maze.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Maze.Runtime.UI
{
    public enum FADETYPE
    {
        FADEIN = 0,
        FADEOUT = 1
    }

    public class FadeImageInMenu : MonoBehaviour
    {
        [SerializeField] private Image _screenFadeImageBackground;
        [SerializeField] private FadeImage _fadeImage;
        [SerializeField] private FADETYPE FADETYPE;

        void Start()
        {
            SetFadeType(FADETYPE);
        }


        private void SetFadeType(FADETYPE FADETYPE)
        {
            switch (FADETYPE)
            {
                case FADETYPE.FADEIN:
                    _fadeImage.SetFadeInMenu(_screenFadeImageBackground);
                    break;
                case FADETYPE.FADEOUT:
                    _fadeImage.SetFadeOut(_screenFadeImageBackground);
                    break;
            }
        }
    }
}
