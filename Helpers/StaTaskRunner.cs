namespace RemittanceMatcherApp.Helpers;

public static class StaTaskRunner
{
    public static Task<T> RunAsync<T>(Func<T> work, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        var thread = new Thread(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = work();
                tcs.TrySetResult(result);
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled(cancellationToken);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();

        cancellationToken.Register(() =>
        {
            if (thread.IsAlive)
            {
                tcs.TrySetCanceled(cancellationToken);
            }
        });

        return tcs.Task;
    }
}
