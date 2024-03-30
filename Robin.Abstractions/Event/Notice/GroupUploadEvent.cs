using Robin.Abstractions.Entities;

namespace Robin.Abstractions.Event.Notice;

public record GroupUploadEvent(
    long Time,
    long GroupId,
    long UserId,
    UploadFile File
) : NoticeEvent(Time);