using System.Net;
using System.Threading;

namespace SharpRakLib.Server
{
	public class ThreadedRaknetServer : RakNetServer
	{
		private readonly Thread _thread;

		public ThreadedRaknetServer(IPEndPoint bindAddress, ServerOptions options) : base(bindAddress, options)
		{
			_thread = new Thread(base.Run);
			_thread.Name = "RakNetServer";
		}

		protected override void Run()
		{
			_thread.Start();
		}
	}
}