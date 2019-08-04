using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Config;

namespace SimpleServer
{
	class Program
	{
		private static SimpleServer SimpleServer { get; set; }
		static void Main(string[] args)
		{
			var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
			XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
			
			SimpleServer = new SimpleServer();
			SimpleServer.Start();
		}
	}
}
