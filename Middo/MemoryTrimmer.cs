namespace Middo;

/// <summary>
/// 降低后台托盘状态的工作集占用。
/// </summary>
internal static class MemoryTrimmer
{
    private static readonly object SyncRoot = new();
    private static CancellationTokenSource? _pendingTrim;

    public static void TrimSoon(int delayMilliseconds = 1000)
    {
        CancellationTokenSource trimRequest;

        lock (SyncRoot)
        {
            _pendingTrim?.Cancel();
            _pendingTrim = new CancellationTokenSource();
            trimRequest = _pendingTrim;
        }

        CancellationToken token = trimRequest.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayMilliseconds, token);
                TrimNow();
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                lock (SyncRoot)
                {
                    if (ReferenceEquals(_pendingTrim, trimRequest))
                    {
                        _pendingTrim = null;
                    }
                }

                trimRequest.Dispose();
            }
        });
    }

    private static void TrimNow()
    {
        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            NativeMethods.EmptyWorkingSet(NativeMethods.GetCurrentProcess());
        }
        catch
        {
            // 内存收缩是机会型优化，失败时不影响托盘工具正常工作。
        }
    }
}
