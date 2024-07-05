using Maze.Tools;
using TMPro;
using UnityEngine;

namespace Maze.Runtime.UI
{
    public class FadeTextView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _screenFadeTxt;
        [SerializeField] private FadeText _fadeText;

        void Start()
        {
            _fadeText.SetFadeIn(_screenFadeTxt);
        }
    }
}
