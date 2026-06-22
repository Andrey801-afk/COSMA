public readonly struct CommandExecutionResult
{
    public CommandExecutionResult(bool success, string message, int? jumpTargetLineIndex = null)
    {
        Success = success;
        Message = message;
        JumpTargetLineIndex = jumpTargetLineIndex;
    }

    public bool Success { get; }
    public string Message { get; }
    public int? JumpTargetLineIndex { get; }
}
