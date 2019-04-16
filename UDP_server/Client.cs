using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace UDP_server
{
    public class Client
    {
        public Client(IPEndPoint endPoint, DateTime dateTime)
        {
            EndPoint = endPoint;
            LastPing = dateTime;
        }
        public IPEndPoint EndPoint { get; set; }
        public DateTime LastPing { get; set; }
        public int DataNumber { get; set; }
    }
}
