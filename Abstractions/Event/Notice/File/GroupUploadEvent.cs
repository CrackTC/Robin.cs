namespace Robin.Abstractions.Event.Notice.File;

public record GroupUploadEvent(
    long Time,
    long GroupId,
    long UserId,
    UploadFile File
) : NoticeEvent(Time);