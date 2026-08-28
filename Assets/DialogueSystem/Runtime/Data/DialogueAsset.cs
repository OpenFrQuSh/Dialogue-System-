// 由 MCP 写回以同步 Unity 的脚本导入缓存。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem.Data
{
    [CreateAssetMenu(menuName = "Dialogue System/Dialogue Asset", fileName = "DialogueAsset")]
    public sealed class DialogueAsset : ScriptableObject
    {
        [SerializeField]
        private string entryNodeId;

        [SerializeField]
        private List<DialogueVariableDefinition> variables = new List<DialogueVariableDefinition>();

        [SerializeField]
        private List<DialogueNodeData> nodes = new List<DialogueNodeData>();

        private readonly Dictionary<string, DialogueNodeData> nodeLookup =
            new Dictionary<string, DialogueNodeData>();

        public string EntryNodeId => entryNodeId;

        public IReadOnlyList<DialogueVariableDefinition> Variables => variables;

        public IReadOnlyList<DialogueNodeData> Nodes => nodes;

        private void OnEnable()
        {
            RebuildNodeLookup();
        }

        private void OnValidate()
        {
            // Inspector 修改节点 ID 后立即重建缓存，避免编辑器预览仍指向旧节点。
            RebuildNodeLookup();
        }

        public bool TryGetNode(string id, out DialogueNodeData node)
        {
            EnsureNodeLookup();
            return nodeLookup.TryGetValue(id ?? string.Empty, out node);
        }

        public IReadOnlyDictionary<string, DialogueValue> CreateInitialValues()
        {
            var initialValues = new Dictionary<string, DialogueValue>();

            foreach (var variable in variables)
            {
                if (variable == null || string.IsNullOrWhiteSpace(variable.Key))
                {
                    continue;
                }

                if (initialValues.ContainsKey(variable.Key))
                {
                    throw new InvalidOperationException("Dialogue variable '" + variable.Key + "' is duplicated.");
                }

                initialValues.Add(variable.Key, variable.CreateValue());
            }

            return initialValues;
        }

        private void EnsureNodeLookup()
        {
            if (nodeLookup.Count == 0 && nodes.Count > 0)
            {
                RebuildNodeLookup();
            }
        }

        private void RebuildNodeLookup()
        {
            nodeLookup.Clear();

            foreach (var node in nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Id))
                {
                    continue;
                }

                // 保留首个节点而非覆盖，重复 ID 交由静态校验器以稳定错误码报告。
                if (!nodeLookup.ContainsKey(node.Id))
                {
                    nodeLookup.Add(node.Id, node);
                }
            }
        }
    }
}
