namespace Robin.Abstractions.Message.Entities.Keyboard;

public record KeyboardButton(
    string? Id,
    KeyboardRenderData RenderData,
    KeyboardAction Action);