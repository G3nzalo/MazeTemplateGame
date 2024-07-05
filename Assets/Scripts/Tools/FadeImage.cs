using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Maze.Tools
{
    public class FadeImage : MonoBehaviour
    {
        [SerializeField] private float _duration = 2f;

        public void SetFadeInLoading(Image target) => FadeInFromLoading(target);
        public void SetFadeInMenu(Image target) => FadeInMenu(target);

        public void SetFadeOut(Image target) => FadeOut(target);

        private void FadeInFromLoading(Image target)
        {
            SetDataToFadeIn(target);
            target.DOFade(1f, _duration).SetEase(Ease.InOutQuad).OnComplete(() =>
            {
                FadeOut(target);
            });
        }

        private void FadeInMenu(Image target)
        {
            SetDataToFadeIn(target);
            target.DOFade(1f, _duration).SetEase(Ease.InOutQuad).OnComplete(() =>
            {
            });
        }


        private void FadeOut(Image target)
        {
            SetDataToFadeOut(target);
            target.DOFade(0f, _duration).SetEase(Ease.InOutQuad).OnComplete(() =>
            {
                target.gameObject.SetActive(false);
            });


        }

        private void SetDataToFadeIn(Image target)
        {
            Color startColor = target.color;
            startColor.a = 0f;
            target.color = startColor;
        }

        private void SetDataToFadeOut(Image target)
        {
            Color startColor = target.color;
            startColor.a = 1f;
            target.color = startColor;
        }
    }
}

