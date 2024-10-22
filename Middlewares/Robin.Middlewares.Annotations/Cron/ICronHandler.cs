namespace Robin.Middlewares.Annotations.Cron;

public interface ICronHandler
{
    Task OnCronEventAsync(CancellationToken token);
}
