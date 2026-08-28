using System;
using System.Collections;
using UnityEngine;

namespace DialogueSystem.UI
{
    public sealed class DialogueCanvasFader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0f)] private float transitionDuration = 0.2f;

        public bool IsTransitioning { get; private set; }

        public bool IsVisible { get; private set; }

        public void Configure(CanvasGroup group, float duration)
        {
            canvasGroup = group;
            transitionDuration = Mathf.Max(0f, duration);
        }

        public void ShowImmediate()
        {
            EnsureCanvasGroup();
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            IsVisible = true;
            IsTransitioning = false;
        }

        public void HideImmediate(bool deactivateAfterHide)
        {
            EnsureCanvasGroup();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            IsVisible = false;
            IsTransitioning = false;

            // 最终关闭才禁用根对象；步骤间隐藏需要保留对象以加载下一段文本。
            if (deactivateAfterHide)
            {
                gameObject.SetActive(false);
            }
        }

        public IEnumerator FadeIn()
        {
            EnsureCanvasGroup();
            gameObject.SetActive(true);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            IsTransitioning = true;
            IsVisible = false;

            var startAlpha = canvasGroup.alpha;
            yield return AnimateAlpha(startAlpha, 1f);

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            IsVisible = true;
            IsTransitioning = false;
        }

        public IEnumerator FadeOut(bool deactivateAfterHide)
        {
            EnsureCanvasGroup();
            // 淡出第一帧立即切断输入，避免透明过程中的误点击推进下一句。
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            IsTransitioning = true;
            IsVisible = false;

            var startAlpha = canvasGroup.alpha;
            yield return AnimateAlpha(startAlpha, 0f);

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            IsTransitioning = false;

            if (deactivateAfterHide)
            {
                gameObject.SetActive(false);
            }
        }

        private IEnumerator AnimateAlpha(float from, float to)
        {
            if (transitionDuration <= 0f)
            {
                canvasGroup.alpha = to;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / transitionDuration));
                yield return null;
            }
        }

        private void EnsureCanvasGroup()
        {
            // 允许组件直接挂在 Canvas 根对象上，降低手工配置 Demo 时漏绑引用的概率。
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                throw new InvalidOperationException("DialogueCanvasFader 需要绑定 CanvasGroup。");
            }
        }
    }
}
