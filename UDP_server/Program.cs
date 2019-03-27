using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;

namespace UDP_server
{

    public class UDPListener
    {
        private const int listenPort = 11000;
        private static BlockingCollection<IPEndPoint> AllClients = new BlockingCollection<IPEndPoint>();
        private static void StartListener()
        {
            Console.WriteLine("*********Server*******");
            UdpClient listener = new UdpClient(listenPort);
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
                        //already exist in collection 
                        if(groupEP != null && !AllClients.Any((ip)=> ip.Address.ToString() == groupEP.Address.ToString()))
                        {
                            AllClients.TryAdd(groupEP);
                        }
                        Console.WriteLine($"Received from {groupEP} :");
                        Console.WriteLine($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
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
                        sender.Send(myString, myString.Length, iPEndPoint.Address.ToString(),11001);
                    }
                }
            });
            Console.ReadLine();
        }

        public static void Main()
        {
            StartListener();
        }
    }
   
}
