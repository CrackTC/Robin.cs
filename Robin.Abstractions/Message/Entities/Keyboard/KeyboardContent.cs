using Robin.Abstractions.Common;

namespace Robin.Abstractions.Message.Entities.Keyboard;

public record KeyboardContent(EquatableImmutableArray<KeyboardRow> Rows);