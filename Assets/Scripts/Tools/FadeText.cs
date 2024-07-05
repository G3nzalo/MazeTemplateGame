using UnityEngine;
using DG.Tweening;
using TMPro;

namespace Maze.Tools
{
    public class FadeText : MonoBehaviour
    {
        [SerializeField] private float _duration = 2f;

        public void SetFadeIn(TMP_Text target) => FadeIn(target);
        public void SetFadeOut(TMP_Text target) => FadeOut(target);

        private void FadeIn(TMP_Text target)
        {

            Color startColor = target.color;
            startColor.a = 0f;
            target.color = startColor;
            target.DOFade(1f, _duration).SetEase(Ease.InOutQuad).OnComplete(() =>
            {
                target.gameObject.SetActive(true);
            });
        }

        private void FadeOut(TMP_Text target)
        {
            Color startColor = target.color;
            startColor.a = 1f;
            target.color = startColor;
            target.DOFade(0f, _duration).SetEase(Ease.InOutQuad).OnComplete(() =>
            {
                target.gameObject.SetActive(false);
            });


        }

    }
}

