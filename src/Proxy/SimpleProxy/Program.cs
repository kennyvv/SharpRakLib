using System.IO;
using System.Reflection;
using NLog;

namespace SimpleProxy
{
	class Program
	{
		private static SimpleServer SimpleServer { get; set; }
		static void Main(string[] args)
		{
			ConfigureNLog(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
			
			SimpleServer = new SimpleServer();
			SimpleServer.Start();
		}
		
		private static void ConfigureNLog(string baseDir)
		{
			string loggerConfigFile = Path.Combine(baseDir, "NLog.config");

			string logsDir = Path.Combine(baseDir, "logs");
			if (!Directory.Exists(logsDir))
			{
				Directory.CreateDirectory(logsDir);
			}

			NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(loggerConfigFile, true);
			LogManager.Configuration.Variables["basedir"] = baseDir;
		}
	}
}
