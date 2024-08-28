using System;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using PacketDotNet;
using PacketDotNet.Ieee80211;
using PacketDotNet.Utils.Converters;
using SharpPcap;
using SharpPcap.LibPcap;
using SharpPcapConsoleProject1;

namespace NpcapRemoteCapture
{
    public class Program
    {
        public static bool Endian = BitConverter.IsLittleEndian;
        //global information 
        public static PAT pat;
        private static readonly int targetPort = 2000;
        private static readonly string targetIpAddress = "224.0.0.0";
        static int ByteSize = 8;

        //static 
        static int Packetcount = 0;
        private static bool patFound = false;
        private static Dictionary<int, PAT> patDictionary = new Dictionary<int, PAT>(); // Dictionary to hold all PATs
        TransportPackets packet = new TransportPackets();
        
        byte currentByte = 0;
        static void Main(string[] args)
        {
            
            // List all available devices
            var devices = CaptureDeviceList.Instance;
            Console.WriteLine("Available devices:");
            for (int i = 0; i < devices.Count; i++)
            {
                Console.WriteLine("{0}: {1}", i, devices[i].Description);
            }


            // Ensure we have at least one device to capture from
            if (devices.Count < 1)
            {
                Console.WriteLine("No devices were found on this machine");
                return;
            }


            // Specify the device index for the loopback device (adjust if necessary)
            int loopbackDeviceIndex = 7; // Adjust this index based on the actual list of devices


            // Ensure the specified index is valid
            if (loopbackDeviceIndex < 0 || loopbackDeviceIndex >= devices.Count)
            {
                Console.WriteLine("Invalid device index specified");
                return;
            }

            //puts device number 7 loopback in current device
            var device = devices[loopbackDeviceIndex];
            
            //prints devixe description
            Console.WriteLine("Using device: {0}", device.Description);

            // Open the device for capture
            device.OnPacketArrival += new PacketArrivalEventHandler(dev_OnPacketArrival);

            try
            {
                
                device.Open(DeviceModes.MaxResponsiveness, 1000);//currently working with running the video on the background
                                                                 //still need to figure out how this works 
                device.Filter = "udp port 2000";
                Console.WriteLine("-- Listening on {0}, hit 'Enter' to stop...", device.Description);

                // Start the capturing process
                device.StartCapture();

                // Wait for 'Enter' from the user
                Console.ReadLine();

                // Stop the capturing process
                device.StopCapture();

                Console.WriteLine("-- Capture stopped.");

                // Print out the device statistics
                Console.WriteLine(device.Statistics.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }


        

        public static void StoreData(Packet packet, byte[][] DataPayload)
        {
            
            //global
            int packetLength = 188;

            //static 
            int packetIndex = 0;

            
            //copying the 188-byte packets into the packetArray
            //going through the entire packet bytes
            
            //PROBLEM HERE IF 47 FOUND BUT HAS LESS THAN 188 BYTES IN PAYLOAD 
            //WILL PRINT OUT AN ERROR 

            for (int i = 0; i < packet.Bytes.Length-1  ; i++)
            {
                if (packet.Bytes[i] == 0x47 && i < (packet.Bytes.Length - 1)-187)
                {
                    Console.WriteLine("packet.Bytes[i] == 0x47 in spot i: {0} ", i);

                    // Found 0x47, copy the following 188 bytes
                    byte[] singlePacket = new byte[packetLength];//188
                    Array.Copy(packet.Bytes, i, singlePacket, 0, packetLength);//from 0-183, size:188

                    // Store the 188-byte array in the packet array
                    DataPayload[packetIndex] = singlePacket;

                    //counter increase
                    packetIndex++;

                    // Move the index to the next packet start (i.e., skip the current 188-byte packet)
                    i += packetLength-1; // Because loop increments i, we do i += 187 to move to the next packet start
                }
            }

            for (int i = 0; i < DataPayload.Length - 1; i++)//print number of packet its length and the value inside 
            {
                // Process the 188-byte packets
                if (DataPayload[i] != null)
                {
                    Console.WriteLine("Packet {0}: with the length of {1} :{2}", i, DataPayload[i].Length, BitConverter.ToString(DataPayload[i]));

                }
            }
        }

        public static PAT ParsePAT(BinaryReader binaryReader, TransportPackets packet)
        {
            PAT pat = new PAT(packet);
            int currentByte;

            // Extract 8 bit Table ID
            currentByte = binaryReader.ReadByte();
            pat.TableID  = currentByte;
            Console.WriteLine($"Pat tableID: {pat.TableID}");

            // Extract 1 bit SectionSyntaxIndicator
            currentByte = binaryReader.ReadByte();
            pat.SectionSyntaxIndicator  = (currentByte & 0x80) !=0;
            Console.WriteLine($"Pat SectionSyntaxIndicator: {pat.SectionSyntaxIndicator}");

            // Extract 12 bit SectionLength
            pat.SectionLength = ((currentByte & 0x0F) << 8) | binaryReader.ReadByte();
            Console.WriteLine($"Pat SectionLength: {pat.SectionLength}");

            // Extract the 16 bit transport stream ID
            pat.TransportStreamID  = (binaryReader.ReadByte() << 8) | binaryReader.ReadByte();
            Console.WriteLine($"Pat transportStreamID: {pat.TransportStreamID}");

            // Extract 5 bit VersionNumber
            currentByte = binaryReader.ReadByte();
            pat.VersionNumber = ((currentByte & 0b00111110) >> 1);
            Console.WriteLine($"Pat VersionNumber: {pat.VersionNumber}");

            //Extract 1 bit CurrentNextIndicator
            pat.CurrentNextIndicator = (currentByte & 0x01)!=0;
            Console.WriteLine($"Pat CurrentNextIndicator: {pat.CurrentNextIndicator}");

            //Extract 8 bit SectionNumber
            currentByte = binaryReader.ReadByte();
            pat.SectionNumber = currentByte;
            Console.WriteLine($"Pat SectionNumber: {pat.SectionNumber}");

            //Extract 8 bit LastSectionNumber
            currentByte = binaryReader.ReadByte();
            pat.LastSectionNumber = currentByte;
            Console.WriteLine($"Pat LastSectionNumber: {pat.LastSectionNumber}");
            int counter = 0;

            // Now, loop through the programs
            while (((pat.SectionLength -9) / 4)>counter) // +2 to add the section length size -4 to ignore CRC_32
            {
                // Program number
                int programNumber = BitConverter.ToInt16(binaryReader.ReadBytes(2), 0);
                byte[] bytes = BigEndianReadBytes(binaryReader, 2);
                if(programNumber == 0)
                {
                    pat.NetworkPID = (BitConverter.ToInt16(bytes, 0) & 0b1111111111111); //13 bit PAT PID
                }
                else
                {
                    pat.PMTs.Add((BitConverter.ToInt16(bytes, 0) & 0b1111111111111), new PMT());//13 bit PMT PID
                }
                counter++;
                // PMT PID
            }

            //extract crc32 value and replace it with the temperery one in the pat 
            
            
            pat.CRC32 = BitConverter.ToInt16(BigEndianReadBytes(binaryReader, 4), 0);


            Console.WriteLine($"pat.CRC32: {pat.CRC32}");
            return pat;
            // Add the PAT to the dictionary
            //if (!patDictionary.ContainsKey(pat.TransportStreamID))
            //{
            //    patDictionary.Add(pat.TransportStreamID, pat);
            //    patFound = true;
            //}
            //else
            //{
            //    Console.WriteLine($"PAT with Transport Stream ID {pat.TransportStreamID} already exists.");
            //}

        }

        public static PMT DisplayPMT(BinaryReader binaryReader, TransportPackets packet)
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
            pmt.ProgramNumber = BitConverter.ToInt16(BigEndianReadBytes(binaryReader, 2), 0);
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
            pmt.ProgramInfoDescriptors = BigEndianReadBytes(binaryReader, pmt.ProgramInfoLength);
            
            // Now, loop through the elementary streams
            while (binaryReader.BaseStream.Position -2 < pmt.SectionLength) // Account 2 byte it takes to calc programInfoLength and for the 4 byte CRC_32 at the end
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
                pmtinfo.Descriptors = BigEndianReadBytes(binaryReader, pmtinfo.ESinfoLength);

                // Add the PMT info to the PMT object
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
            for(int i=0; i<pmt.CRC32.Length-1;i++ )
            {
                pmt.CRC32[i] = binaryReader.ReadByte();
            }
            
            Console.WriteLine($"PMT CRC_32: {BitConverter.ToString(pmt.CRC32).Replace("-", " ")}");

            return pmt;
        }



        public static void HandleHeaderPayload(byte[][] DataPayload)
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
                        byte currentByte =reader.ReadByte();
                        TransportPacket.TransportErrorIndicator = (currentByte>>7 >0);
                        TransportPacket.PayloadUnitStratIndicator = (currentByte>>6 >0);
                        TransportPacket.TransportPriority = (currentByte>>5 >0);
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
                                        TransportPacket.PieceWiseRate = ((currentByte & 0x5F) << 8) | reader.ReadByte() << 8 |reader.ReadByte();
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
                        if(TransportPacket.PayloadUnitStratIndicator)
                            reader.BaseStream.Seek(1, SeekOrigin.Current);
                        if(pat == null)
                        {
                            if(TransportPacket.PID == 0)
                            {
                                Console.WriteLine("PID is 0 -> arrived to PAT");
                                string stringPidOfBitArray1 = Convert.ToString(TransportPacket.PID, 2).PadLeft(13, '0');
                                Console.WriteLine("PAT bit array representation: " + stringPidOfBitArray1);
                                pat = ParsePAT(reader, TransportPacket);
                            }
                        }

                        else if(pat.PMTs.ContainsKey(TransportPacket.PID))//needs fixing not looking for transportpacket.id
                        {
                            Console.WriteLine("Error: PMT arrived before PAT");
                            string stringPidOfBitArray2 = Convert.ToString(TransportPacket.PID, 2).PadLeft(13, '0');
                            Console.WriteLine("PMT bit array representation: " + stringPidOfBitArray2);
                            pmt = DisplayPMT(reader, TransportPacket);
                        }
                        
                        // Display the extracted PID as a binary string
                        string stringPidOfBitArray = Convert.ToString(TransportPacket.PID, 2).PadLeft(13, '0');
                        Console.WriteLine("NOT VALID PMT/PAT FOR EXTRACTION bit array representation: " + stringPidOfBitArray);

                        
                    }
                }
            }
            Console.WriteLine("for loop ended");
            return ;
        }



        static void dev_OnPacketArrival(object sender, PacketCapture e)
        {
            //global sizes for packet payload and header
            int packetLength = 188;
            int amountOfHeaderBytes = 4;

            //incrementing and printing packetcount each time 
            Packetcount++;
            Console.WriteLine("packets : {0}", Packetcount);

            //getting packet and printing its information
            var rawPacket = e.GetPacket();
            
            Console.WriteLine("e.GetPacket : {0}", rawPacket);

            //printing raw packet length
            var rawPacketLength = rawPacket.Data.Length;
            rawPacket.PacketLength.ToString();

           
            //parsing the rawpacket data into Packet packet
            var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            //printing rawPacket information
            Console.WriteLine("rawPacketLength : {0}", rawPacketLength);
            Console.WriteLine("rawPacket.LinkLayerType : {0}", rawPacket.LinkLayerType);
            Console.WriteLine("rawPacket.Data : {0}", rawPacket.Data);


            // Check if packet contains an IP packet
            if (packet.PayloadPacket is IPPacket ipPacket)
            {
                //check if packet contains the correct ip
                if (ipPacket.DestinationAddress.ToString() == targetIpAddress)
                {
                    // Check if the IP packet contains a UDP packet and matches the target IP
                    try
                    {
                        var udppacket = new UdpPacket(((UdpPacket)(packet.PayloadPacket.PayloadPacket)).SourcePort,
                        ((UdpPacket)(packet.PayloadPacket.PayloadPacket)).DestinationPort);

                        //check if udp DestinationPort is correct / equal to 2000 port
                        if (udppacket.DestinationPort == targetPort)
                        {
                            Console.WriteLine("UDP packet detected from {0}:{1}", targetIpAddress, targetPort);

                            // Initialize arrays to hold 188 byte data pyloads and the 4 byte headers
                            byte[][] DataPayload = new byte[packet.Bytes.Length / packetLength][];

                            //call the storeData function, goes throgh the bytes in the packet and stores 184 byte chunks that start with 0x47
                            //and also stores the 4 byte headers in headers [][] byte array 
                            StoreData(packet, DataPayload);

                            //returns an int that describes what pid was in the payload : pat,cat,pmt,tsdt,and errors
                            HandleHeaderPayload(DataPayload);  

                        }

                        else
                        {
                            //if the ip destination and target port are diffrent still print where the packet has come from 
                            Console.WriteLine("target port was not correct details:  {0}:{1}", ipPacket.DestinationAddress.ToString(), udppacket.DestinationPort);
                        }

                    }
                    catch (Exception g)
                    {
                        Console.WriteLine("error captured  : {0} ", g.Message);
                        return;
                    }
                    
                    
                }
                else
                {
                    //if the ip destination are diffrent still print where the packet has come from 
                    Console.WriteLine("ip was not correct  details:  {0}", ipPacket.DestinationAddress.ToString());
                }
            }
        }

        public static byte[] BigEndianReadBytes(BinaryReader reader, int size)
        {
            byte[] bytes = reader.ReadBytes(size);
            if (Endian) Array.Reverse(bytes);
            return bytes;
        }

        
    }
}
