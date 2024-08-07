using Robin.Fluent.Builder;

namespace Robin.Fluent;

public interface IFluentFunction
{
    void OnCreating(FunctionBuilder functionBuilder);

    IEnumerable<string> Descriptions { get; set; }
}