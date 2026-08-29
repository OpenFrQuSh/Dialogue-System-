namespace DialogueSystem.Data
{
    public enum DialogueNodeKind
    {
        Line,
        Choice,
        End
    }

    public enum DialogueValueKind
    {
        Bool,
        Int
    }

    public enum DialogueComparison
    {
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual
    }

    public enum DialogueEffectOperation
    {
        SetBool,
        SetInt,
        AddInt
    }
}
