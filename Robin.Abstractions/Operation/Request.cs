namespace Robin.Abstractions.Operation;

public abstract record Request;
public abstract record RequestFor<TResp> : Request where TResp : Response;
