using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpPcapConsoleProject1.DataTypes
{
    public class TransportPackets
    {

        public TransportPackets()
        {

        }

        public TransportPackets(TransportPackets packet)
        {
            SyncByte = packet.SyncByte;
            TransportErrorIndicator = packet.TransportErrorIndicator;
            PayloadUnitStratIndicator = packet.PayloadUnitStratIndicator;
            TransportPriority = packet.TransportPriority;
            PID = packet.PID;
            TransportScramblingControl = packet.TransportScramblingControl;
            AdaptationFieldControl = packet.AdaptationFieldControl;

            ContinuityCounter = packet.ContinuityCounter;

            AdaptationFieldPresent = packet.AdaptationFieldPresent;
            AdaptationFieldLength = packet.AdaptationFieldLength;
            DiscontinuityIndicator = packet.DiscontinuityIndicator;
            RandomAccessIndicator = packet.RandomAccessIndicator;
            ElementaryStreamPriorityIndicator = packet.ElementaryStreamPriorityIndicator;
            PCRFlag = packet.PCRFlag;
            OPCRFlag = packet.OPCRFlag;
            SplicingPointFlag = packet.SplicingPointFlag;
            TransportPrivateDataFlag = packet.TransportPrivateDataFlag;
            AdaptationFieldExtsionlength = packet.AdaptationFieldExtsionlength;

            PCR = packet.PCR;
            OPCR = packet.OPCR;
            SpliceCountdown = packet.SpliceCountdown;
            TransportPrivateDataLength = packet.TransportPrivateDataLength;
            TransportPrivateData = packet.TransportPrivateData;
            AdaptationFieldExtsionlength = packet.AdaptationFieldExtsionlength;

            ItwFlag = packet.ItwFlag;
            PiecewiseRateFlag = packet.PiecewiseRateFlag;

            ItwValidFlag = packet.ItwValidFlag;
            SeamlessSPliceFlag = packet.SeamlessSPliceFlag;
            ItwOffSet = packet.ItwOffSet;

            PieceWiseRate = packet.PieceWiseRate;
            SpliceType = packet.SpliceType;
            DTSNextAu = packet.DTSNextAu;

        }

        public int SyncByte { get; set; }
        public bool TransportErrorIndicator { get; set; }
        public bool PayloadUnitStratIndicator { get; set; }
        public bool TransportPriority { get; set; }
        public int PID { get; set; }
        public int TransportScramblingControl { get; set; }
        public int AdaptationFieldControl { get; set; }
        public int ContinuityCounter { get; set; }


        public bool AdaptationFieldPresent { get; set; }
        public int? AdaptationFieldLength { get; set; }
        public bool? DiscontinuityIndicator { get; set; }
        public bool? RandomAccessIndicator { get; set; }
        public bool? ElementaryStreamPriorityIndicator { get; set; }
        public bool? PCRFlag { get; set; }
        public bool? OPCRFlag { get; set; }
        public bool? SplicingPointFlag { get; set; }
        public bool? TransportPrivateDataFlag { get; set; }
        public bool? AdaptationFieldExtentionFlag { get; set; }



        public long? PCR { get; set; }
        public long? OPCR { get; set; }
        public int? SpliceCountdown { get; set; }
        public int? TransportPrivateDataLength { get; set; }
        public byte[]? TransportPrivateData { get; set; }
        public int? AdaptationFieldExtsionlength { get; set; }


        public bool? ItwFlag { get; set; }
        public bool? PiecewiseRateFlag { get; set; }
        public bool? ItwValidFlag { get; set; }
        public bool? SeamlessSPliceFlag { get; set; }
        public int? ItwOffSet { get; set; }

        public int? PieceWiseRate { get; set; }
        public int? SpliceType { get; set; }
        public long? DTSNextAu { get; set; }

    }
}
