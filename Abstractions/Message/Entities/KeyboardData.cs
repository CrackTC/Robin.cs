using Robin.Abstractions.Message.Entities.Keyboard;

namespace Robin.Abstractions.Message.Entities;

public record KeyboardData(KeyboardContent Content) : SegmentData;