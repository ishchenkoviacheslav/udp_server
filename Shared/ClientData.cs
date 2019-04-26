using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    [Serializable]
    public class ClientData
    {
        public float ID { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float NumberOfPacket { get; set; }
    }
}
