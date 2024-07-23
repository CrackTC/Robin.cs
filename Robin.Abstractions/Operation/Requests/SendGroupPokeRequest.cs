namespace Robin.Abstractions.Operation.Requests;

public record SendGroupPokeRequest(long GroupId, long UserId) : Request;