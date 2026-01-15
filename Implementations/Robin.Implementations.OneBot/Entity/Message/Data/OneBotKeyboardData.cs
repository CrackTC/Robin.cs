using System.Text.Json;
using System.Text.Json.Serialization;
using Robin.Abstractions.Message;
using Robin.Abstractions.Message.Entity;
using Robin.Implementations.OneBot.Converter;

namespace Robin.Implementations.OneBot.Entity.Message.Data;

[Serializable]
[OneBotSegmentData("keyboard", typeof(KeyboardData))]
internal class OneBotKeyboardData : IOneBotSegmentData
{
    [JsonPropertyName("content")]
    public required OneBotKeyboardContent Content { get; set; }

    public OneBotSegment FromSegmentData(SegmentData data, OneBotMessageConverter converter)
    {
        var d = data as KeyboardData;
        Content = new OneBotKeyboardContent
        {
            Rows = d!
                .Content.Rows.Select(row => new OneBotKeyboardRow
                {
                    Buttons = row
                        .Buttons.Select(button => new OneBotKeyboardButton
                        {
                            Id = button.Id,
                            RenderData = new OneBotKeyboardRenderData
                            {
                                Label = button.RenderData.Label,
                                VisitedLabel = button.RenderData.VisitedLabel,
                                Style = (int)button.RenderData.Style,
                            },
                            Action = new OneBotKeyboardAction
                            {
                                Type = (int)button.Action.Type,
                                Permission = new OneBotKeyboardPermission
                                {
                                    Type = (int)button.Action.Permission.Type,
                                    UserIds = button.Action.Permission.UserIds,
                                    RoleIds = button.Action.Permission.RoleIds,
                                },
                                Data = button.Action.Data,
                                Reply = button.Action.Reply,
                                Enter = button.Action.Enter,
                                UnsupportedTips = button.Action.UnsupportedTips,
                            },
                        })
                        .ToList(),
                })
                .ToList(),
        };

        return new OneBotSegment()
        {
            Type = "keyboard",
            Data = JsonSerializer.SerializeToNode(this),
        };
    }

    public SegmentData ToSegmentData(OneBotMessageConverter converter) =>
        throw new NotImplementedException();
}

[Serializable]
internal class OneBotKeyboardContent
{
    [JsonPropertyName("rows")]
    public required List<OneBotKeyboardRow> Rows { get; set; }
}

[Serializable]
internal class OneBotKeyboardRow
{
    [JsonPropertyName("buttons")]
    public required List<OneBotKeyboardButton> Buttons { get; set; }
}

[Serializable]
internal class OneBotKeyboardButton
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("render_data")]
    public required OneBotKeyboardRenderData RenderData { get; set; }

    [JsonPropertyName("action")]
    public required OneBotKeyboardAction Action { get; set; }
}

[Serializable]
internal class OneBotKeyboardRenderData
{
    [JsonPropertyName("label")]
    public required string Label { get; set; }

    [JsonPropertyName("visited_label")]
    public required string VisitedLabel { get; set; }

    [JsonPropertyName("style")]
    public required int Style { get; set; }
}

[Serializable]
internal class OneBotKeyboardAction
{
    [JsonPropertyName("type")]
    public required int Type { get; set; }

    [JsonPropertyName("permission")]
    public required OneBotKeyboardPermission Permission { get; set; }

    [JsonPropertyName("unsupport_tips")]
    public required string UnsupportedTips { get; set; }

    [JsonPropertyName("data")]
    public required string Data { get; set; }

    [JsonPropertyName("reply")]
    public bool? Reply { get; set; }

    [JsonPropertyName("enter")]
    public bool? Enter { get; set; }
}

[Serializable]
internal class OneBotKeyboardPermission
{
    [JsonPropertyName("type")]
    public required int Type { get; set; }

    [JsonPropertyName("specify_role_ids")]
    public List<string>? RoleIds { get; set; }

    [JsonPropertyName("specify_user_ids")]
    public List<string>? UserIds { get; set; }
}
