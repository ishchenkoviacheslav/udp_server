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
        //private static int Client_listenPort = 0;
        private static int Server_listenPort = 0;
        private static BlockingCollection<IPEndPoint> AllClients = new BlockingCollection<IPEndPoint>();
        private static byte[] ping = Encoding.ASCII.GetBytes("ping");
        private static int pauseBetweenSendData = 0;

        private static void StartListener()
        {
            //for ping it work slow!
            Console.WriteLine("Waiting ...");

            //Client_listenPort = int.Parse(configuration["client_listenPort"]);
            Server_listenPort = int.Parse(configuration["server_listenPort"]);
            //Server_listenPort = int.Parse(configuration.GetSection("server_listenPort").Value);
            pauseBetweenSendData = int.Parse(configuration["pauseBetweenSendData"]);

            if (/*Client_listenPort == 0 ||*/ Server_listenPort == 0 || pauseBetweenSendData < 10)
                throw new Exception("configuration data is wrong");

            Console.WriteLine("*********Server*******");
            UdpClient listener = new UdpClient(Server_listenPort);
            //UdpClient sender = new UdpClient();
            IPEndPoint groupEP = null;// new IPEndPoint(IPAddress.Any, listenPort);
            byte[] myString = Encoding.ASCII.GetBytes("Data from server!");
            //listen 
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        UdpReceiveResult result = await listener.ReceiveAsync();
                        byte[] bytes = result.Buffer;
                        groupEP = result.RemoteEndPoint;
                        //answer for it fast as possible
                        if (bytes.SequenceEqual(ping))
                        {
                            listener.Send(bytes, bytes.Length, groupEP);
                        }
                        ////else
                        ////{
                        ////    // not good - add to list only after. But cw is slow command. It must to work only if current request is NOT ping.
                        ////    //first request will response fast but if cw will work than second request will wait to finish of cw.
                        ////    Console.WriteLine($"Received from {groupEP} :");
                        ////    Console.WriteLine($" {Encoding.ASCII.GetString(bytes)}");
                        ////}
                        //already exist in collection 
                        if (groupEP != null && !AllClients.Any((ip)=> ip.Address.ToString() == groupEP.Address.ToString()))
                        {
                            Console.WriteLine($"added {groupEP}");
                            AllClients.TryAdd(groupEP);
                        }
                        //not critical make null. ref modificator will change this reference
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
                    Thread.Sleep(pauseBetweenSendData);
                    foreach (IPEndPoint iPEndPoint in AllClients)
                    {
                        //Console.WriteLine(iPEndPoint.Address.ToString() + ":" + iPEndPoint.Port);//123.456.789.101:12345
                        //this answer will come to client not from 11000 port...
                        listener.Send(myString, myString.Length, iPEndPoint);
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

            ////int port = 27005;
            ////UdpClient udpListener = new UdpClient(port);
            ////IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, port);

            ////byte[] receivedBytes = udpListener.Receive(ref ipEndPoint);      // Receive the information from the client as byte array
            ////string clientMessage = Encoding.UTF8.GetString(receivedBytes);   // Convert the message to a string

            ////byte[] response = Encoding.UTF8.GetBytes("Hello client, this is the server");   // Convert the reponse we want to send to the client to byte array
            ////udpListener.Send(response, response.Length, ipEndPoint);

            ////work good
            //Task.Run(async()=> 
            //{
            //    Console.WriteLine("Server");

            //    int port = 27005;
            //    UdpClient udpListener = new UdpClient(port);
            //    IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, port);

            //    UdpReceiveResult result = await udpListener.ReceiveAsync();      // Receive the information from the client as byte array
            //    ipEndPoint = result.RemoteEndPoint;
            //    byte[] receivedBytes = result.Buffer;
            //    string clientMessage = Encoding.UTF8.GetString(receivedBytes);   // Convert the message to a string

            //    byte[] response = Encoding.UTF8.GetBytes("Hello client, this is the server");   // Convert the reponse we want to send to the client to byte array
            //    await udpListener.SendAsync(response, response.Length, ipEndPoint);

            //});
            //Console.ReadLine();
        }
    }
   
}
