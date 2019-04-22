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
using NLog;

namespace UDP_server
{
    //by productions: 
    //delete all cw(logger instead of this)
    //
    public class UDPListener
    {
        private static IConfigurationRoot configuration;
        //private static int Client_listenPort = 0;
        private static int server_listenPort = 0;
        private static List<Client> AllClients = new List<Client>();
        private static byte[] ping = Encoding.ASCII.GetBytes("ping");
        private static int pauseBetweenSendData = 0;
        private const int SIO_UDP_CONNRESET = -1744830452;
        private static int waitBeforeDisconnect = 0;
        private static int refreshListOfClients = 0;
        private static object locker = new object();
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static TimeSpan minimumPause = new TimeSpan(0,0,0,0,10);
        public static double IntervalForLogging = 0;
        private static bool IsDebug = false;

        private static void StartListener()
        {
            

            //Client_listenPort = int.Parse(configuration["client_listenPort"]);
            server_listenPort = int.Parse(configuration[nameof(server_listenPort)]);
            //Server_listenPort = int.Parse(configuration.GetSection("server_listenPort").Value);
            pauseBetweenSendData = int.Parse(configuration[nameof(pauseBetweenSendData)]);
            waitBeforeDisconnect = int.Parse(configuration[nameof(waitBeforeDisconnect)]);
            refreshListOfClients = int.Parse(configuration[nameof(refreshListOfClients)]);
            IntervalForLogging = double.Parse(configuration[nameof(IntervalForLogging)]);
            IsDebug = bool.Parse(configuration[nameof(IsDebug)]);
            if (/*Client_listenPort == 0 ||*/ server_listenPort == 0 || pauseBetweenSendData < 10 || waitBeforeDisconnect == 0 || refreshListOfClients == 0 || IntervalForLogging < 30000)
            {
                logger.Fatal("configuration data is wrong");
                if (IsDebug)
                {
                    Console.WriteLine("configuration data is wrong");
                }
                return;
            }
            if(IsDebug)
            {
                Console.WriteLine("Waiting ...");
                logger.Info("Waiting...");
                Console.WriteLine("*********Server*******");
            }
            UdpClient listener = new UdpClient(server_listenPort);
            //fix problem with disconnect or crash one of clients
            listener.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            //sekonds
            TimeSpan compareTimeForRemove = new TimeSpan(0, 0, waitBeforeDisconnect);
            
            //listen 
            Task.Run(async () =>
            {
                try
                {
                    IPEndPoint clientIP = null;// new IPEndPoint(IPAddress.Any, listenPort);
                    UdpReceiveResult result;
                    byte[] bytes;
                    while (true)
                    {
                        result = await listener.ReceiveAsync();
                        bytes = result.Buffer;
                        clientIP = result.RemoteEndPoint;
                        //answer for it fast as possible
                        if (bytes.SequenceEqual(ping))
                        {
                            await listener.SendAsync(bytes, bytes.Length, clientIP);
                            lock (locker)
                            {
                                //Client currClient = AllClients.FirstOrDefault((c) => c.EndPoint.Address.Equals(groupEP.Address));
                                //only for test !!! port instead adress
                                Client currClient = AllClients.FirstOrDefault((c) => c.EndPoint.Port.Equals(clientIP.Port));
                                if (currClient != null)
                                {
                                    currClient.LastPing = DateTime.UtcNow;
                                }
                            }
                            continue;
                        }
                        
                        //already exist in collection 
                        lock (locker)
                        {
                            //if (groupEP != null && !AllClients.Any((client) => client.EndPoint.Address.Equals(groupEP.Address)))
                            //for test only!!!port instead adress
                            if (clientIP != null && !AllClients.Any((client) => client.EndPoint.Port.Equals(clientIP.Port)))
                            {
                                //log Count of clients? Like a critical load
                                if (IsDebug)
                                {
                                    Console.WriteLine($"added {clientIP}");
                                    logger.Info($"added {clientIP}");
                                }
                                AllClients.Add(new Client(clientIP, DateTime.UtcNow));
                            }
                            else
                            {
                                //Client client = AllClients.FirstOrDefault(c => c.EndPoint.Adress.Equals(clientIP.Adress));
                                //for test only!!!port instead adress
                                Client client = AllClients.FirstOrDefault(c => c.EndPoint.Port.Equals(clientIP.Port));
                                //client.Data = (ClientData)(bytes.Deserializer());
                            }
                        }
                        //not critical make null. ref modificator will change this reference
                        clientIP = null;
                    }
                }
                catch (SocketException e)
                {
                    logger.Fatal(e, $"UdpClient object is closing...{e.Message}");
                }
                finally
                {
                    listener.Close();
                }
            });
            //send data to all clients
            Task.Run(() =>
            {
                try
                {
                    DateTime temp;
                    List<ClientData> myVisibleClientsTemp = null;
                    //timer only for logging
                    System.Timers.Timer intervalForWriteLog = new System.Timers.Timer() { Interval = IntervalForLogging, Enabled = true, AutoReset = false };
                    intervalForWriteLog.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) => { ((System.Timers.Timer)sender).Enabled = false; };
                    while (true)
                    {
                        //await can't be in lock - is reason why the Collection without lock
                        //but i think it's non-critical in current situation, because here only send data for all clients(1.only read 2.not big problem if someone take data a few millisecond later)
                        myVisibleClientsTemp = new List<ClientData>();
                        temp = DateTime.UtcNow;
                        lock (locker)
                        {
                            for (int z = 0; z < AllClients.Count; z++)
                            {
                                //this answer will come to client not from 11000(this port is which server listen) port...(from who and to whom)
                                //server will send answer from some server's output port to client's port(but not to 11001...?)
                                //all my clients will in this current range
                                float minX = AllClients[z].Data.X - 10;
                                float maxX = AllClients[z].Data.X + 10;

                                float maxY = AllClients[z].Data.Y - 10;
                                float minY = AllClients[z].Data.Y + 10;

                                float maxZ = AllClients[z].Data.Z - 10;
                                float minZ = AllClients[z].Data.Z + 10;

                                for (int n = 0; n < AllClients.Count; n++)
                                {
                                    if(AllClients[n].Data.X > minX && AllClients[n].Data.X < maxX)
                                    {
                                        myVisibleClientsTemp.Add(AllClients[n].Data);
                                    }
                                }
                                byte[] bytes = myVisibleClientsTemp.Serializer();
                                listener.Send(bytes, bytes.Length, AllClients[z].EndPoint);
                            }
                        }
                        TimeSpan total = DateTime.UtcNow.Subtract(temp);
                        //timer only for logging
                        if (!intervalForWriteLog.Enabled)
                        {
                            int CountOfClient;
                            lock(locker)
                            {
                                CountOfClient = AllClients.Count;
                            }
                            logger.Info($"Time execution for clients list {total}, Count of clients: {CountOfClient}");
                            intervalForWriteLog.Enabled = true;
                        }
                        //make a pause if time will so short (less than 10 ms). Or if pauseBetweenSendData will 40 ms / 2 = 20, than 19 ms will make a pause also
                        if (total < new TimeSpan(0, 0, 0, 0, (pauseBetweenSendData/2)) || total < minimumPause)
                        {
                            Thread.Sleep(pauseBetweenSendData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Fatal(ex, $"send data mechanism has exception: {ex.Message}");
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
                        List<Client> listForRemove = new List<Client>();
                        DateTime temp = DateTime.UtcNow;
                        lock (locker)
                        {
                            for (int rl = 0; rl < AllClients.Count; rl++)
                            {
                                if (DateTime.UtcNow.Subtract(AllClients[rl].LastPing) > compareTimeForRemove)
                                {
                                    listForRemove.Add(AllClients[rl]);
                                }
                            }
                            AllClients = AllClients.Except(listForRemove).ToList();
                        }
                        TimeSpan result = DateTime.UtcNow.Subtract(temp);
                        Console.WriteLine(result);
                    }
                }
                catch (Exception ex)
                {
                    logger.Fatal(ex, $"mechanishm of remove disconnected clients has exception: {ex.Message}");
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
