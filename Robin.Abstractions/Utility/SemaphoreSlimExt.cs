namespace Robin.Abstractions.Utility;

public static class SemaphoreSlimExt
{
    public static async Task<T> ConsumeAsync<T>(
        this SemaphoreSlim semaphore,
        Func<Task<T>> func,
        CancellationToken token
    )
    {
        await semaphore.WaitAsync(token);
        try
        {
            return await func();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static async ValueTask<T> ConsumeAsync<T>(
        this SemaphoreSlim semaphore,
        Func<ValueTask<T>> func,
        CancellationToken token
    )
    {
        await semaphore.WaitAsync(token);
        try
        {
            return await func();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static async Task ConsumeAsync(
        this SemaphoreSlim semaphore,
        Func<Task> func,
        CancellationToken token
    )
    {
        await semaphore.WaitAsync(token);
        try
        {
            await func();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static async ValueTask ConsumeAsync(
        this SemaphoreSlim semaphore,
        Func<ValueTask> func,
        CancellationToken token
    )
    {
        await semaphore.WaitAsync(token);
        try
        {
            await func();
        }
        finally
        {
            semaphore.Release();
        }
    }
}
