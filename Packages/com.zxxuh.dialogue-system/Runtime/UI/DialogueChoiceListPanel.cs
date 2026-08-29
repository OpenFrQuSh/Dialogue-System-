using System;
using System.Collections.Generic;
using DialogueSystem.Execution;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueSystem.UI
{
    public sealed class DialogueChoiceListPanel : MonoBehaviour
    {
        [SerializeField] private Transform contentRoot;
        [SerializeField] private Button choiceButtonPrefab;
        private readonly List<Button> buttons = new List<Button>();

        public IReadOnlyList<Button> Buttons => buttons;

        public void Configure(Transform root, Button buttonPrefab)
        {
            contentRoot = root;
            choiceButtonPrefab = buttonPrefab;
        }

        // 选项只依据 Presentation 中的可见列表创建，索引可直接交给 DialogueRunner 选择。
        public void ShowChoices(IReadOnlyList<DialogueChoicePresentation> choices, Action<int> onSelected)
        {
            ClearChoices();
            if (choices == null || contentRoot == null || choiceButtonPrefab == null)
            {
                return;
            }

            for (var index = 0; index < choices.Count; index++)
            {
                var visibleIndex = index;
                var button = Instantiate(choiceButtonPrefab, contentRoot);
                button.gameObject.SetActive(true);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onSelected?.Invoke(visibleIndex));

                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = choices[index].Text ?? string.Empty;
                }

                buttons.Add(button);
            }
        }

        public void ClearChoices()
        {
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            buttons.Clear();
        }
    }
}
