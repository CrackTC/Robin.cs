using Robin.Fluent.Builder;

namespace Robin.Fluent;

public interface IFluentFunction
{
    Task OnCreatingAsync(FunctionBuilder builder, CancellationToken token);

    string? Description { get; set; }
}