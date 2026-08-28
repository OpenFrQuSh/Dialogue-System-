namespace DialogueSystem.Editor
{
    public enum DialogueValidationSeverity
    {
        Error,
        Warning
    }

    public sealed class DialogueValidationIssue
    {
        public DialogueValidationIssue(DialogueValidationSeverity severity,string code,string message,string nodeId = null)
        {
            Severity = severity;
            Code = code;
            Message = message;
            NodeId = nodeId;
        }

        public DialogueValidationSeverity Severity { get; }

        public string Code { get; }

        public string Message { get; }

        public string NodeId { get; }
    }
}
