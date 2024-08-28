using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpPcapConsoleProject1.DataTypes
{
    public class PMTinfo
    {
        public int StreamType { get; set; }
        public int ElementaryPID { get; set; }
        public int ESinfoLength { get; set; }
        public int DescriptorTag { get; set; }
        public int DescriptorLength { get; set; }
        public int ElementryStreamIDExtention { get; set; }
        public byte[] Descriptors { get; set; }
        public bool isInitialized { get; set; } = false;


        public override string ToString()
        {
            return $"StreamType: {StreamType}, ElementaryPID: {ElementaryPID}, ESinfoLength: {ESinfoLength}";
        }

    }
}
