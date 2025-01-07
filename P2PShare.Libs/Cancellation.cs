using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
