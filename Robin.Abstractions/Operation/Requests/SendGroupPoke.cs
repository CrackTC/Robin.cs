namespace Robin.Abstractions.Operation.Requests;

public record SendGroupPoke(long GroupId, long UserId) : RequestFor<Response>;
