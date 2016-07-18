using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleServer
{
	class Program
	{
		private static SimpleServer SimpleServer { get; set; }
		static void Main(string[] args)
		{
			SimpleServer = new SimpleServer();
			SimpleServer.Start();
		}
	}
}
