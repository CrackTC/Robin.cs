using Robin.Abstractions.Message.Entity.Keyboard;

namespace Robin.Abstractions.Message.Entity;

public record KeyboardData(KeyboardContent Content) : SegmentData;