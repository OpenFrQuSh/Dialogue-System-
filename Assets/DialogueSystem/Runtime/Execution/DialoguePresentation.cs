using System.Collections.Generic;
using DialogueSystem.Data;

namespace DialogueSystem.Execution
{
    public sealed class DialogueChoicePresentation
    {
        public DialogueChoicePresentation(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }

    public sealed class DialoguePresentation
    {
        public DialoguePresentation(
            DialogueNodeKind kind,
            string speaker,
            string text,
            IReadOnlyList<DialogueChoicePresentation> choices,
            string endingId,
            string endingDescription)
        {
            Kind = kind;
            Speaker = speaker;
            Text = text;
            Choices = choices;
            EndingId = endingId;
            EndingDescription = endingDescription;
        }

        public DialogueNodeKind Kind { get; }

        public string Speaker { get; }

        public string Text { get; }

        public IReadOnlyList<DialogueChoicePresentation> Choices { get; }

        public string EndingId { get; }

        public string EndingDescription { get; }
    }
}
