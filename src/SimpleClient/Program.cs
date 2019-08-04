using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using SharpRakLib.Core.Client;
using SharpRakLib.Protocol.RakNet;

namespace SimpleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            RakNetClient client = new RakNetClient(new IPEndPoint(IPAddress.Loopback, 19132));
            
            Console.WriteLine("Started!");
            Thread clientThread = new Thread(() =>
            {
                client.Start();
            });
            
            clientThread.Start();
            
            while (!client.IsConnected)
            {
                UnconnectedPingOpenConnectionsPacket packet = new UnconnectedPingOpenConnectionsPacket();
                packet.PingId = Stopwatch.GetTimestamp();
                
                client.Session.SendPacket(packet);
                
                Thread.Sleep(500);
            }
            
            Console.WriteLine($"Finished!");
        }
    }
}