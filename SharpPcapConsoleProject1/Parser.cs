using SharpPcapConsoleProject1.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpPcapConsoleProject1
{
    //parser class is responsible for parsing the 4 byte packet header using TransportPacket object
    //parsing pmt : 184 byte packet data paylod information into PMT object
    //parsing pat : 184 byte packet data paylod information into PAT object
    public class Parser
    {
        //checks if the system is little endian or big endian
        public static bool Endian = BitConverter.IsLittleEndian;



        //the function reacives PAT pat and byte[][] DataPayload
        //goes through the DataPayload and extracts the byte information from the header into the TransportPackets object
        // if the pat is null and pid=0 -> call the ParsePAT function
        // if the pat != null and the pid is in the pat.PMTs dictionary -> calls the ParsePMT function
        // if the pat != null and the pid is not in the pat.PMTs dictionary -> prints the pid
        

        public static void ParseHeaderPayload(byte[][] DataPayload,PAT pat)
        {

            PMT pmt = null;
            

            for (int i = 0; i < DataPayload.Length - 1; i++)
            {

                //check if the row is null
                if (DataPayload[i] == null)
                {
                    Console.WriteLine("error : DataPayload[i] == null, row was null");
                    return;
                }


                //give the first data payload to firstDataPayload
                byte[] firstPayload = DataPayload[i];

                using (MemoryStream ms = new MemoryStream(firstPayload))
                {
                    using (BinaryReader reader = new BinaryReader(ms))
                    {
                        TransportPackets TransportPacket = new TransportPackets();
                        TransportPacket.SyncByte = reader.ReadByte();
                        byte currentByte = reader.ReadByte();
                        TransportPacket.TransportErrorIndicator = (currentByte >> 7 > 0);
                        TransportPacket.PayloadUnitStratIndicator = (currentByte >> 6 > 0);
                        TransportPacket.TransportPriority = (currentByte >> 5 > 0);
                        TransportPacket.PID = (currentByte & 0x1f);
                        currentByte = reader.ReadByte();
                        TransportPacket.PID = TransportPacket.PID << 8;
                        TransportPacket.PID = TransportPacket.PID | currentByte;
                        currentByte = reader.ReadByte();
                        Console.WriteLine($"{TransportPacket.PID}");
                        TransportPacket.TransportScramblingControl = (short)((currentByte & 0xc0) >> 6);
                        TransportPacket.AdaptationFieldControl = (short)((currentByte & 0x30) >> 4);
                        TransportPacket.ContinuityCounter = (short)((currentByte & 0x0F));
                        if (TransportPacket.AdaptationFieldControl > 2)
                        {
                            TransportPacket.AdaptationFieldPresent = true;
                            TransportPacket.AdaptationFieldLength = reader.ReadByte();
                            if (TransportPacket.AdaptationFieldLength > 0)
                            {
                                currentByte = reader.ReadByte();
                                TransportPacket.DiscontinuityIndicator = (currentByte & 0x80) > 0;
                                TransportPacket.RandomAccessIndicator = (currentByte & 0x40) > 0;
                                TransportPacket.ElementaryStreamPriorityIndicator = (currentByte & 0x20) > 0;
                                TransportPacket.PCRFlag = (currentByte & 0x10) > 0;
                                TransportPacket.OPCRFlag = (currentByte & 0x01) > 0;
                                TransportPacket.SplicingPointFlag = (currentByte & 0x04) > 0;
                                TransportPacket.TransportPrivateDataFlag = (currentByte & 0x02) > 0;
                                TransportPacket.AdaptationFieldExtentionFlag = (currentByte & 0X01) > 0;

                                if (TransportPacket.PCRFlag.Value)
                                {
                                    TransportPacket.PCR = BitConverter.ToInt64(reader.ReadBytes(6), 0);
                                }

                                if (TransportPacket.OPCRFlag.Value)
                                {
                                    TransportPacket.OPCR = BitConverter.ToInt64(reader.ReadBytes(6), 0);
                                }

                                if (TransportPacket.SplicingPointFlag.Value)
                                {
                                    TransportPacket.SpliceCountdown = reader.ReadByte();
                                }

                                if (TransportPacket.TransportPrivateDataFlag.Value)
                                {
                                    TransportPacket.TransportPrivateDataLength = reader.ReadByte();
                                    TransportPacket.TransportPrivateData = reader.ReadBytes(TransportPacket.TransportPrivateDataLength.Value);
                                }

                                if (TransportPacket.AdaptationFieldExtentionFlag.Value)
                                {
                                    TransportPacket.AdaptationFieldExtsionlength = reader.ReadByte();
                                    currentByte = reader.ReadByte();
                                    TransportPacket.ItwFlag = (currentByte & 0x80) > 0;
                                    TransportPacket.PiecewiseRateFlag = (currentByte & 0x40) > 0;
                                    TransportPacket.SeamlessSPliceFlag = (currentByte & 0x20) > 0;
                                    if (TransportPacket.ItwFlag.Value)
                                    {
                                        currentByte = reader.ReadByte();
                                        if ((TransportPacket.ItwValidFlag = (currentByte & 0x80) > 0).Value)
                                        {
                                            TransportPacket.ItwOffSet = ((currentByte & 0x7F) << 8) | reader.ReadByte();
                                        }

                                    }
                                    if (TransportPacket.PiecewiseRateFlag.Value)
                                    {
                                        currentByte = reader.ReadByte();
                                        TransportPacket.PieceWiseRate = ((currentByte & 0x5F) << 8) | reader.ReadByte() << 8 | reader.ReadByte();
                                    }
                                    if (TransportPacket.SeamlessSPliceFlag.Value)
                                    {
                                        currentByte = reader.ReadByte();
                                        TransportPacket.SpliceType = (short)currentByte >> 4;
                                        TransportPacket.DTSNextAu = currentByte & 0x0e << 32;
                                        TransportPacket.DTSNextAu = reader.ReadByte() & 0xfe << 29;
                                        TransportPacket.DTSNextAu = reader.ReadByte() & 0XFE << 14;
                                    }
                                }
                            }
                            if ((TransportPacket.AdaptationFieldControl & 1) == 1)
                            {
                                reader.BaseStream.Seek(TransportPacket.AdaptationFieldLength.Value + 4, SeekOrigin.Current);
                            }


                        }
                        if (TransportPacket.PayloadUnitStratIndicator)
                            reader.BaseStream.Seek(1, SeekOrigin.Current);
                        if (pat == null)
                        {
                            if (TransportPacket.PID == 0)
                            {
                                Console.WriteLine("PID is 0 -> arrived to PAT");
                                string stringPidOfBitArray1 = Convert.ToString(TransportPacket.PID, 2).PadLeft(13, '0');
                                Console.WriteLine("PAT bit array representation: " + stringPidOfBitArray1);
                                pat = Parser.ParsePAT(reader, TransportPacket);
                            }
                        }

                        else if (pat.PMTs.ContainsKey(TransportPacket.PID))
                        {
                            Console.WriteLine(" pat.pmts contains PMT pid");
                            string stringPidOfBitArray2 = Convert.ToString(TransportPacket.PID, 2).PadLeft(13, '0');
                            Console.WriteLine("PMT bit array representation: " + stringPidOfBitArray2);
                            pmt = Parser.ParsePMT(reader, TransportPacket);
                            pat.PMTs[TransportPacket.PID] = pmt;// very important change adds the new pmt to the pat dictionary
                        }
                        else
                        {
                            // Display the extracted PID as a binary string
                            string stringPidOfBitArray = Convert.ToString(TransportPacket.PID, 2).PadLeft(13, '0');
                            Console.WriteLine("pat not null and pmt pid not found in dic bit array representation: " + stringPidOfBitArray);

                        }



                    }
                }
            }
        }



        //parses the PAT packet, creates a new PAT object with the packet header , and returns it with the extracted information
        //extracts the table ID, SectionSyntaxIndicator, SectionLength, TransportStreamID, VersionNumber,
        //CurrentNextIndicator, SectionNumber, LastSectionNumber ,program numbers, PMT PID and CRC32 and adds them to the PAT object

        public static PAT ParsePAT(BinaryReader binaryReader, TransportPackets packet)
        {
            //creates a pat(with packet header)
            PAT pat = new(packet);
            int currentByte;
            //creates counter that counts the pat programs
            int ProgramCounter = 0; 

            // Extract 8 bit Table ID
            currentByte = binaryReader.ReadByte();
            pat.TableID = currentByte;
            Console.WriteLine($"Pat tableID: {pat.TableID}");

            // Extract 1 bit SectionSyntaxIndicator
            currentByte = binaryReader.ReadByte();
            pat.SectionSyntaxIndicator = (currentByte & 0x80) != 0;
            Console.WriteLine($"Pat SectionSyntaxIndicator: {pat.SectionSyntaxIndicator}");

            // Extract 12 bit SectionLength
            pat.SectionLength = ((currentByte & 0x0F) << 8) | binaryReader.ReadByte();
            Console.WriteLine($"Pat SectionLength: {pat.SectionLength}");

            // Extract the 16 bit transport stream ID
            pat.TransportStreamID = (binaryReader.ReadByte() << 8) | binaryReader.ReadByte();
            Console.WriteLine($"Pat transportStreamID: {pat.TransportStreamID}");

            // Extract 5 bit VersionNumber
            currentByte = binaryReader.ReadByte();
            pat.VersionNumber = ((currentByte & 0b00111110) >> 1);
            Console.WriteLine($"Pat VersionNumber: {pat.VersionNumber}");

            //Extract 1 bit CurrentNextIndicator
            pat.CurrentNextIndicator = (currentByte & 0x01) != 0;
            Console.WriteLine($"Pat CurrentNextIndicator: {pat.CurrentNextIndicator}");

            //Extract 8 bit SectionNumber
            currentByte = binaryReader.ReadByte();
            pat.SectionNumber = currentByte;
            Console.WriteLine($"Pat SectionNumber: {pat.SectionNumber}");

            //Extract 8 bit LastSectionNumber
            currentByte = binaryReader.ReadByte();
            pat.LastSectionNumber = currentByte;
            Console.WriteLine($"Pat LastSectionNumber: {pat.LastSectionNumber}");
            

            // Now, loop through the programs
            while (((pat.SectionLength - 9) / 4) > ProgramCounter) // +2 to add the section length size -4 to ignore CRC_32
            {
                // pat or pmt Program number
                int programNumber = BitConverter.ToInt16(binaryReader.ReadBytes(2), 0);
                byte[] bytes = BigEndian.BigEndianReadBytes(binaryReader, 2, Endian);
                if (programNumber == 0)
                {
                    //PAT PID 
                    pat.NetworkPID = (BitConverter.ToInt16(bytes, 0) & 0b1111111111111); //13 bit PAT PID
                }
                else
                {
                    //PMT PID 
                    pat.PMTs.Add((BitConverter.ToInt16(bytes, 0) & 0b1111111111111), new PMT());//13 bit PMT PID
                }
                ProgramCounter++;
            }

            //extract CRC32 value and replace it with the temperery one in the pat 
            pat.CRC32 = BitConverter.ToInt16(BigEndian.BigEndianReadBytes(binaryReader, 4, Endian), 0);
            Console.WriteLine($"pat.CRC32: {pat.CRC32}");

            return pat;
            

        }
        
        //parses the PMT packet, creates a new PMT object with the packet header , and returns it with the extracted information
        //extracts the table ID, SectionSyntaxIndicator, SectionLength, ProgramNumber, VersionNumber, CurrentNextIndicator, SectionNumber, LastSectionNumber, PCR_PID, ProgramInfoLength, ProgramInfoDescriptors, ElementaryStreams and CRC32 and adds them to the PMT object
        //extracts the elementary streams and identifies the stream type
        public static PMT ParsePMT(BinaryReader binaryReader, TransportPackets packet)
        {
            PMT pmt = new PMT(packet);

            // Extract the Table ID (8 bits)
            pmt.TableID = binaryReader.ReadByte();
            Console.WriteLine($"Table ID: 0x{pmt.TableID:X2} ({pmt.TableID})");

            // Section Syntax Indicator (1 bit) + 3 bits reserved + Section Length (12 bits)
            int currentByte = binaryReader.ReadByte();
            pmt.SectionSyntaxIndicator = (currentByte >> 7) > 0;
            pmt.SectionLength = ((currentByte & 0x0F) << 8) | binaryReader.ReadByte();
            Console.WriteLine($"PMT Section Syntax Indicator: {pmt.SectionSyntaxIndicator}");
            Console.WriteLine($"PMT Section Length: {pmt.SectionLength}");

            // Program Number (16 bits)
            pmt.ProgramNumber = BitConverter.ToInt16(BigEndian.BigEndianReadBytes(binaryReader, 2, Endian), 0);
            Console.WriteLine($"PMT Program Number: {pmt.ProgramNumber}");

            // Reserved (2 bits) + Version Number (5 bits) + Current/Next Indicator (1 bit)
            currentByte = binaryReader.ReadByte();
            pmt.VersionNumber = (currentByte & 0x3E) >> 1;
            pmt.CurrentNextIndicator = (currentByte & 0x01) != 0;
            Console.WriteLine($"PMT Version Number: {pmt.VersionNumber}");
            Console.WriteLine($"PMT Current/Next Indicator: {pmt.CurrentNextIndicator}");

            // Section Number (8 bits)
            pmt.SectionNumber = binaryReader.ReadByte();
            Console.WriteLine($"PMT Section Number: {pmt.SectionNumber}");

            // Last Section Number (8 bits)
            pmt.LastSectionNumber = binaryReader.ReadByte();
            Console.WriteLine($"PMT Last Section Number: {pmt.LastSectionNumber}");

            // Reserved (3 bits) + PCR PID (13 bits)
            currentByte = binaryReader.ReadByte();
            pmt.PCR_PID = ((currentByte & 0x1F) << 8) | binaryReader.ReadByte();
            Console.WriteLine($"PMT PCR PID: 0x{pmt.PCR_PID:X4} ({pmt.PCR_PID})");

            // Reserved (4 bits) + Program Info Length (12 bits)
            currentByte = binaryReader.ReadByte();
            pmt.ProgramInfoLength = ((currentByte & 0x0F) << 8) | binaryReader.ReadByte();
            Console.WriteLine($"PMT Program Info Length: {pmt.ProgramInfoLength}");


            int counter = 0;
            pmt.ProgramInfoDescriptors = BigEndian.BigEndianReadBytes(binaryReader, pmt.ProgramInfoLength, Endian);

            // Now, loop through the elementary streams
            while (binaryReader.BaseStream.Position - 2 < pmt.SectionLength) // Account 2 byte it takes to calc programInfoLength and for the 4 byte CRC_32 at the end
            {
                PMTinfo pmtinfo = new PMTinfo();

                // Stream Type (8 bits)
                pmtinfo.StreamType = binaryReader.ReadByte();
                Console.WriteLine($"PMT Stream Type: 0x{pmtinfo.StreamType:X2} ({pmtinfo.StreamType})");

                // Reserved (3 bits) + Elementary PID (13 bits)
                pmtinfo.ElementaryPID = ((binaryReader.ReadByte() & 0x1F) << 8) | binaryReader.ReadByte();
                Console.WriteLine($"PMT Elementary PID: 0x{pmtinfo.ElementaryPID:X4} ({pmtinfo.ElementaryPID})");

                // Reserved (4 bits) + ES Info Length (12 bits)
                pmtinfo.ESinfoLength = ((binaryReader.ReadByte() & 0x0F) << 8) | binaryReader.ReadByte();
                Console.WriteLine($"PMT ES Info Length: {pmtinfo.ESinfoLength}");

                // ES Info Descriptors (variable length based on ES Info Length)
                pmtinfo.Descriptors = BigEndian.BigEndianReadBytes(binaryReader, pmtinfo.ESinfoLength,Endian);

                // Add the elementary stream Pid and pmtinfo object into the dictionary
                pmt.AddElementaryStream(pmtinfo.ElementaryPID, pmtinfo);

                // Identifying the stream type
                switch (pmtinfo.StreamType)
                {
                    case 0x1B: // H.264 video
                        Console.WriteLine($"Found H.264 video stream with PID {pmtinfo.ElementaryPID:X}");
                        break;
                    case 0x0F: // AAC audio
                        Console.WriteLine($"Found AAC audio stream with PID {pmtinfo.ElementaryPID:X}");
                        break;
                    case 0x06: // Subtitles or other
                        Console.WriteLine($"Found Subtitles or other stream with PID {pmtinfo.ElementaryPID:X}");
                        break;
                    default:
                        Console.WriteLine($"Unknown stream type {pmtinfo.StreamType:X} with PID {pmtinfo.ElementaryPID:X}");
                        break;
                }
            }
            pmt.CRC32 = new byte[4];
            // CRC_32 (32 bits)
            for (int i = 0; i < pmt.CRC32.Length - 1; i++)
            {
                pmt.CRC32[i] = binaryReader.ReadByte();
            }

            Console.WriteLine($"PMT CRC_32: {BitConverter.ToString(pmt.CRC32).Replace("-", " ")}");

            return pmt;
        }
  
    }
}
