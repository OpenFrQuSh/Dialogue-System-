// 由 MCP 写回以同步 Unity 的脚本导入缓存。
using System;

namespace DialogueSystem.Data
{
    /// <summary>
    /// 对话变量的不可变运行时值，借由 Kind 避免布尔与整数在分支判断中被混用。
    /// </summary>
    public sealed class DialogueValue
    {
        private DialogueValue(DialogueValueKind kind, bool boolValue, int intValue)
        {
            Kind = kind;
            this.boolValue = boolValue;
            this.intValue = intValue;
        }

        public DialogueValueKind Kind { get; }

        public bool BoolValue
        {
            get
            {
                // 读取错误类型必须立刻报错，防止错误条件被静默当作 false。
                if (Kind != DialogueValueKind.Bool)
                {
                    throw new InvalidOperationException("DialogueValue is " + Kind + ", not Bool.");
                }

                return boolValue;
            }
        }

        public int IntValue
        {
            get
            {
                // 读取错误类型必须立刻报错，防止数值效果误作用于布尔变量。
                if (Kind != DialogueValueKind.Int)
                {
                    throw new InvalidOperationException("DialogueValue is " + Kind + ", not Int.");
                }

                return intValue;
            }
        }

        private readonly bool boolValue;
        private readonly int intValue;

        public static DialogueValue FromBool(bool value)
        {
            return new DialogueValue(DialogueValueKind.Bool, value, default);
        }

        public static DialogueValue FromInt(int value)
        {
            return new DialogueValue(DialogueValueKind.Int, default, value);
        }
    }
}
