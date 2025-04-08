namespace P2PShare.Libs
{
    public class Cancellation
    {
        public static CancellationTokenSource? Cancel(CancellationTokenSource? cancellationTokenSource)
        {
            if (cancellationTokenSource is not null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }
            
            return null;
        }
    }
}
