using System;
using UnityEngine;

namespace DialogueSystem.Data
{
    [Serializable]
    public sealed class DialogueVariableDefinition
    {
        [Tooltip("变量唯一键，条件和效果都通过它引用。")]
        public string Key;

        public DialogueValueKind Kind;

        public bool BoolValue;

        public int IntValue;

        public DialogueValue CreateValue()
        {
            // 资产字段只能在此处转换为不可变运行时值，避免运行时污染初始配置。
            return Kind == DialogueValueKind.Bool
                ? DialogueValue.FromBool(BoolValue)
                : DialogueValue.FromInt(IntValue);
        }
    }
}
