using System;
using System.Collections.Generic;

namespace DialogueSystem.Data
{
    [Serializable]
    public sealed class DialogueChoiceData
    {
        public string Text;

        public List<DialogueCondition> Conditions = new List<DialogueCondition>();

        public List<DialogueEffect> Effects = new List<DialogueEffect>();

        public string NextNodeId;
    }
}
