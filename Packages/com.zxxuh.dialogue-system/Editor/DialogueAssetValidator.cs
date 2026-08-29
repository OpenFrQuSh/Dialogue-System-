using System;
using System.Collections.Generic;
using DialogueSystem.Data;

namespace DialogueSystem.Editor
{
    public static class DialogueAssetValidator
    {
        public static IReadOnlyList<DialogueValidationIssue> Validate(DialogueAsset asset)
        {
            var issues = new List<DialogueValidationIssue>();
            if (asset == null)
            {
                issues.Add(Error("DIALOGUE_EMPTY_ASSET", "Dialogue asset is missing."));
                return issues;
            }

            var uniqueNodes = ValidateNodeIds(asset, issues);
            ValidateEntry(asset, uniqueNodes, issues);
            ValidateLinksAndVariables(asset, uniqueNodes, issues);
            ValidateReachability(asset, uniqueNodes, issues);
            return issues;
        }

        private static Dictionary<string, DialogueNodeData> ValidateNodeIds(
            DialogueAsset asset,
            List<DialogueValidationIssue> issues)
        {
            var uniqueNodes = new Dictionary<string, DialogueNodeData>();

            foreach (var node in asset.Nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Id))
                {
                    issues.Add(Error("DIALOGUE_EMPTY_NODE_ID", "A dialogue node has no ID."));
                    continue;
                }

                if (uniqueNodes.ContainsKey(node.Id))
                {
                    issues.Add(Error(
                        "DIALOGUE_DUPLICATE_NODE_ID",
                        "Duplicate dialogue node ID '" + node.Id + "'.",
                        node.Id));
                    continue;
                }

                uniqueNodes.Add(node.Id, node);
            }

            return uniqueNodes;
        }

        private static void ValidateEntry(
            DialogueAsset asset,
            IReadOnlyDictionary<string, DialogueNodeData> uniqueNodes,
            List<DialogueValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(asset.EntryNodeId)
                || !uniqueNodes.ContainsKey(asset.EntryNodeId))
            {
                issues.Add(Error(
                    "DIALOGUE_MISSING_ENTRY",
                    "Entry node ID does not resolve to a valid node.",
                    asset.EntryNodeId));
            }
        }

        private static void ValidateLinksAndVariables(
            DialogueAsset asset,
            IReadOnlyDictionary<string, DialogueNodeData> uniqueNodes,
            List<DialogueValidationIssue> issues)
        {
            var variableKeys = new HashSet<string>();
            foreach (var variable in asset.Variables)
            {
                if (variable != null && !string.IsNullOrWhiteSpace(variable.Key))
                {
                    variableKeys.Add(variable.Key);
                }
            }

            foreach (var pair in uniqueNodes)
            {
                var node = pair.Value;
                if (node.Kind == DialogueNodeKind.Line)
                {
                    if (string.IsNullOrWhiteSpace(node.Text))
                    {
                        issues.Add(Error("DIALOGUE_EMPTY_LINE_TEXT", "Line node has no text.", node.Id));
                    }

                    ValidateTarget(node.NextNodeId, node.Id, uniqueNodes, issues);
                    continue;
                }

                if (node.Kind == DialogueNodeKind.Choice)
                {
                    if (node.Choices == null || node.Choices.Count == 0)
                    {
                        issues.Add(Error("DIALOGUE_EMPTY_CHOICE_SET", "Choice node has no choices.", node.Id));
                        continue;
                    }

                    foreach (var choice in node.Choices)
                    {
                        if (choice == null)
                        {
                            issues.Add(Error("DIALOGUE_EMPTY_CHOICE", "Choice node contains an empty choice.", node.Id));
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(choice.Text))
                        {
                            issues.Add(Error("DIALOGUE_EMPTY_CHOICE_TEXT", "Choice text is empty.", node.Id));
                        }

                        ValidateTarget(choice.NextNodeId, node.Id, uniqueNodes, issues);
                        ValidateVariableReferences(choice.Conditions, variableKeys, node.Id, issues);
                        ValidateVariableReferences(choice.Effects, variableKeys, node.Id, issues);
                    }
                }
            }
        }

        private static void ValidateTarget(
            string targetId,
            string sourceId,
            IReadOnlyDictionary<string, DialogueNodeData> uniqueNodes,
            List<DialogueValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(targetId) || !uniqueNodes.ContainsKey(targetId))
            {
                issues.Add(Error(
                    "DIALOGUE_BROKEN_LINK",
                    "Node '" + sourceId + "' links to an invalid target '" + targetId + "'.",
                    sourceId));
            }
        }

        private static void ValidateVariableReferences<T>(
            IEnumerable<T> entries,
            ISet<string> variableKeys,
            string nodeId,
            List<DialogueValidationIssue> issues)
            where T : class
        {
            if (entries == null)
            {
                return;
            }

            foreach (var entry in entries)
            {
                string key = null;
                if (entry is DialogueCondition condition)
                {
                    key = condition.VariableKey;
                }
                else if (entry is DialogueEffect effect)
                {
                    key = effect.VariableKey;
                }

                if (entry == null || string.IsNullOrWhiteSpace(key) || !variableKeys.Contains(key))
                {
                    issues.Add(Error(
                        "DIALOGUE_UNKNOWN_VARIABLE",
                        "Node '" + nodeId + "' references an undefined dialogue variable '" + key + "'.",
                        nodeId));
                }
            }
        }

        private static void ValidateReachability(
            DialogueAsset asset,
            IReadOnlyDictionary<string, DialogueNodeData> uniqueNodes,
            List<DialogueValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(asset.EntryNodeId)
                || !uniqueNodes.ContainsKey(asset.EntryNodeId))
            {
                return;
            }

            // BFS 只跟随已验证存在的目标，断链不会让校验本身再抛异常。
            var reachable = new HashSet<string>();
            var pending = new Queue<string>();
            pending.Enqueue(asset.EntryNodeId);

            while (pending.Count > 0)
            {
                var nodeId = pending.Dequeue();
                if (!reachable.Add(nodeId) || !uniqueNodes.TryGetValue(nodeId, out var node))
                {
                    continue;
                }

                if (node.Kind == DialogueNodeKind.Line)
                {
                    EnqueueIfValid(node.NextNodeId, uniqueNodes, pending);
                }
                else if (node.Kind == DialogueNodeKind.Choice && node.Choices != null)
                {
                    foreach (var choice in node.Choices)
                    {
                        if (choice != null)
                        {
                            EnqueueIfValid(choice.NextNodeId, uniqueNodes, pending);
                        }
                    }
                }
            }

            foreach (var pair in uniqueNodes)
            {
                if (!reachable.Contains(pair.Key))
                {
                    issues.Add(Error(
                        "DIALOGUE_UNREACHABLE_NODE",
                        "Node cannot be reached from the entry node.",
                        pair.Key));
                }
            }
        }

        private static void EnqueueIfValid(
            string targetId,
            IReadOnlyDictionary<string, DialogueNodeData> uniqueNodes,
            Queue<string> pending)
        {
            if (!string.IsNullOrWhiteSpace(targetId) && uniqueNodes.ContainsKey(targetId))
            {
                pending.Enqueue(targetId);
            }
        }

        private static DialogueValidationIssue Error(string code, string message, string nodeId = null)
        {
            return new DialogueValidationIssue(DialogueValidationSeverity.Error, code, message, nodeId);
        }
    }
}
