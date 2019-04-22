using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace UDP_server
{
    [Serializable]
    public class Client
    {
        public Client(IPEndPoint endPoint, DateTime dateTime)
        {
            EndPoint = endPoint;
            LastPing = dateTime;
        }
        public IPEndPoint EndPoint { get; set; }
        public DateTime LastPing { get; set; }
        public uint NumberOfPacket { get; set; }
        public ClientData Data { get; set; } = new ClientData() { X = 0, Y = 0, Z = 0 };
    }
}
