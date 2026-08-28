using System;
using System.Collections.Generic;

namespace DialogueSystem.Data
{
    [Serializable]
    public sealed class DialogueNodeData
    {
        public string Id;

        public DialogueNodeKind Kind;

        public string Speaker;

        public string Text;

        public string NextNodeId;

        public List<DialogueChoiceData> Choices = new List<DialogueChoiceData>();

        public string EndingId;

        public string EndingDescription;
    }
}
