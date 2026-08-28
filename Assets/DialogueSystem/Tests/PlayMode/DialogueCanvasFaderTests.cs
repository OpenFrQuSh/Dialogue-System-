using System.Collections;
using DialogueSystem.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DialogueSystem.Tests
{
    public sealed class DialogueCanvasFaderTests
    {
        [UnityTest]
        public IEnumerator FadeOut_FinalCloseDisablesCanvasAndRaycasts()
        {
            var root = new GameObject("Canvas", typeof(Canvas), typeof(CanvasGroup));
            var fader = root.AddComponent<DialogueCanvasFader>();
            var group = root.GetComponent<CanvasGroup>();
            fader.Configure(group, 0.01f);
            fader.ShowImmediate();

            yield return fader.FadeOut(true);

            Assert.That(group.alpha, Is.Zero);
            Assert.That(group.interactable, Is.False);
            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(root.activeSelf, Is.False);
            Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator FadeOut_BetweenStepsKeepsCanvasReusable()
        {
            var root = new GameObject("Canvas", typeof(Canvas), typeof(CanvasGroup));
            var fader = root.AddComponent<DialogueCanvasFader>();
            var group = root.GetComponent<CanvasGroup>();
            fader.Configure(group, 0.01f);
            fader.ShowImmediate();

            yield return fader.FadeOut(false);

            Assert.That(root.activeSelf, Is.True);
            Assert.That(group.alpha, Is.Zero);
            Assert.That(group.interactable, Is.False);
            Assert.That(group.blocksRaycasts, Is.False);
            Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator FadeIn_RestoresVisibilityAndInteraction()
        {
            var root = new GameObject("Canvas", typeof(Canvas), typeof(CanvasGroup));
            var fader = root.AddComponent<DialogueCanvasFader>();
            var group = root.GetComponent<CanvasGroup>();
            fader.Configure(group, 0.01f);
            fader.HideImmediate(false);

            yield return fader.FadeIn();

            Assert.That(fader.IsVisible, Is.True);
            Assert.That(fader.IsTransitioning, Is.False);
            Assert.That(group.alpha, Is.EqualTo(1f));
            Assert.That(group.interactable, Is.True);
            Assert.That(group.blocksRaycasts, Is.True);
            Object.DestroyImmediate(root);
        }
    }
}
