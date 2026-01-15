using System.Reflection;
using Robin.Abstractions;
using Robin.Abstractions.Context;
using Robin.Abstractions.Event;
using Robin.Middlewares.Fluent.Cron;
using Robin.Middlewares.Fluent.Event;
using Robin.Middlewares.Fluent.Tunnel;

namespace Robin.Middlewares.Fluent;

internal record FunctionInfo(
    IEnumerable<EventTunnel> EventTunnels,
    IEnumerable<CronTunnel> CronTunnels
);

public class FunctionBuilder(BotFunction function)
{
    private readonly BotFunction _function = function;
    private readonly IList<EventTunnel> _eventTunnels = [];
    private readonly IList<CronTunnel> _cronTunnels = [];

    internal void AddEventTunnel(EventTunnel tunnel) => _eventTunnels.Add(tunnel);

    internal void AddCronTunnel(CronTunnel tunnel) => _cronTunnels.Add(tunnel);

    internal FunctionInfo Build() => new(_eventTunnels, _cronTunnels);

    public EventTunnelBuilder<EventContext<TEvent>> On<TEvent>(string? name = null)
        where TEvent : BotEvent =>
        new EventTunnelBuilder<EventContext<TEvent>>(
            this,
            name,
            new Tunnel<EventContext<BotEvent>, EventContext<TEvent>>(ctx =>
                Task.FromResult(
                    ctx.Event switch
                    {
                        TEvent e and IGroupEvent { GroupId: var id } => new(
                            new(e, ctx.Token),
                            _function.Context.GroupFilter.IsIdEnabled(id)
                        ),
                        TEvent e and IPrivateEvent { UserId: var id } => new(
                            new(e, ctx.Token),
                            _function.Context.PrivateFilter.IsIdEnabled(id)
                        ),
                        TEvent e => new(new(e, ctx.Token), true),
                        _ => new TunnelResult<EventContext<TEvent>>(default, false),
                    } as ITunnelResult<EventContext<TEvent>>
                )
            )
        ).WithDescription(
            "收到" + typeof(TEvent).GetCustomAttribute<EventDescriptionAttribute>()?.Description
                ?? typeof(TEvent).Name
        );

    public CronTunnelBuilder<CancellationToken> OnCron(string cron, string name = "main cron") =>
        new(
            this,
            cron,
            name,
            new Tunnel<CancellationToken, CancellationToken>(token =>
                Task.FromResult(
                    new TunnelResult<CancellationToken>(token, true)
                        as ITunnelResult<CancellationToken>
                )
            )
        );
}
