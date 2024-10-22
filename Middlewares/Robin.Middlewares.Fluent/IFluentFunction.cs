namespace Robin.Middlewares.Fluent;

public interface IFluentFunction
{
    Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token);
}
