namespace Robin.Abstractions.Message.Entities.Keyboard;

public record KeyboardAction(
    KeyboardActionType Type,
    KeyboardPermission Permission,
    string UnsupportedTips,
    string Data,
    bool? Reply,
    bool? Enter);