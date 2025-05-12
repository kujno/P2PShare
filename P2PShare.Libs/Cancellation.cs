namespace P2PShare.Libs
{
    public class Cancellation
    {
        private CancellationTokenSource? _tokenSource;
        public CancellationTokenSource? TokenSource
        {
            get
            {
                return _tokenSource;
            }
        }

        public Cancellation()
        {        
        }

        public void Cancel()
        {
            if (TokenSource is not null)
            {
                TokenSource.Cancel();
                TokenSource.Dispose();
            }
            _tokenSource = null;
        }

        public async Task TimeOut()
        {
            await Task.Delay(ConnectionClient.Timeout);

            Cancel();
        }

        public void NewTokenSource()
        {
            _tokenSource = new();
        }
    }
}
