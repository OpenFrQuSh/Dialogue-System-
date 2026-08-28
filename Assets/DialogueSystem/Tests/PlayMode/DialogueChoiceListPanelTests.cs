using System.Collections.Generic;
using DialogueSystem.Execution;
using DialogueSystem.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueSystem.Tests
{
    public sealed class DialogueChoiceListPanelTests
    {
        [Test]
        public void ShowChoices_ClickForwardsVisibleChoiceIndex()
        {
            var panelObject = new GameObject("Choices", typeof(RectTransform));
            var contentObject = new GameObject("Content", typeof(RectTransform));
            var prefabObject = new GameObject("Choice Prefab", typeof(RectTransform), typeof(Image), typeof(Button));
            contentObject.transform.SetParent(panelObject.transform, false);
            var panel = panelObject.AddComponent<DialogueChoiceListPanel>();
            panel.Configure(contentObject.transform, prefabObject.GetComponent<Button>());
            var selectedIndex = -1;

            panel.ShowChoices(
                new List<DialogueChoicePresentation>
                {
                    new DialogueChoicePresentation("Continue"),
                    new DialogueChoicePresentation("Withdraw")
                },
                index => selectedIndex = index);

            Assert.That(panel.Buttons.Count, Is.EqualTo(2));
            panel.Buttons[1].onClick.Invoke();
            Assert.That(selectedIndex, Is.EqualTo(1));

            Object.DestroyImmediate(prefabObject);
            Object.DestroyImmediate(panelObject);
        }
    }
}
