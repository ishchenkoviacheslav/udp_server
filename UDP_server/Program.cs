using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace UDP_server
{

    public class UDPListener
    {
        private static IConfigurationRoot configuration;
        private static int Client_listenPort = 0;
        private static int Server_listenPort = 0;
        private static BlockingCollection<IPEndPoint> AllClients = new BlockingCollection<IPEndPoint>();
        private static void StartListener()
        {
            Client_listenPort = int.Parse(configuration["client_listenPort"]);
            Server_listenPort = int.Parse(configuration["server_listenPort"]);
            //Server_listenPort = int.Parse(configuration.GetSection("server_listenPort").Value);
            if (Client_listenPort == 0 || Server_listenPort == 0)
                throw new Exception("configuration data is wrong");

            Console.WriteLine("*********Server*******");
            UdpClient listener = new UdpClient(Server_listenPort);
            UdpClient sender = new UdpClient();
            IPEndPoint groupEP = null;// new IPEndPoint(IPAddress.Any, listenPort);
            byte[] myString = Encoding.ASCII.GetBytes("Data from server!");
            Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        Console.WriteLine("Waiting ...");
                        //listen on 11000
                        byte[] bytes = listener.Receive(ref groupEP);
                        //answer for it fast as possible
                        if (Encoding.ASCII.GetString(bytes) == "ping")
                        {
                            sender.Send(bytes, bytes.Length, groupEP.Address.ToString(), Client_listenPort);
                        }
                        //already exist in collection 
                        if (groupEP != null && !AllClients.Any((ip)=> ip.Address.ToString() == groupEP.Address.ToString()))
                        {
                            AllClients.TryAdd(groupEP);
                        }
                        
                        Console.WriteLine($"Received from {groupEP} :");
                        Console.WriteLine($" {Encoding.ASCII.GetString(bytes)}");
                        groupEP = null;
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    listener.Close();
                }
            });
            Task.Run(() =>
            {
                while (true)
                {
                    //ukrtelecom 92.112.59.89 - must be
                    //umc 46.133.172.211
                    Thread.Sleep(10000);
                    foreach (IPEndPoint iPEndPoint in AllClients)
                    {
                        //this answer will come to client not from 11000 port...
                        sender.Send(myString, myString.Length, iPEndPoint.Address.ToString(), Client_listenPort);
                    }
                }
            });
            Console.ReadLine();
        }

        public static void Main()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            configuration = builder.Build();

            StartListener();
        }
    }
   
}
