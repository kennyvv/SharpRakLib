using System.Net;
using System.Threading;

namespace SharpRakLib.Core.Client
{
    public class ThreadedRaknetClient : RakNetClient
    {
        private Thread ProcessingThread;
        public ThreadedRaknetClient(IPEndPoint endpoint) : base(endpoint)
        {
            
        }

        protected override void Run()
        {
            ProcessingThread = new Thread(() =>
            {
                base.Run();
            });
            ProcessingThread.Start();
        }
    }
}