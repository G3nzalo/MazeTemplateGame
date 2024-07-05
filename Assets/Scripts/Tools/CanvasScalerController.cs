using UnityEngine;
using UnityEngine.UI;

namespace Maze.Tools
{
    public class CanvasScalerController : UIView
    {

        [Header("Settings")]
        [SerializeField] protected bool recalculateOnEnable = default;

        [Header("References")]
        [SerializeField] protected CanvasScaler canvasScaler = default;

        protected virtual void OnEnable()
        {
            if (recalculateOnEnable)
            {
                Recalculate();
            }
        }

        protected virtual void Update()
        {
            if (Application.isEditor)
            {
                Recalculate();
            }
        }

        public virtual void Recalculate()
        {
            if (Screen.width <= 0)
            {
                Debug.LogError($"[{nameof(CanvasScalerController)}] Screen.width is <= zero.");
                return;
            }

            if (canvasScaler.referenceResolution.x <= 0)
            {
                Debug.LogError($"[{nameof(CanvasScalerController)}] canvasScaler.referenceResolution.x is <= zero.");
                return;
            }

            float currentAspectRatio = Screen.height / (float)Screen.width;
            float canvasScalerAspectRatio = canvasScaler.referenceResolution.y / canvasScaler.referenceResolution.x;
            canvasScaler.matchWidthOrHeight = currentAspectRatio < canvasScalerAspectRatio ? 1f : 0f;
        }
    }
}

