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

        // H.264 and H.265-specific properties
        public string NalUnitType { get; set; }       // Type of NAL unit (e.g., IDR, P, B frame)
        public string FrameType { get; set; }         // Type of frame (I, P, B)
        public int BitDepth { get; set; }             // Bit depth of the video stream
        public string ChromaFormat { get; set; }      // Chroma format (e.g., 4:2:0)
        public string Resolution { get; set; }        // Resolution of the video stream
        public double FrameRate { get; set; }         // Frame rate of the video stream
        public int BitRate { get; set; }              // Bit rate of the video stream

        // Audio-specific properties
        public string AudioEncoder { get; set; }      // Name of the audio encoder (e.g., AAC, AC-3)

        // Additional attributes for data streams or other types
        public string StreamDescription { get; set; } // Description or type of data stream

        public override string ToString()
        {
            return $"StreamType: {StreamType}, ElementaryPID: {ElementaryPID}, ESinfoLength: {ESinfoLength}, " +
                   $"NalUnitType: {NalUnitType}, FrameType: {FrameType}, BitDepth: {BitDepth}, ChromaFormat: {ChromaFormat}, " +
                   $"Resolution: {Resolution}, FrameRate: {FrameRate}, BitRate: {BitRate}, AudioEncoder: {AudioEncoder}, " +
                   $"StreamDescription: {StreamDescription}";
        }
    }
}
