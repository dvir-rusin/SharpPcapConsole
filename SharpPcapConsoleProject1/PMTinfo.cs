using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpPcapConsoleProject1
{
    internal class PMTinfo
    {
        int StreamType { get; set; }
        int ElementaryPID { get; set;}
        int ESinfoLength { get; set; }
        int DescriptorTag { get; set; }
        int DescriptorLength { get; set; }
        int ElementryStreamIDExtention { get; set; }
        public byte[] Descriptors { get; set; }
        public bool isInitialized { get; set; } = false;

        public PMTinfo(int streamType, int elementaryPID, int esInfoLength)
        {
            StreamType = streamType;
            ElementaryPID = elementaryPID;
            ESinfoLength = esInfoLength;
        }

        public override string ToString()
        {
            return $"StreamType: {StreamType}, ElementaryPID: {ElementaryPID}, ESinfoLength: {ESinfoLength}";
        }

    }
}
