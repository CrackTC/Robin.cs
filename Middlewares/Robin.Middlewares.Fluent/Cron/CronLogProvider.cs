using Microsoft.Extensions.Logging;
using QL = Quartz.Logging;

namespace Robin.Middlewares.Fluent.Cron;

internal class CronLogProvider(ILogger logger) : QL.ILogProvider
{
    public QL.Logger GetLogger(string name) => (level, func, exception, parameters) =>
    {
        var l = level switch
        {
            QL.LogLevel.Trace => LogLevel.Trace,
            QL.LogLevel.Debug => LogLevel.Debug,
            QL.LogLevel.Info => LogLevel.Information,
            QL.LogLevel.Warn => LogLevel.Warning,
            QL.LogLevel.Error => LogLevel.Error,
            QL.LogLevel.Fatal => LogLevel.Critical,
            _ => LogLevel.None
        };

        if (func is not null) logger.Log(l, exception, func(), parameters);
        return true;
    };

    public IDisposable OpenMappedContext(string key, object value, bool destructure = false) => throw new NotImplementedException();
    public IDisposable OpenNestedContext(string message) => throw new NotImplementedException();
}
