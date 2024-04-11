namespace Robin.Annotations.Cron;

public interface ICronHandler
{
    Task OnCronEventAsync(CancellationToken token);
}