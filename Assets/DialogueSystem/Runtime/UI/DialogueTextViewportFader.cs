using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueSystem.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class DialogueTextViewportFader : MonoBehaviour
    {
        [SerializeField] private RectTransform viewport;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField, Min(1f)] private float fadeDistance = 72f;

        private TMP_Text targetText;

        public void Configure(RectTransform fadeViewport, ScrollRect ownerScrollRect, float distance = 72f)
        {
            Unsubscribe();
            viewport = fadeViewport;
            scrollRect = ownerScrollRect;
            fadeDistance = Mathf.Max(1f, distance);
            Subscribe();
            MarkVerticesDirty(Vector2.zero);
        }

        public static float EvaluateAlpha(
            float localY,
            float viewportBottom,
            float viewportTop,
            float distance)
        {
            if (localY <= viewportBottom || localY >= viewportTop)
            {
                return 0f;
            }

            var safeDistance = Mathf.Max(0.0001f, distance);
            var bottomAlpha = (localY - viewportBottom) / safeDistance;
            var topAlpha = (viewportTop - localY) / safeDistance;
            return Mathf.Clamp01(Mathf.Min(bottomAlpha, topAlpha));
        }

        private void Awake()
        {
            targetText = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (targetText == null)
            {
                targetText = GetComponent<TMP_Text>();
            }

            if (targetText != null)
            {
                targetText.OnPreRenderText -= ApplyFade;
                targetText.OnPreRenderText += ApplyFade;
            }

            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(MarkVerticesDirty);
                scrollRect.onValueChanged.AddListener(MarkVerticesDirty);
            }
        }

        private void Unsubscribe()
        {
            if (targetText != null)
            {
                targetText.OnPreRenderText -= ApplyFade;
            }

            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(MarkVerticesDirty);
            }
        }

        private void MarkVerticesDirty(Vector2 _)
        {
            targetText?.SetVerticesDirty();
        }

        private void OnRectTransformDimensionsChange()
        {
            MarkVerticesDirty(Vector2.zero);
        }

        private void ApplyFade(TMP_TextInfo textInfo)
        {
            if (viewport == null || targetText == null || textInfo == null)
            {
                return;
            }

            var viewportRect = viewport.rect;
            for (var characterIndex = 0; characterIndex < textInfo.characterCount; characterIndex++)
            {
                var character = textInfo.characterInfo[characterIndex];
                if (!character.isVisible)
                {
                    continue;
                }

                var meshInfo = textInfo.meshInfo[character.materialReferenceIndex];
                for (var corner = 0; corner < 4; corner++)
                {
                    var vertexIndex = character.vertexIndex + corner;
                    var worldPosition = targetText.rectTransform.TransformPoint(meshInfo.vertices[vertexIndex]);
                    var viewportPosition = viewport.InverseTransformPoint(worldPosition);
                    var alpha = EvaluateAlpha(
                        viewportPosition.y,
                        viewportRect.yMin,
                        viewportRect.yMax,
                        fadeDistance);
                    var color = meshInfo.colors32[vertexIndex];
                    color.a = (byte)Mathf.RoundToInt(color.a * alpha);
                    meshInfo.colors32[vertexIndex] = color;
                }
            }
        }
    }
}
