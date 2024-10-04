using Robin.Abstractions.Entity;

namespace Robin.Abstractions.Event.Notice;

[EventDescription("群文件上传")]
public record GroupUploadEvent(
    long Time,
    long GroupId,
    long UserId,
    UploadFile File
) : NoticeEvent(Time);
