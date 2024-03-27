using Robin.Abstractions.Common;

namespace Robin.Abstractions.Message.Entities.Keyboard;

public record KeyboardRow(EquatableImmutableArray<KeyboardButton> Buttons);