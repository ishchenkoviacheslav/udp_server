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
        private static List<Client> AllClients = new List<Client>();
        private static byte[] ping = Encoding.ASCII.GetBytes("ping");
        private static int pauseBetweenSendData = 0;
        private const int SIO_UDP_CONNRESET = -1744830452;
        private static int waitBeforeDisconnect = 0;
        private static int refreshListOfClients = 0;
        private static object locker = new object();

        private static void StartListener()
        {
            //for ping it work slow!
            Console.WriteLine("Waiting ...");

            //Client_listenPort = int.Parse(configuration["client_listenPort"]);
            Server_listenPort = int.Parse(configuration["server_listenPort"]);
            //Server_listenPort = int.Parse(configuration.GetSection("server_listenPort").Value);
            pauseBetweenSendData = int.Parse(configuration["pauseBetweenSendData"]);
            waitBeforeDisconnect = int.Parse(configuration["waitBeforeDisconnect"]);
            refreshListOfClients = int.Parse(configuration["refreshListOfClients"]);
            if (/*Client_listenPort == 0 ||*/ Server_listenPort == 0 || pauseBetweenSendData < 10 || waitBeforeDisconnect == 0 || refreshListOfClients == 0)
                throw new Exception("configuration data is wrong");

            Console.WriteLine("*********Server*******");
            UdpClient listener = new UdpClient(Server_listenPort);
            //fix problem with disconnect or crash one of clients
            listener.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            //UdpClient sender = new UdpClient();
            IPEndPoint groupEP = null;// new IPEndPoint(IPAddress.Any, listenPort);
            //sekonds
            TimeSpan compareTimeForRemove = new TimeSpan(0, 0, waitBeforeDisconnect);
            byte[] myString = Encoding.ASCII.GetBytes("Data from server!");
            //listen 
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        UdpReceiveResult result;
                        result = await listener.ReceiveAsync();
                        byte[] bytes = result.Buffer;
                        groupEP = result.RemoteEndPoint;
                        //answer for it fast as possible
                        if (bytes.SequenceEqual(ping))
                        {
                            await listener.SendAsync(bytes, bytes.Length, groupEP);
                            lock (locker)
                            {
                                Client currClient = AllClients.FirstOrDefault((c) => c.EndPoint.Address.ToString() == groupEP.Address.ToString());
                                if (currClient != null)
                                {
                                    currClient.LastPing = DateTime.UtcNow;
                                }
                            }
                        }
                        ////else
                        ////{
                        ////    // not good - add to list only after. But cw is slow command. It must to work only if current request is NOT ping.
                        ////    //first request will response fast but if cw will work than second request will wait to finish of cw.
                        ////    Console.WriteLine($"Received from {groupEP} :");
                        ////    Console.WriteLine($" {Encoding.ASCII.GetString(bytes)}");
                        ////}
                        //already exist in collection 
                        lock (locker)
                        {
                            if (groupEP != null && !AllClients.Any((client) => client.EndPoint.Address.ToString() == groupEP.Address.ToString()))
                            {
                                Console.WriteLine($"added {groupEP}");

                                AllClients.Add(new Client(groupEP, DateTime.UtcNow));
                            }
                        }
                        //not critical make null. ref modificator will change this reference
                        groupEP = null;
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e?.Message);
                }
                finally
                {
                    listener.Close();
                }
            });
            //send data to all clients
            Task.Run(async () =>
            {
                while (true)
                {
                    Thread.Sleep(pauseBetweenSendData);
                    //await can't be in lock - is reason why the Collection without lock
                    //but i think it's non-critical in current situation, because here only send data for all clients(1.only read 2.not big problem if someone take data a few millisecond later)
                    //lock(locker)
                    //{
                    foreach (Client currClient in AllClients)
                    {
                        //Console.WriteLine(iPEndPoint.Address.ToString() + ":" + iPEndPoint.Port);//123.456.789.101:12345
                        //this answer will come to client not from 11000 port...
                        await listener.SendAsync(myString, myString.Length, currClient.EndPoint);
                    }
                    //}

                }
            });
            //remove all disconnected clients
            Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        Thread.Sleep((refreshListOfClients * 1000));
                        Console.WriteLine($"Start of copy: {DateTime.UtcNow}, count of clients: {AllClients.Count}");
                        List<Client> listForRemove = new List<Client>();
                        lock (locker)
                        {
                            foreach (Client client in AllClients)
                            {
                                if (DateTime.UtcNow.Subtract(client.LastPing) > compareTimeForRemove)
                                {
                                    listForRemove.Add(client);
                                }
                            }
                            AllClients = AllClients.Except(listForRemove).ToList();
                        }
                        Console.WriteLine($"End of copy: {DateTime.UtcNow}, count of clients: {AllClients.Count}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
            Console.ReadLine();
            listener.Close();
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
            //Task.Run(async () =>
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
