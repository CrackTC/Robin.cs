namespace Robin.Extensions.Gemini;

[Serializable]
internal class GeminiOption
{
    public required string ApiKey { get; set; }
    public required string Model { get; set; } = "gemini-pro";
    public required string ModelRegexString { get; set; } = @"^切换模型 *([\s\S]*)$";
    public required string SystemRegexString { get; set; } = @"^切换预设 *([\s\S]*)$";
    public required string ClearRegexString { get; set; } = "^重置会话$";
    public required string RollbackRegexString { get; set; } = "^回滚会话$";
    public required string ModelReply { get; set; } = "切换模型成功";
    public required string SystemReply { get; set; } = "切换预设成功";
    public required string ClearReply { get; set; } = "重置会话成功";
    public required string RollbackReply { get; set; } = "回滚会话成功";
    public required string ErrorReply { get; set; } = "发生错误，请重试";
    public required string FilteredReply { get; set; } = "消息中包含敏感内容，已被过滤";
}