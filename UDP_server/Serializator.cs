using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace UDP_server
{
    public static class Serializator
    {

        public static byte[] Serializer(this object _object)
        {
            byte[] bytes;
            using (var _MemoryStream = new MemoryStream())
            {
                IFormatter _BinaryFormatter = new BinaryFormatter();
                _BinaryFormatter.Serialize(_MemoryStream, _object);
                bytes = _MemoryStream.ToArray();
            }
            return bytes;
        }
        public static Object Deserializer(this byte[] _byteArray)
        {
            Object obj;
            using (var _MemoryStream = new MemoryStream(_byteArray))
            {
                IFormatter _BinaryFormatter = new BinaryFormatter();
                obj = _BinaryFormatter.Deserialize(_MemoryStream);
            }
            return obj;
        }
    }
}
