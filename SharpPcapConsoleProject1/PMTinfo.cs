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
