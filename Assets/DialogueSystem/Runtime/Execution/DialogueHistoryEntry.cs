namespace DialogueSystem.Execution
{
    public enum DialogueHistoryKind
    {
        Line,
        Choice
    }

    public sealed class DialogueHistoryEntry
    {
        public DialogueHistoryEntry(DialogueHistoryKind kind, string speaker, string text)
        {
            Kind = kind;
            Speaker = speaker;
            Text = text;
        }

        public DialogueHistoryKind Kind { get; }

        public string Speaker { get; }

        public string Text { get; }
    }

    public enum DialogueSkipResult
    {
        ReachedChoice,
        ReachedEnd
    }
}
