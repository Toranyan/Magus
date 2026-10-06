using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using tora.singleton;

namespace magus.cutscene
{
    /// <summary>Full-screen black overlay, drawn above every other canvas. Builds its own
    /// Canvas/Image on Awake, so nothing needs to be placed in a scene - SingletonComponent
    /// creates it on first use of Instance. Driven by CutsceneManager (ScreenFadeAction) and
    /// by ScreenFadeTrack inside a cutscene's Timeline.</summary>
    public class ScreenFader : SingletonComponent<ScreenFader>
    {
        private const int SortingOrder = 1000;

        private Image _image;
        private CancellationTokenSource _fadeCts;

        public float Alpha => _image != null ? _image.color.a : 0f;

        private void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            gameObject.AddComponent<GraphicRaycaster>();

            var imageObject = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(transform, false);

            var rect = (RectTransform)imageObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _image = imageObject.GetComponent<Image>();
            ApplyAlpha(0f);
        }

        private void OnDestroy()
        {
            CancelFade();
        }

        /// <summary>Snaps immediately, cancelling any fade in progress.</summary>
        public void SetAlpha(float alpha)
        {
            CancelFade();
            ApplyAlpha(alpha);
        }

        /// <summary>Fades from the current alpha to targetAlpha over duration seconds
        /// (unscaled time). A newer FadeAsync/SetAlpha call cancels this one, which then
        /// completes early rather than throwing.</summary>
        public async UniTask FadeAsync(float targetAlpha, float duration)
        {
            CancelFade();

            if (duration <= 0f)
            {
                ApplyAlpha(targetAlpha);
                return;
            }

            _fadeCts = new CancellationTokenSource();
            var token = _fadeCts.Token;

            var startAlpha = Alpha;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow())
                {
                    return;
                }

                elapsed += Time.unscaledDeltaTime;
                ApplyAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
            }
        }

        private void CancelFade()
        {
            if (_fadeCts == null)
            {
                return;
            }

            _fadeCts.Cancel();
            _fadeCts.Dispose();
            _fadeCts = null;
        }

        private void ApplyAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            _image.color = new Color(0f, 0f, 0f, alpha);

            // Swallow clicks while (partly) black, but never block UI once fully clear.
            _image.raycastTarget = alpha > 0f;
            _image.enabled = alpha > 0f;
        }
    }
}
