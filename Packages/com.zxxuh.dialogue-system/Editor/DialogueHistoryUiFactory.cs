using DialogueSystem.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueSystem.Editor
{
    internal static class DialogueHistoryUiFactory
    {
        public static DialogueHistoryPanel Create(
            Transform root,
            Vector2 anchorMin,
            Vector2 anchorMax,
            TMP_FontAsset fontAsset = null)
        {
            var panelObject = CreateRectObject("History Panel", root, typeof(Image));
            var panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = anchorMin;
            panelRect.anchorMax = anchorMax;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelObject.GetComponent<Image>().color = new Color(0.01f, 0.015f, 0.025f, 0.985f);

            var title = CreateText(panelObject.transform, "Story Title", "故事情节", 38f, fontAsset);
            SetStretch(title.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(44f, -92f), new Vector2(-80f, -28f));
            title.alignment = TextAlignmentOptions.Left;
            title.fontStyle = FontStyles.Bold;

            var divider = CreateRectObject("Title Divider", panelObject.transform, typeof(Image));
            SetStretch(
                divider.GetComponent<RectTransform>(),
                new Vector2(0f, 1f),
                Vector2.one,
                new Vector2(44f, -108f),
                new Vector2(-44f, -106f));
            divider.GetComponent<Image>().color = new Color(0.45f, 0.62f, 0.78f, 0.55f);

            var scrollObject = CreateRectObject("History Scroll View", panelObject.transform, typeof(ScrollRect));
            var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            SetStretch(scrollRectTransform, Vector2.zero, Vector2.one, new Vector2(42f, 36f), new Vector2(-42f, -124f));
            var scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.12f;
            scrollRect.scrollSensitivity = 48f;

            var viewportObject = CreateRectObject("Viewport", scrollObject.transform, typeof(RectMask2D));
            var viewport = viewportObject.GetComponent<RectTransform>();
            SetStretch(viewport, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-22f, 0f));

            var historyText = CreateText(viewportObject.transform, "History Text", string.Empty, 27f, fontAsset);
            var historyTextRect = historyText.rectTransform;
            historyTextRect.anchorMin = new Vector2(0f, 1f);
            historyTextRect.anchorMax = new Vector2(1f, 1f);
            historyTextRect.pivot = new Vector2(0.5f, 1f);
            historyTextRect.anchoredPosition = Vector2.zero;
            historyTextRect.sizeDelta = Vector2.zero;
            historyText.alignment = TextAlignmentOptions.TopLeft;
            historyText.enableWordWrapping = true;
            historyText.lineSpacing = 11f;
            historyText.color = new Color(0.89f, 0.94f, 1f, 1f);
            var fitter = historyText.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollbar = CreateScrollbar(scrollObject.transform);
            scrollRect.viewport = viewport;
            scrollRect.content = historyTextRect;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 8f;

            var fader = historyText.gameObject.AddComponent<DialogueTextViewportFader>();
            fader.Configure(viewport, scrollRect, 76f);

            var historyPanel = panelObject.AddComponent<DialogueHistoryPanel>();
            historyPanel.Configure(historyText, scrollRect);
            panelObject.SetActive(false);
            return historyPanel;
        }

        private static Scrollbar CreateScrollbar(Transform parent)
        {
            var scrollbarObject = CreateRectObject("Scrollbar Vertical", parent, typeof(Image), typeof(Scrollbar));
            var scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = Vector2.one;
            scrollbarRect.pivot = new Vector2(1f, 1f);
            scrollbarRect.offsetMin = new Vector2(-10f, 0f);
            scrollbarRect.offsetMax = Vector2.zero;
            scrollbarObject.GetComponent<Image>().color = new Color(0.18f, 0.25f, 0.34f, 0.35f);

            var slidingArea = CreateRectObject("Sliding Area", scrollbarObject.transform);
            SetStretch(slidingArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            var handle = CreateRectObject("Handle", slidingArea.transform, typeof(Image));
            SetStretch(handle.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            handle.GetComponent<Image>().color = new Color(0.58f, 0.76f, 0.92f, 0.78f);

            var scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.handleRect = handle.GetComponent<RectTransform>();
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.value = 0f;
            return scrollbar;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string value,
            float size,
            TMP_FontAsset fontAsset)
        {
            var textObject = CreateRectObject(name, parent, typeof(TextMeshProUGUI));
            var text = textObject.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = size;
            text.raycastTarget = false;
            if (fontAsset != null)
            {
                text.font = fontAsset;
            }

            return text;
        }

        private static GameObject CreateRectObject(string name, Transform parent, params System.Type[] components)
        {
            var componentTypes = new System.Type[components.Length + 1];
            componentTypes[0] = typeof(RectTransform);
            components.CopyTo(componentTypes, 1);
            var result = new GameObject(name, componentTypes);
            result.transform.SetParent(parent, false);
            return result;
        }

        private static void SetStretch(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
