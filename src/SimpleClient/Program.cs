using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using log4net;
using log4net.Config;
using SharpRakLib.Core.Client;
using SharpRakLib.Protocol.RakNet;

namespace SimpleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
            
            ThreadedRaknetClient client = new ThreadedRaknetClient(new IPEndPoint(IPAddress.Loopback, 19132));
            client.Start();
            
            Console.WriteLine("Started!");

            var session = client.WaitForSession();

            Console.WriteLine($"Session established!");
            Console.ReadLine();
            
            Console.WriteLine($"Finished!");
        }
    }
}