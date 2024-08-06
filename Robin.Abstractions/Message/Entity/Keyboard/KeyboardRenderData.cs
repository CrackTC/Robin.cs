namespace Robin.Abstractions.Message.Entity.Keyboard;

public record KeyboardRenderData(
    string Label,
    string VisitedLabel,
    KeyboardButtonStyle Style);