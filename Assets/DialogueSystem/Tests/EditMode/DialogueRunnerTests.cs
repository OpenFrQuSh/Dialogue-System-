using DialogueSystem.Data;
using DialogueSystem.Execution;
using NUnit.Framework;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace DialogueSystem.Tests
{
    public sealed class DialogueRunnerTests
    {
        [Test]
        public void StartDialogue_PublishesFirstLineAndMarksRunnerActive()
        {
            var gameObject = new GameObject("DialogueRunnerTest");
            var runner = gameObject.AddComponent<DialogueRunner>();
            DialoguePresentation presented = null;
            runner.Presented += value => presented = value;

            runner.StartDialogue(DialogueTestAssetFactory.CreateBranchingAsset());

            Assert.That(presented, Is.Not.Null);
            Assert.That(presented.Text, Is.EqualTo("通讯接入。"));
            Assert.That(runner.IsRunning, Is.True);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void StartDialogue_WithInvalidAsset_PublishesFailureWithoutPresentation()
        {
            var gameObject = new GameObject("DialogueRunnerFailureTest");
            var runner = gameObject.AddComponent<DialogueRunner>();
            var failureCount = 0;
            var presentationCount = 0;
            runner.Failed += _ => failureCount++;
            runner.Presented += _ => presentationCount++;

            // 失败边界应写入 Unity Console；测试显式声明该日志以验证而非吞掉错误。
            LogAssert.Expect(LogType.Error, new Regex("\\[DialogueSystem\\]"));

            runner.StartDialogue(ScriptableObject.CreateInstance<DialogueAsset>());

            Assert.That(failureCount, Is.EqualTo(1));
            Assert.That(presentationCount, Is.EqualTo(0));
            Object.DestroyImmediate(gameObject);
        }
    }
}
