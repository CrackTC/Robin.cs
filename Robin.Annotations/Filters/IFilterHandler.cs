using Robin.Abstractions.Context;

namespace Robin.Annotations.Filters;

public interface IFilterHandler
{
    // ReSharper disable once UnusedParameter.Global
    Task<bool> OnFilteredEventAsync(int filterGroup, EventContext eventContext);
}