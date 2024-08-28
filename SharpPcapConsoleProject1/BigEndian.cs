using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpPcapConsoleProject1
{
    //BigEndian class return a an array (revered one or unchanged ) based on if bytes 
    //are read as big endian or little endian(based on the system used)
    public class BigEndian
    {
        public static byte[] BigEndianReadBytes(BinaryReader reader, int size,bool Endian)
        {
            byte[] bytes = reader.ReadBytes(size);
            if (Endian) Array.Reverse(bytes);
            return bytes;
        }
    }
}
