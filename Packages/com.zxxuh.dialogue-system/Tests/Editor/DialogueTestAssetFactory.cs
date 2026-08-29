using System.Collections.Generic;
using System.Reflection;
using DialogueSystem.Data;
using UnityEngine;

namespace DialogueSystem.Tests
{
    internal static class DialogueTestAssetFactory
    {
        // 测试工厂集中构造数据，避免每个断言重复关注序列化字段。
        public static DialogueCondition BoolCondition(string key, bool expected)
        {
            return new DialogueCondition
            {
                VariableKey = key,
                Comparison = DialogueComparison.Equal,
                BoolValue = expected
            };
        }

        // 测试工厂显式传入比较方式，确保比较逻辑与数据配置解耦。
        public static DialogueCondition IntCondition(
            string key,
            DialogueComparison comparison,
            int expected)
        {
            return new DialogueCondition
            {
                VariableKey = key,
                Comparison = comparison,
                IntValue = expected
            };
        }

        // 测试工厂用既有值的增量效果覆盖会话中最常见的数值变更。
        public static DialogueEffect AddIntEffect(string key, int amount)
        {
            return new DialogueEffect
            {
                VariableKey = key,
                Operation = DialogueEffectOperation.AddInt,
                IntValue = amount
            };
        }

        // 测试直接构造内存资产，避免测试依赖磁盘资源与 Inspector 操作。
        public static DialogueAsset CreateBranchingAsset()
        {
            var asset = ScriptableObject.CreateInstance<DialogueAsset>();
            ConfigureAsset(asset, "line", new List<DialogueNodeData>
            {
                new DialogueNodeData
                {
                    Id = "line",
                    Kind = DialogueNodeKind.Line,
                    Speaker = "控制中枢",
                    Text = "通讯接入。",
                    NextNodeId = "decision"
                },
                new DialogueNodeData
                {
                    Id = "decision",
                    Kind = DialogueNodeKind.Choice,
                    Choices = new List<DialogueChoiceData>
                    {
                        new DialogueChoiceData { Text = "接受", NextNodeId = "accept" },
                        new DialogueChoiceData { Text = "拒绝", NextNodeId = "decline" }
                    }
                },
                new DialogueNodeData { Id = "accept", Kind = DialogueNodeKind.End, EndingId = "accept" },
                new DialogueNodeData { Id = "decline", Kind = DialogueNodeKind.End, EndingId = "decline" }
            });
            return asset;
        }

        // 跳过测试使用连续台词，验证跳过会记录所有实际经过的文本。
        public static DialogueAsset CreateSkipToChoiceAsset()
        {
            var asset = ScriptableObject.CreateInstance<DialogueAsset>();
            ConfigureAsset(asset, "a", new List<DialogueNodeData>
            {
                new DialogueNodeData { Id = "a", Kind = DialogueNodeKind.Line, Text = "台词 A", NextNodeId = "b" },
                new DialogueNodeData { Id = "b", Kind = DialogueNodeKind.Line, Text = "台词 B", NextNodeId = "choice" },
                new DialogueNodeData
                {
                    Id = "choice",
                    Kind = DialogueNodeKind.Choice,
                    Choices = new List<DialogueChoiceData>
                    {
                        new DialogueChoiceData { Text = "继续", NextNodeId = "end" }
                    }
                },
                new DialogueNodeData { Id = "end", Kind = DialogueNodeKind.End, EndingId = "end" }
            });
            return asset;
        }

        private static void ConfigureAsset(
            DialogueAsset asset,
            string entryNodeId,
            List<DialogueNodeData> nodes)
        {
            // 资产公开 API 是只读的；测试通过反射注入序列化字段以验证真实运行时行为。
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(DialogueAsset).GetField("entryNodeId", Flags).SetValue(asset, entryNodeId);
            typeof(DialogueAsset).GetField("nodes", Flags).SetValue(asset, nodes);
        }
    }
}
