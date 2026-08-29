using System.Collections.Generic;
using System.Text;
using DialogueSystem.Execution;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueSystem.UI
{
    public sealed class DialogueHistoryPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text historyText;
        [SerializeField] private ScrollRect scrollRect;

        public string DisplayText { get; private set; } = string.Empty;

        public void Configure(TMP_Text text, ScrollRect historyScrollRect)
        {
            historyText = text;
            scrollRect = historyScrollRect;
        }

        // 仅使用会话传入的历史条目，保证历史面板不会泄露未经历的分支内容。
        public void SetHistory(IReadOnlyList<DialogueHistoryEntry> entries)
        {
            var builder = new StringBuilder();
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    // 每条记录使用“名称、换行、内容”的购物清单结构；玩家选项也作为明确的故事行为保留。
                    var speaker = entry.Kind == DialogueHistoryKind.Choice
                        ? "你的选择"
                        : string.IsNullOrWhiteSpace(entry.Speaker) ? "旁白" : entry.Speaker;
                    if (builder.Length > 0)
                    {
                        builder.Append("\n\n");
                    }

                    builder.Append(speaker).Append('\n');
                    builder.Append(entry.Text ?? string.Empty).Append('\n');
                    builder.Append("────────────────────────");
                }
            }

            DisplayText = builder.ToString().TrimEnd();
            if (historyText != null)
            {
                historyText.text = DisplayText;
            }

            ScrollToLatest();
        }

        public void ToggleVisible()
        {
            var shouldShow = !gameObject.activeSelf;
            gameObject.SetActive(shouldShow);
            if (shouldShow)
            {
                ScrollToLatest();
            }
        }

        private void ScrollToLatest()
        {
            if (scrollRect == null)
            {
                return;
            }

            // 强制完成文字首选高度与 Content 布局后再定位底部，避免首次打开仍停在旧位置。
            Canvas.ForceUpdateCanvases();
            if (historyText != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(historyText.rectTransform);
            }

            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
