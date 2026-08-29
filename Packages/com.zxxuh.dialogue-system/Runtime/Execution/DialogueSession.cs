using System;
using System.Collections.Generic;
using DialogueSystem.Data;

namespace DialogueSystem.Execution
{
    public sealed class DialogueSession
    {
        private readonly List<DialogueHistoryEntry> history = new List<DialogueHistoryEntry>();
        private readonly List<DialogueChoiceData> visibleChoices = new List<DialogueChoiceData>();
        private Dictionary<string, DialogueValue> values;
        private DialogueAsset asset;

        public DialoguePresentation Current { get; private set; }

        public IReadOnlyList<DialogueHistoryEntry> History => history;

        public bool IsEnded { get; private set; }

        public string EndingId { get; private set; }

        public void Start(DialogueAsset dialogueAsset)
        {
            if (dialogueAsset == null)
            {
                throw new ArgumentNullException(nameof(dialogueAsset));
            }

            asset = dialogueAsset;
            values = new Dictionary<string, DialogueValue>(asset.CreateInitialValues());
            history.Clear();
            visibleChoices.Clear();
            IsEnded = false;
            EndingId = null;
            MoveTo(asset.EntryNodeId);
        }

        public void Advance()
        {
            EnsureStarted();

            if (IsEnded)
            {
                throw new InvalidOperationException("Dialogue has already ended.");
            }

            if (Current.Kind != DialogueNodeKind.Line)
            {
                throw new InvalidOperationException("Advance is only valid while presenting a line.");
            }

            MoveTo(GetCurrentNode().NextNodeId);
        }

        public void SelectChoice(int visibleChoiceIndex)
        {
            EnsureStarted();

            if (IsEnded || Current.Kind != DialogueNodeKind.Choice)
            {
                throw new InvalidOperationException("A visible choice is required before selection.");
            }

            if (visibleChoiceIndex < 0 || visibleChoiceIndex >= visibleChoices.Count)
            {
                throw new InvalidOperationException("Selected visible choice index is out of range.");
            }

            var choice = visibleChoices[visibleChoiceIndex];
            if (choice.Effects != null)
            {
                foreach (var effect in choice.Effects)
                {
                    effect?.Apply(values);
                }
            }

            history.Add(new DialogueHistoryEntry(DialogueHistoryKind.Choice, null, choice.Text));
            MoveTo(choice.NextNodeId);
        }

        public DialogueSkipResult SkipToDecisionOrEnd(int maxSteps = 10000)
        {
            EnsureStarted();
            if (maxSteps <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSteps));
            }

            var steps = 0;
            while (true)
            {
                if (IsEnded)
                {
                    return DialogueSkipResult.ReachedEnd;
                }

                if (Current.Kind == DialogueNodeKind.Choice)
                {
                    return DialogueSkipResult.ReachedChoice;
                }

                if (steps >= maxSteps)
                {
                    throw new InvalidOperationException(
                        "Dialogue skip exceeded " + maxSteps
                        + " steps in asset '" + asset.name
                        + "' at node '" + GetCurrentNode().Id + "'.");
                }

                // 跳过重用真实推进路径，因此经过的台词仍会进入历史记录。
                Advance();
                steps++;
            }
        }

        private void MoveTo(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || !asset.TryGetNode(nodeId, out var node))
            {
                throw new InvalidOperationException(
                    "Dialogue asset '" + asset.name + "' cannot resolve node '" + nodeId + "'.");
            }

            visibleChoices.Clear();
            if (node.Kind == DialogueNodeKind.Line)
            {
                Current = new DialoguePresentation(
                    node.Kind,
                    node.Speaker,
                    node.Text,
                    Array.Empty<DialogueChoicePresentation>(),
                    null,
                    null);
                history.Add(new DialogueHistoryEntry(DialogueHistoryKind.Line, node.Speaker, node.Text));
                return;
            }

            if (node.Kind == DialogueNodeKind.Choice)
            {
                var choices = new List<DialogueChoicePresentation>();
                if (node.Choices != null)
                {
                    foreach (var choice in node.Choices)
                    {
                        if (choice != null && IsChoiceVisible(choice))
                        {
                            visibleChoices.Add(choice);
                            choices.Add(new DialogueChoicePresentation(choice.Text));
                        }
                    }
                }

                if (visibleChoices.Count == 0)
                {
                    throw new InvalidOperationException("Dialogue choice node '" + node.Id + "' has no visible choice.");
                }

                Current = new DialoguePresentation(
                    node.Kind,
                    node.Speaker,
                    node.Text,
                    choices,
                    null,
                    null);
                return;
            }

            IsEnded = true;
            EndingId = node.EndingId;
            Current = new DialoguePresentation(
                DialogueNodeKind.End,
                node.Speaker,
                node.Text,
                Array.Empty<DialogueChoicePresentation>(),
                node.EndingId,
                node.EndingDescription);
        }

        private bool IsChoiceVisible(DialogueChoiceData choice)
        {
            if (choice.Conditions == null)
            {
                return true;
            }

            foreach (var condition in choice.Conditions)
            {
                if (condition != null && !condition.IsMet(values))
                {
                    return false;
                }
            }

            return true;
        }

        private DialogueNodeData GetCurrentNode()
        {
            if (!asset.TryGetNode(GetCurrentNodeId(), out var node))
            {
                throw new InvalidOperationException("Current dialogue node no longer exists.");
            }

            return node;
        }

        private string GetCurrentNodeId()
        {
            // 展示快照不暴露节点 ID；从引用匹配回节点以保持 UI 数据与作者 GUID 解耦。
            foreach (var node in asset.Nodes)
            {
                if (node == null)
                {
                    continue;
                }

                if (node.Kind == Current.Kind
                    && node.Speaker == Current.Speaker
                    && node.Text == Current.Text
                    && (Current.Kind != DialogueNodeKind.End || node.EndingId == Current.EndingId))
                {
                    return node.Id;
                }
            }

            throw new InvalidOperationException("Unable to identify the current dialogue node.");
        }

        private void EnsureStarted()
        {
            if (asset == null || Current == null || values == null)
            {
                throw new InvalidOperationException("Dialogue session has not started.");
            }
        }
    }
}
