using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Robin.Abstractions.Communication;

namespace Robin.Abstractions.Context;

public class FunctionContext(
    ILogger logger,
    long uin,
    IOperationProvider operationProvider,
    IConfiguration configuration,
    IEnumerable<BotFunction> functions
)
{
    public ILogger Logger => logger;
    public long Uin => uin;
    public IOperationProvider OperationProvider => operationProvider;
    public IConfiguration Configuration => configuration;
    public IEnumerable<BotFunction> Functions => functions;
}
