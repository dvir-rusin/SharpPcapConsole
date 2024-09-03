using PacketDotNet;
using SharpPcapConsoleProject1.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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
        

        public static void ParseHeaderPayload(byte[][] DataPayload,PAT pat, Packet packet)
        {

            PMT pmt = null;
            PMTinfo pMTinfo = null;
            

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

                            // After parsing the PMT, extract video/audio streams
                            if (pat.PMTs.Count != 0)
                            {
                                foreach (var pmtEntry in pat.PMTs)
                                {
                                    PMT pmt_value = pmtEntry.Value;
                                    if (pmt_value != null)
                                    {
                                        foreach (var esEntry in pmt_value.ElementaryStreams)
                                        {
                                            PMTinfo pmtInfoInstance = esEntry.Value;
                                            if (pmtInfoInstance != null)
                                            {
                                                switch (pmtInfoInstance.StreamType)
                                                {
                                                    case 0x1B: // H.264 video stream
                                                    case 0x24: // H.265 video stream
                                                        Console.WriteLine("Parsing video stream...");
                                                        pMTinfo = Parser.ParsePESPacket(packet, pat, pmtInfoInstance);
                                                        break;
                                                    case 0x0F: // AAC audio stream
                                                    case 0x81: // AC-3 audio stream
                                                        Console.WriteLine("Parsing audio stream...");
                                                        pMTinfo = Parser.ParseAudioStream(pmtInfoInstance, new BinaryReader(new MemoryStream(packet.PayloadPacket.PayloadData)));
                                                        break;
                                                    default:
                                                        Console.WriteLine($"Unknown stream type: {pmtInfoInstance.StreamType}");
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("pat.PMTs.Count == 0");
                            }

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


       
            /// <summary>
            /// Parses the PES packet to extract NAL units and relevant video or audio stream information.
            /// </summary>
            /// <param name="packetPayload">The payload of the PES packet.</param>
            /// <param name="pmtInfo">The PMTinfo object to store extracted stream information.</param>
            public static PMTinfo ParsePESPacket(Packet packet, PAT pat,PMTinfo pmtInfo)
            {
                using (MemoryStream ms = new MemoryStream(packet.Bytes))
                {
                    using (BinaryReader binaryReader = new BinaryReader(ms))
                    {
                        // Verify the PES start code (0x000001)
                        if (binaryReader.ReadByte() == 0x00 && binaryReader.ReadByte() == 0x00 && binaryReader.ReadByte() == 0x01)
                        {
                            // Stream ID and PES packet length
                            
                            int streamId = binaryReader.ReadByte();
                            int pesPacketLength = binaryReader.ReadUInt16();
                            binaryReader.ReadBytes(3); // Skip optional PES header fields
                            int pesHeaderLength = binaryReader.ReadByte();
                            binaryReader.BaseStream.Seek(pesHeaderLength, SeekOrigin.Current); // Skip PES header

                        
                        // Loop to find NAL unit start code within the payload
                        while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length - 3)
                            {
                                if (binaryReader.ReadByte() == 0x00 && binaryReader.ReadByte() == 0x00 && binaryReader.ReadByte() == 0x01)
                                {
                                    ParseNALUnit(binaryReader, pmtInfo);
                                    break;
                                }
                                else
                                {
                                    binaryReader.BaseStream.Seek(-2, SeekOrigin.Current);
                                }
                            }
                        }
                    }
                }
                return pmtInfo; 
            }

            /// <summary>
            /// Parses the NAL unit to extract video details such as frame type, resolution, bit depth, etc.
            /// </summary>
            /// <param name="binaryReader">BinaryReader for reading the NAL unit data.</param>
            /// <param name="pmtInfo">The PMTinfo object to store extracted stream information.</param>
            private static void ParseNALUnit(BinaryReader binaryReader, PMTinfo pmtInfo)
            {
                // Read NAL unit header to determine type
                byte nalHeader = binaryReader.ReadByte();
                int nalUnitType = nalHeader & 0x1F;

                // Parse Sequence Parameter Set (SPS) or Picture Parameter Set (PPS) for video details
                if (nalUnitType == 7 || nalUnitType == 8) // SPS or PPS
                {
                    ParseSPSorPPS(binaryReader, pmtInfo, nalUnitType);
                }
                else if (nalUnitType == 1 || nalUnitType == 5) // Non-IDR (P/B frame) or IDR (I frame)
                {
                    pmtInfo.FrameType = nalUnitType == 5 ? "I" : "P/B";
                    Console.WriteLine($"Frame Type: {pmtInfo.FrameType}");
                }
            }

            /// <summary>
            /// Parses the SPS or PPS to extract critical video information such as resolution, chroma format, bit depth, etc.
            /// </summary>
            /// <param name="binaryReader">BinaryReader for reading the SPS/PPS data.</param>
            /// <param name="pmtInfo">The PMTinfo object to store extracted stream information.</param>
            /// <param name="nalUnitType">The type of the NAL unit (SPS or PPS).</param>
            private static void ParseSPSorPPS(BinaryReader binaryReader, PMTinfo pmtInfo, int nalUnitType)
            {
                if (nalUnitType == 7) // Sequence Parameter Set (SPS)
                {
                    // Parsing the profile and level for video details
                    int profileIdc = binaryReader.ReadByte();
                    binaryReader.ReadByte(); // Skip constraint flags
                    int levelIdc = binaryReader.ReadByte();
                    int spsId = ReadUE(binaryReader);

                    // Parsing chroma format, bit depth, and resolution
                    if (profileIdc == 100 || profileIdc == 110 || profileIdc == 122 || profileIdc == 144)
                    {
                        int chromaFormatIdc = ReadUE(binaryReader);
                        pmtInfo.ChromaFormat = chromaFormatIdc switch
                        {
                            0 => "Monochrome",
                            1 => "4:2:0",
                            2 => "4:2:2",
                            3 => "4:4:4",
                            _ => "Unknown"
                        };
                        Console.WriteLine($"Chroma Format: {pmtInfo.ChromaFormat}");

                        int bitDepthLuma = ReadUE(binaryReader) + 8;
                        pmtInfo.BitDepth = bitDepthLuma;
                        Console.WriteLine($"Bit Depth: {pmtInfo.BitDepth}");
                        ReadUE(binaryReader); // Chroma bit depth
                        binaryReader.ReadByte(); // Transform bypass flag
                    }

                    // Parsing the resolution from picWidth and picHeight
                    ReadUE(binaryReader); // log2_max_frame_num_minus4
                    int picOrderCntType = ReadUE(binaryReader);
                    if (picOrderCntType == 0)
                    {
                        ReadUE(binaryReader); // log2_max_pic_order_cnt_lsb_minus4
                    }

                    ReadUE(binaryReader); // num_ref_frames
                    binaryReader.ReadByte(); // gaps_in_frame_num_value_allowed_flag

                    int picWidthInMbsMinus1 = ReadUE(binaryReader);
                    int picHeightInMapUnitsMinus1 = ReadUE(binaryReader);
                    bool frameMbsOnlyFlag = (binaryReader.ReadByte() & 0x80) != 0;

                    int width = (picWidthInMbsMinus1 + 1) * 16;
                    int height = (picHeightInMapUnitsMinus1 + 1) * 16 * (frameMbsOnlyFlag ? 1 : 2);
                    pmtInfo.Resolution = $"{width}x{height}";
                    Console.WriteLine($"Resolution: {pmtInfo.Resolution}");

                    // Extracting frame rate from VUI parameters if present
                    bool vuiParametersPresentFlag = (binaryReader.ReadByte() & 0x01) != 0;
                    if (vuiParametersPresentFlag)
                    {
                        ParseVUIParameters(binaryReader, pmtInfo);
                    }
                }
            }

            /// <summary>
            /// Parses the VUI (Video Usability Information) parameters to extract frame rate and other video details.
            /// </summary>
            /// <param name="binaryReader">BinaryReader for reading the VUI parameters.</param>
            /// <param name="pmtInfo">The PMTinfo object to store extracted stream information.</param>
            private static void ParseVUIParameters(BinaryReader binaryReader, PMTinfo pmtInfo)
            {
                // Parsing aspect ratio, overscan, video signal type, and timing information
                bool aspectRatioInfoPresentFlag = (binaryReader.ReadByte() & 0x80) != 0;
                if (aspectRatioInfoPresentFlag)
                {
                    int aspectRatioIdc = binaryReader.ReadByte();
                    if (aspectRatioIdc == 255) // Extended_SAR
                    {
                        binaryReader.ReadBytes(4); // Skip next 4 bytes
                    }
                }

                bool timingInfoPresentFlag = (binaryReader.ReadByte() & 0x04) != 0;
                if (timingInfoPresentFlag)
                {
                    uint numUnitsInTick = binaryReader.ReadUInt32();
                    uint timeScale = binaryReader.ReadUInt32();
                    bool fixedFrameRateFlag = (binaryReader.ReadByte() & 0x01) != 0;

                    if (fixedFrameRateFlag)
                    {
                        pmtInfo.FrameRate = timeScale / (2.0 * numUnitsInTick);
                        Console.WriteLine($"Frame Rate: {pmtInfo.FrameRate}");
                    }
                }
            }

            /// <summary>
            /// Parses and identifies the audio stream, setting the appropriate audio encoder name.
            /// </summary>
            /// <param name="pmtInfo">The PMTinfo object to store extracted audio stream information.</param>
            /// <param name="binaryReader">BinaryReader for reading the audio stream data.</param>
            public static PMTinfo ParseAudioStream(PMTinfo pmtInfo, BinaryReader binaryReader)
            {
                // Simplified example: Assume audio stream uses common encoders
                switch (pmtInfo.StreamType)
                {
                    case 0x0F: // AAC
                        pmtInfo.AudioEncoder = "AAC";
                        break;
                    case 0x81: // AC-3
                        pmtInfo.AudioEncoder = "AC-3";
                        break;
                    default:
                        pmtInfo.AudioEncoder = "Unknown";
                        break;
                }

                Console.WriteLine($"Audio Encoder: {pmtInfo.AudioEncoder}");
                return pmtInfo;
            }

            /// <summary>
            /// Reads an unsigned Exp-Golomb-coded integer from the bitstream.
            /// </summary>
            /// <param name="reader">BinaryReader to read from.</param>
            /// <returns>The decoded unsigned integer.</returns>
            private static int ReadUE(BinaryReader reader)
            {
                int zeroBits = 0;
                while (reader.ReadByte() == 0)
                {
                    zeroBits++;
                }
                int result = (1 << zeroBits) - 1 + reader.ReadByte();
                return result;
            }

            /// <summary>
            /// Reads a signed Exp-Golomb-coded integer from the bitstream.
            /// </summary>
            /// <param name="reader">BinaryReader to read from.</param>
            /// <returns>The decoded signed integer.</returns>
            private static int ReadSE(BinaryReader reader)
            {
                int value = ReadUE(reader);
                return ((value & 1) == 0) ? -(value >> 1) : (value >> 1);
            }

        

    }
}
