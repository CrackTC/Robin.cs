namespace Robin.Abstractions.Message.Entity.Keyboard;

public record KeyboardButton(
    string? Id,
    KeyboardRenderData RenderData,
    KeyboardAction Action);
