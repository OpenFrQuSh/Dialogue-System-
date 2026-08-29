// 由 MCP 写回以同步 Unity 的脚本导入缓存。
using System;
using System.Collections.Generic;

namespace DialogueSystem.Data
{
    [Serializable]
    public sealed class DialogueCondition
    {
        public string VariableKey;

        public DialogueComparison Comparison;

        public bool BoolValue;

        public int IntValue;

        public bool IsMet(IReadOnlyDictionary<string, DialogueValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (string.IsNullOrWhiteSpace(VariableKey) || !values.TryGetValue(VariableKey, out var value))
            {
                throw new InvalidOperationException("Dialogue variable '" + VariableKey + "' is missing.");
            }

            if (value.Kind == DialogueValueKind.Bool)
            {
                // 布尔变量只允许相等判断，排序比较会掩盖作者配置错误。
                switch (Comparison)
                {
                    case DialogueComparison.Equal:
                        return value.BoolValue == BoolValue;
                    case DialogueComparison.NotEqual:
                        return value.BoolValue != BoolValue;
                    default:
                        throw new InvalidOperationException("Variable '" + VariableKey + "' is Bool and cannot use " + Comparison + ".");
                }
            }

            var actual = value.IntValue;
            switch (Comparison)
            {
                case DialogueComparison.Equal:
                    return actual == IntValue;
                case DialogueComparison.NotEqual:
                    return actual != IntValue;
                case DialogueComparison.Greater:
                    return actual > IntValue;
                case DialogueComparison.GreaterOrEqual:
                    return actual >= IntValue;
                case DialogueComparison.Less:
                    return actual < IntValue;
                case DialogueComparison.LessOrEqual:
                    return actual <= IntValue;
                default:
                    throw new InvalidOperationException("Variable '" + VariableKey + "' uses an unsupported comparison " + Comparison + ".");
            }
        }
    }
}
