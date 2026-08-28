// 由 MCP 写回以同步 Unity 的脚本导入缓存。
using System;
using System.Collections.Generic;

namespace DialogueSystem.Data
{
    [Serializable]
    public sealed class DialogueEffect
    {
        public string VariableKey;

        public DialogueEffectOperation Operation;

        public bool BoolValue;

        public int IntValue;

        public void Apply(IDictionary<string, DialogueValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (string.IsNullOrWhiteSpace(VariableKey) || !values.TryGetValue(VariableKey, out var current))
            {
                throw new InvalidOperationException("Dialogue variable '" + VariableKey + "' is missing.");
            }

            switch (Operation)
            {
                case DialogueEffectOperation.SetBool:
                    EnsureKind(current, DialogueValueKind.Bool);
                    values[VariableKey] = DialogueValue.FromBool(BoolValue);
                    break;
                case DialogueEffectOperation.SetInt:
                    EnsureKind(current, DialogueValueKind.Int);
                    values[VariableKey] = DialogueValue.FromInt(IntValue);
                    break;
                case DialogueEffectOperation.AddInt:
                    EnsureKind(current, DialogueValueKind.Int);
                    values[VariableKey] = DialogueValue.FromInt(current.IntValue + IntValue);
                    break;
                default:
                    throw new InvalidOperationException("Variable '" + VariableKey + "' uses an unsupported effect " + Operation + ".");
            }
        }

        private void EnsureKind(DialogueValue value, DialogueValueKind expected)
        {
            // 先核对类型再写回，避免效果的一半被应用后才暴露配置错误。
            if (value.Kind != expected)
            {
                throw new InvalidOperationException("Variable '" + VariableKey + "' is " + value.Kind + ", expected " + expected + " for " + Operation + ".");
            }
        }
    }
}
