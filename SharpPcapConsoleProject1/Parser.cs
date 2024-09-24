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
                            Console.WriteLine(" pat.pmts contains PMT pid {0} :", TransportPacket.PID);
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
                                                        pat.PMTs[ TransportPacket.PID ].ElementaryStreams[pmtInfoInstance.ElementaryPID] = pMTinfo;// very important change adds the new es to the pmt dictionary
                                                        Console.WriteLine("added video es to pmt dictionary with the following info:");
                                                        pMTinfo.ToString();
                                                        break;
                                                    case 0x0F: // AAC audio stream
                                                    case 0x81: // AC-3 audio stream
                                                        Console.WriteLine("Parsing audio stream...");
                                                        pMTinfo = Parser.ParseAudioStream(pmtInfoInstance, new BinaryReader(new MemoryStream(packet.PayloadPacket.Bytes)));
                                                        pat.PMTs[ TransportPacket.PID ].ElementaryStreams[pmtInfoInstance.ElementaryPID] = pMTinfo;// very important change adds the new es to the pmt dictionary
                                                        Console.WriteLine("added audio es to pmt dictionary with the following info:");
                                                        pMTinfo.ToString();
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
                            int pesPacketLength = binaryReader.ReadByte() << 8 | binaryReader.ReadByte();
                        int flag = binaryReader.ReadByte() <<8| binaryReader.ReadByte();
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
                                    // Move back two bytes and continue searching
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
                int forbiddenZeroBit = (nalHeader & 0x80) >> 7;
                int nalRefIdc = (nalHeader & 0x60) >> 5;
                int nalUnitType = nalHeader & 0x1F;

                // Parse based on nal_unit_type
                switch (nalUnitType)
                {
                    case 7: // SPS
                    case 8: // PPS
                        ParseSPSorPPS(binaryReader, pmtInfo, nalUnitType);
                        break;
                    case 5: // IDR (I frame)
                        pmtInfo.FrameType = "I";
                        Console.WriteLine($"Frame Type: {pmtInfo.FrameType}");
                        break;
                    case 1: // Non-IDR (P/B frame)
                        pmtInfo.FrameType = "P/B";
                        Console.WriteLine($"Frame Type: {pmtInfo.FrameType}");
                        break;
                    case 9: // Access Unit Delimiter
                        ParseAccessUnitDelimiter(binaryReader, pmtInfo);
                        break;
                    default:
                        // Skip or handle other NAL unit types if necessary
                        break;
                }
            }

            private static void ParseAccessUnitDelimiter(BinaryReader binaryReader, PMTinfo pmtInfo)
            {
                BitReader bitReader = new BitReader(binaryReader);

                // Read primary_pic_type (3 bits)
                int primary_pic_type = (int)bitReader.ReadBits(3);

                // Map primary_pic_type to frame type
                pmtInfo.FrameType = primary_pic_type switch
                {
                    0 or 1 or 2 => "I",
                    3 or 4 or 5 => "P",
                    6 or 7 or 8 => "B",
                    _ => "Unknown",
                };

                Console.WriteLine($"Frame Type (from AUD): {pmtInfo.FrameType}");

                // rbsp_stop_one_bit (1 bit)
                bitReader.ReadBit(); // Should be '1'

                // rbsp_alignment_zero_bit(s): Read until byte-aligned
                while (!bitReader.IsByteAligned)
                {
                    int alignment_bit = bitReader.ReadBit();
                    if (alignment_bit != 0)
                    {
                        // Should be zero
                        throw new Exception("Invalid rbsp_alignment_zero_bit");
                    }
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
                // Initialize BitReader
                BitReader bitReader = new BitReader(binaryReader);
                if (nalUnitType == 7) // Sequence Parameter Set (SPS)
                {
                    // Parsing the profile and level for video details
                    int profile_idc = binaryReader.ReadByte();
                    binaryReader.ReadByte(); // Skip constraint flags
                    int levelIdc = binaryReader.ReadByte();
                    uint spsId = bitReader.ReadUE();

                    // Initialize default values
                    uint chroma_format_idc = 1;
                    uint bit_depth_luma_minus8 = 0;

                // Parsing chroma format, bit depth, and resolution
                // High profile specifics
                if (profile_idc == 100 || profile_idc == 110 || profile_idc == 122 ||
                    profile_idc == 244 || profile_idc == 44 || profile_idc == 83 ||
                    profile_idc == 86 || profile_idc == 118 || profile_idc == 128 ||
                    profile_idc == 138 || profile_idc == 144)
                {
                    chroma_format_idc = bitReader.ReadUE();
                    if (chroma_format_idc == 3)
                    {
                        // separate_colour_plane_flag
                        bitReader.ReadBit();
                    }
                    bit_depth_luma_minus8 = bitReader.ReadUE();
                    bitReader.ReadUE(); // bit_depth_chroma_minus8
                    bitReader.ReadBit(); // qpprime_y_zero_transform_bypass_flag
                    bool seq_scaling_matrix_present_flag = bitReader.ReadBit() != 0;
                    if (seq_scaling_matrix_present_flag)
                    {
                        int scalingListCount = (chroma_format_idc != 3) ? 8 : 12;
                        for (int i = 0; i < scalingListCount; i++)
                        {
                            bool seq_scaling_list_present_flag = bitReader.ReadBit() != 0;
                            if (seq_scaling_list_present_flag)
                            {
                                // Skipping scaling_list() parsing for simplicity
                                SkipScalingList(bitReader, i < 6 ? 16 : 64);
                            }
                        }
                    }
                }

                bitReader.ReadUE(); // log2_max_frame_num_minus4
                uint pic_order_cnt_type = bitReader.ReadUE();

                if (pic_order_cnt_type == 0)
                {
                    bitReader.ReadUE(); // log2_max_pic_order_cnt_lsb_minus4
                }

                else if (pic_order_cnt_type == 1)
                {
                    bitReader.ReadBit(); // delta_pic_order_always_zero_flag
                    bitReader.ReadSE(); // offset_for_non_ref_pic
                    bitReader.ReadSE(); // offset_for_top_to_bottom_field
                    uint num_ref_frames_in_pic_order_cnt_cycle = bitReader.ReadUE();
                    for (int i = 0; i < num_ref_frames_in_pic_order_cnt_cycle; i++)
                    {
                        bitReader.ReadSE(); // offset_for_ref_frame[i]
                    }
                }

                bitReader.ReadUE(); // max_num_ref_frames
                bitReader.ReadBit(); // gaps_in_frame_num_value_allowed_flag
                uint pic_width_in_mbs_minus1 = bitReader.ReadUE();
                uint pic_height_in_map_units_minus1 = bitReader.ReadUE();
                bool frame_mbs_only_flag = bitReader.ReadBit() != 0;
                if (!frame_mbs_only_flag)
                {
                    bitReader.ReadBit(); // mb_adaptive_frame_field_flag
                }
                bitReader.ReadBit(); // direct_8x8_inference_flag
                bool frame_cropping_flag = bitReader.ReadBit() != 0;
                uint frame_crop_left_offset = 0;
                uint frame_crop_right_offset = 0;
                uint frame_crop_top_offset = 0;
                uint frame_crop_bottom_offset = 0;
                if (frame_cropping_flag)
                {
                    frame_crop_left_offset = bitReader.ReadUE();
                    frame_crop_right_offset = bitReader.ReadUE();
                    frame_crop_top_offset = bitReader.ReadUE();
                    frame_crop_bottom_offset = bitReader.ReadUE();
                }
                bool vui_parameters_present_flag = bitReader.ReadBit() != 0;
                double frameRate = 0.0;
                if (vui_parameters_present_flag)
                {
                    frameRate = ParseVUIParameters(bitReader);
                    pmtInfo.FrameRate = frameRate;
                }

                // Calculate width and height
                uint width = (pic_width_in_mbs_minus1 + 1) * 16;
                uint height = (pic_height_in_map_units_minus1 + 1) * 16;
                if (!frame_mbs_only_flag)
                {
                    height *= 2;
                }

                // Apply cropping offsets
                int crop_unit_x = 1;
                int crop_unit_y = 2 - (frame_mbs_only_flag ? 1 : 0);
                if (chroma_format_idc == 1) // 4:2:0
                {
                    crop_unit_x = 2;
                    crop_unit_y *= 2;
                }
                else if (chroma_format_idc == 2) // 4:2:2
                {
                    crop_unit_x = 2;
                }

                width -= (frame_crop_left_offset + frame_crop_right_offset) * (uint)crop_unit_x;
                height -= (frame_crop_top_offset + frame_crop_bottom_offset) * (uint)crop_unit_y;

                // Update PMTinfo with extracted values
                pmtInfo.Resolution = $"{width}x{height}";
                pmtInfo.BitDepth = (int)(bit_depth_luma_minus8 + 8);
                pmtInfo.ChromaFormat = chroma_format_idc switch
                {
                    0 => "Monochrome",
                    1 => "4:2:0",
                    2 => "4:2:2",
                    3 => "4:4:4",
                    _ => "Unknown",
                };

                Console.WriteLine($"Resolution: {pmtInfo.Resolution}");
                Console.WriteLine($"Bit Depth: {pmtInfo.BitDepth}");
                Console.WriteLine($"Chroma Format: {pmtInfo.ChromaFormat}");
                Console.WriteLine($"Frame Rate: {pmtInfo.FrameRate}");
            }
        }

        private static void SkipScalingList(BitReader bitReader, int sizeOfScalingList)
        {
            int lastScale = 8;
            int nextScale = 8;
            for (int j = 0; j < sizeOfScalingList; j++)
            {
                if (nextScale != 0)
                {
                    int delta_scale = bitReader.ReadSE();
                    nextScale = (lastScale + delta_scale + 256) % 256;
                }
                lastScale = (nextScale == 0) ? lastScale : nextScale;
            }
        }
        /// <summary>
        /// Parses the VUI (Video Usability Information) parameters to extract frame rate and other video details.
        /// </summary>
        /// <param name="binaryReader">BinaryReader for reading the VUI parameters.</param>
        /// <param name="pmtInfo">The PMTinfo object to store extracted stream information.</param>
        private static double ParseVUIParameters(BitReader bitReader)
        {
            bool aspect_ratio_info_present_flag = bitReader.ReadBit() != 0;
            if (aspect_ratio_info_present_flag)
            {
                int aspect_ratio_idc = (int)bitReader.ReadBits(8);
                if (aspect_ratio_idc == 255) // Extended_SAR
                {
                    bitReader.ReadBits(16); // sar_width
                    bitReader.ReadBits(16); // sar_height
                }
            }
            bool overscan_info_present_flag = bitReader.ReadBit() != 0;
            if (overscan_info_present_flag)
            {
                bitReader.ReadBit(); // overscan_appropriate_flag
            }
            bool video_signal_type_present_flag = bitReader.ReadBit() != 0;
            if (video_signal_type_present_flag)
            {
                bitReader.ReadBits(3); // video_format
                bitReader.ReadBit();   // video_full_range_flag
                bool colour_description_present_flag = bitReader.ReadBit() != 0;
                if (colour_description_present_flag)
                {
                    bitReader.ReadBits(8); // colour_primaries
                    bitReader.ReadBits(8); // transfer_characteristics
                    bitReader.ReadBits(8); // matrix_coefficients
                }
            }
            bool chroma_loc_info_present_flag = bitReader.ReadBit() != 0;
            if (chroma_loc_info_present_flag)
            {
                bitReader.ReadUE(); // chroma_sample_loc_type_top_field
                bitReader.ReadUE(); // chroma_sample_loc_type_bottom_field
            }
            bool timing_info_present_flag = bitReader.ReadBit() != 0;
            double frameRate = 0.0;
            if (timing_info_present_flag)
            {
                uint num_units_in_tick = (uint)bitReader.ReadBits(32);
                uint time_scale = (uint)bitReader.ReadBits(32);
                bool fixed_frame_rate_flag = bitReader.ReadBit() != 0;

                if (num_units_in_tick != 0)
                {
                    frameRate = time_scale / (2.0 * num_units_in_tick);
                }
            }
            // Skipping additional VUI parameters for brevity
            return frameRate;
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

          
    }
}
