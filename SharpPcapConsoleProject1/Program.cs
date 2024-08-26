using System;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using PacketDotNet;
using PacketDotNet.Ieee80211;
using SharpPcap;
using SharpPcap.LibPcap;
using SharpPcapConsoleProject1;

namespace NpcapRemoteCapture
{
    public class Program
    {
        
        //global information 
        private static readonly int targetPort = 2000;
        private static readonly string targetIpAddress = "224.0.0.0";
        static int ByteSize = 8;

        //static 
        static int Packetcount = 0;
        private static bool patFound = false;
        private static Dictionary<int, PAT> patDictionary = new Dictionary<int, PAT>(); // Dictionary to hold all PATs
        TransportPackets packet = new TransportPackets();
        PAT pat = null;
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


        

        public static void StoreData(Packet packet, byte[][] HeaderPayload, byte[][] DataPayload)
        {
            
            //global
            int packetLength = 188;
            int amountOfHeaderBytes = 4;

            //static 
            int packetIndex = 0;

            
            //copying the 184-byte packets into the packetArray
            //going through the entire packet bytes
            
            //PROBLEM HERE IF 47 FOUND BUT HAS LESS THAN 188 BYTES IN PAYLOAD 
            //WILL PRINT OUT AN ERROR 

            for (int i = 0; i < packet.Bytes.Length-1  ; i++)
            {
                if (packet.Bytes[i] == 0x47 && i < (packet.Bytes.Length - 1)-187)
                {
                    Console.WriteLine("packet.Bytes[i] == 0x47 in spot i: {0} ", i);

                    // Found 0x47, now skip the next 3 bytes and copy the following 184 bytes
                    byte[] singlePacket = new byte[packetLength];//188
                    Array.Copy(packet.Bytes, i + amountOfHeaderBytes, singlePacket, 0, packetLength - amountOfHeaderBytes);//from 0-183, size:188-4

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
                // Process the 184-byte packets
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
            int tableID = currentByte;
            Console.WriteLine($"Pat tableID: {tableID}");

            // Extract 1 bit SectionSyntaxIndicator
            currentByte = binaryReader.ReadByte();
            bool SectionSyntaxIndicator = (currentByte & 0x80) !=0;
            Console.WriteLine($"Pat SectionSyntaxIndicator: {SectionSyntaxIndicator}");

            // Extract 12 bit SectionLength
            int SectionLength = ((currentByte & 0x1F) << 8) | binaryReader.ReadByte();
            Console.WriteLine($"Pat SectionLength: {SectionLength}");

            // Extract the 16 bit transport stream ID
            int transportStreamID = (binaryReader.ReadByte() << 8) | binaryReader.ReadByte();
            Console.WriteLine($"Pat transportStreamID: {transportStreamID}");

            // Extract 5 bit VersionNumber
            currentByte = binaryReader.ReadByte();
            int VersionNumber = ((currentByte & 0x37) >> 1);
            Console.WriteLine($"Pat VersionNumber: {VersionNumber}");

            //Extract 1 bit CurrentNextIndicator
            bool CurrentNextIndicator = (currentByte & 0x01)!=0;
            Console.WriteLine($"Pat CurrentNextIndicator: {CurrentNextIndicator}");

            //Extract 8 bit SectionNumber
            currentByte = binaryReader.ReadByte();
            int SectionNumber = currentByte;
            Console.WriteLine($"Pat SectionNumber: {SectionNumber}");

            //Extract 8 bit LastSectionNumber
            currentByte = binaryReader.ReadByte();
            int LastSectionNumber = currentByte;
            Console.WriteLine($"Pat LastSectionNumber: {LastSectionNumber}");

            // Create a new PAT object with temp crc32
            byte[] crc32 = new byte[4];
            PAT pat = new PAT(tableID, SectionSyntaxIndicator ? 0 : 1, SectionLength,
                transportStreamID, VersionNumber, CurrentNextIndicator ? 0 : 1, SectionNumber, LastSectionNumber, crc32);
            

            // Now, loop through the programs
            while (binaryReader.BaseStream.Position < binaryReader.Length - 4) // -4 to ignore CRC_32
            {
                // Program number
                int programNumber = (binaryReader.ReadByte() << 8) | binaryReader.ReadByte();

                // PMT PID
                int pmtPID = (binaryReader.ReadByte() & 0x1F) << 8 | binaryReader.ReadByte();

                Console.WriteLine($"Program Number: {programNumber}, PMT PID: 0x{pmtPID:X4}");

                // Add the program and PMT PID to the PAT object
                pat.AddPMT(programNumber, null); // Initially, PMT is null, it will be updated later
            }
            
            //extract crc32 value and replace it with the temperery one in the pat 
            crc32 = binaryReader.ReadBytes(4);
            for (int i = 0; i < crc32.Length-1; i++)
            {
                pat.CRC32[i] = crc32[i];
            }
            Console.WriteLine($"CRC_32: {BitConverter.ToString(crc32).Replace("-", " ")}");

            // Add the PAT to the dictionary
            if (!patDictionary.ContainsKey(transportStreamID))
            {
                patDictionary.Add(transportStreamID, pat);
                patFound = true;
            }
            else
            {
                Console.WriteLine($"PAT with Transport Stream ID {transportStreamID} already exists.");
            }
            return pat;
        }

        public static PMT DisplayPMT(BinaryReader binaryReader, TransportPackets packet)
        {
            PMT pmt = new PMT(packet);
            byte[] programInfo= { 0 };

            // Extract the Table ID (8 bits)
            pmt.TableID = binaryReader.ReadByte();
            Console.WriteLine($"Table ID: 0x{pmt.TableID:X2} ({pmt.TableID})");

            // Section Syntax Indicator (1 bit) + 3 bits reserved + Section Length (12 bits)
            int currentByte = binaryReader.ReadByte();
            pmt.SectionSyntaxIndicator = (currentByte & 0x80) > 0;
            pmt.SectionLength = ((currentByte & 0x0F) << 8) | binaryReader.ReadByte();
            Console.WriteLine($"PMT Section Syntax Indicator: {pmt.SectionSyntaxIndicator}");
            Console.WriteLine($"PMT Section Length: {pmt.SectionLength}");

            // Program Number (16 bits)
            pmt.ProgramNumber = (binaryReader.ReadByte() << 8) | binaryReader.ReadByte();
            Console.WriteLine($"PMT Program Number: {pmt.ProgramNumber}");

            // Reserved (2 bits) + Version Number (5 bits) + Current/Next Indicator (1 bit)
            currentByte = binaryReader.ReadByte();
            pmt.VersionNumber = (currentByte & 0x3E) >> 1;
            pmt.CurrentNextIndicator = (currentByte & 0x01) != 0;
            Console.WriteLine($"PMT Version Number: {pmt.VersionNumber}");
            Console.WriteLine($"PMT Current/Next Indicator: {pmt.CurrentNextIndicator}");

            //-----------------------------------------------------------------------------------got to here
            // Section Number (8 bits)
            int sectionNumber = binaryReader.ReadByte();
            Console.WriteLine($"PMT Section Number: {sectionNumber}");

            // Last Section Number (8 bits)
            int lastSectionNumber = binaryReader.ReadByte();
            Console.WriteLine($"PMT Last Section Number: {lastSectionNumber}");

            // Reserved (3 bits) + PCR PID (13 bits)
            currentByte = binaryReader.ReadByte();
            int pcrPID = ((currentByte & 0x1F) << 8) | binaryReader.ReadByte();
            Console.WriteLine($"PMT PCR PID: 0x{pcrPID:X4} ({pcrPID})");

            // Reserved (4 bits) + Program Info Length (12 bits)
            currentByte = binaryReader.ReadByte();
            int programInfoLength = ((currentByte & 0x0F) << 8) | binaryReader.ReadByte();
            Console.WriteLine($"PMT Program Info Length: {programInfoLength}");

            // Program Info Descriptors (variable length based on Program Info Length)
            
            if (programInfoLength > 0)
            {
                programInfo = binaryReader.ReadBytes(programInfoLength);
                Console.WriteLine("PMT Program Info Descriptors:");
                Console.WriteLine(BitConverter.ToString(programInfo).Replace("-", " "));
            }

            MemoryStream msprogramInfo = new MemoryStream(programInfo);
            BinaryReader programInfobinaryReader = new BinaryReader(msprogramInfo);

            Console.WriteLine($"PMT msprogramInfo programInfo Position: {msprogramInfo.Position}");
            Console.WriteLine($"PMT sectionLength: {sectionLength}");
            // Now, loop through the elementary streams
            while (msprogramInfo.Position < programInfoLength + 2 - 4) // Account 2 byte it takes to calc programInfoLength and for the 4 byte CRC_32 at the end
            {
                // Stream Type (8 bits)
                byte streamType = binaryReader.ReadByte();
                Console.WriteLine($"PMT Stream Type: 0x{streamType:X2} ({streamType})");

                // Reserved (3 bits) + Elementary PID (13 bits)
                int elementaryPID = ((binaryReader.ReadByte() & 0x1F) << 8) | binaryReader.ReadByte();
                Console.WriteLine($"PMT Elementary PID: 0x{elementaryPID:X4} ({elementaryPID})");

                // Identifying the stream type
                switch (streamType)
                {
                    case 0x1B: // H.264 video
                        Console.WriteLine($"Found H.264 video stream with PID {elementaryPID:X}");
                        break;
                    case 0x0F: // AAC audio
                        Console.WriteLine($"Found AAC audio stream with PID {elementaryPID:X}");
                        break;
                    case 0x06: // Subtitles or other
                        Console.WriteLine($"Found Subtitles or other stream with PID {elementaryPID:X}");
                        break;
                    default:
                        Console.WriteLine($"Unknown stream type {streamType:X} with PID {elementaryPID:X}");
                        break;
                }

                // Reserved (4 bits) + ES Info Length (12 bits)
                int esInfoLength = ((binaryReader.ReadByte() & 0x0F) << 8) | binaryReader.ReadByte();
                Console.WriteLine($"PMT ES Info Length: {esInfoLength}");

                // ES Info Descriptors (variable length based on ES Info Length)
                if (esInfoLength > 0)
                {
                    byte[] esInfo = binaryReader.ReadBytes(esInfoLength);
                    Console.WriteLine("PMT ES Info Descriptors:");
                    Console.WriteLine(BitConverter.ToString(esInfo).Replace("-", " "));
                }
            }

            // CRC_32 (32 bits)
            byte[] crc32 = binaryReader.ReadBytes(4);
            Console.WriteLine($"PMT CRC_32: {BitConverter.ToString(crc32).Replace("-", " ")}");

            // Find the corresponding PAT for this PMT
            foreach (var pat in patDictionary.Values)
            {
                if (pat.PMTs.ContainsKey(programNumber))
                {
                    Console.WriteLine($"PMT found for Program Number: {programNumber}");

                    // Create a PMT object and associate it with the PAT
                    PMT pmt = new PMT(tableID, sectionSyntaxIndicator ? 1 : 0, sectionLength, programNumber,
                        versionNumber, currentNextIndicator, sectionNumber, lastSectionNumber, pcrPID, programInfoLength, 0); // Add crc32 if needed

                    // Add the PMT to the PAT's dictionary
                    pat.PMTs[programNumber] = pmt;
                    break;
                }
            }
            return pmt;
        }



        public static int HandleHeaderPayload(byte[][] DataPayload)
        {
            PAT pat = null;
            for (int i = 0; i < DataPayload.Length - 1; i++)
            {

                //check if the row is null
                if (DataPayload[i] == null)
                {
                    Console.WriteLine("error : DataPayload[i] == null, row was null");
                    return -2;
                }
                

                //give the first data payload to firstDataPayload
                byte[] firstPayload = DataPayload[i];
                
                using (MemoryStream ms = new MemoryStream(firstPayload))
                {
                    using (BinaryReader reader = new BinaryReader(ms))
                    {
                        TransportPackets packet = new TransportPackets();
                        packet.SyncByte = reader.ReadByte();
                        byte currentByte =reader.ReadByte();
                        packet.TransportErrorIndicator = (currentByte>>7 >0);
                        packet.PayloadUnitStratIndicator = (currentByte>>6 >0);
                        packet.TransportPriority = (currentByte>>5 >0);
                        packet.PID = (currentByte & 0x1f);
                        currentByte = reader.ReadByte();
                        packet.PID = packet.PID << 8;
                        packet.PID = packet.PID | currentByte;
                        currentByte = reader.ReadByte();
                        Console.WriteLine($"{packet.PID}");
                        packet.TransportScramblingControl = (short)((currentByte & 0xc0) >> 6);
                        packet.AdaptationFieldControl = (short)((currentByte & 0x30) >> 4);
                        packet.ContinuityCounter = (short)((currentByte & 0x0F));
                        if (packet.AdaptationFieldControl > 2)
                        {
                            packet.AdaptationFieldPresent = true;
                            packet.AdaptationFieldLength = reader.ReadByte();
                            if (packet.AdaptationFieldLength > 0)
                            {
                                currentByte = reader.ReadByte();
                                packet.DiscontinuityIndicator = (currentByte & 0x80) > 0;
                                packet.RandomAccessIndicator = (currentByte & 0x40) > 0;
                                packet.ElementaryStreamPriorityIndicator = (currentByte & 0x20) > 0;
                                packet.PCRFlag = (currentByte & 0x10) > 0;
                                packet.OPCRFlag = (currentByte & 0x01) > 0;
                                packet.SplicingPointFlag = (currentByte & 0x04) > 0;
                                packet.TransportPrivateDataFlag = (currentByte & 0x02) > 0;
                                packet.AdaptationFieldExtentionFlag = (currentByte & 0X01) > 0;

                                if (packet.PCRFlag.Value)
                                {
                                    packet.PCR = BitConverter.ToInt64(reader.ReadBytes(6), 0);
                                }

                                if (packet.OPCRFlag.Value)
                                {
                                    packet.OPCR = BitConverter.ToInt64(reader.ReadBytes(6), 0);
                                }

                                if (packet.SplicingPointFlag.Value)
                                {
                                    packet.SpliceCountdown = reader.ReadByte();
                                }

                                if (packet.TransportPrivateDataFlag.Value)
                                {
                                    packet.TransportPrivateDataLength = reader.ReadByte();
                                    packet.TransportPrivateData = reader.ReadBytes(packet.TransportPrivateDataLength.Value);
                                }

                                if (packet.AdaptationFieldExtentionFlag.Value)
                                {
                                    packet.AdaptationFieldExtsionlength = reader.ReadByte();
                                    currentByte = reader.ReadByte();
                                    packet.ItwFlag = (currentByte & 0x80) > 0;
                                    packet.PiecewiseRateFlag = (currentByte & 0x40) > 0;
                                    packet.SeamlessSPliceFlag = (currentByte & 0x20) > 0;
                                    if (packet.ItwFlag.Value)
                                    {
                                        currentByte = reader.ReadByte();
                                        if ((packet.ItwValidFlag = (currentByte & 0x80) > 0).Value)
                                        {
                                            packet.ItwOffSet = ((currentByte & 0x7F) << 8) | reader.ReadByte();
                                        }

                                    }
                                    if (packet.PiecewiseRateFlag.Value)
                                    {
                                        currentByte = reader.ReadByte();
                                        packet.PieceWiseRate = ((currentByte & 0x5F) << 8) | reader.ReadByte();
                                    }
                                    if (packet.SeamlessSPliceFlag.Value)
                                    {
                                        currentByte = reader.ReadByte();
                                        packet.SpliceType = (short)currentByte >> 4;
                                        packet.DTSNextAu = currentByte & 0x0e << 32;
                                        packet.DTSNextAu = reader.ReadByte() & 0xfe << 29;
                                        packet.DTSNextAu = reader.ReadByte() & 0XFE << 14;
                                    }
                                }
                            }
                            if ((packet.AdaptationFieldControl & 1) == 1)
                            {
                                reader.BaseStream.Seek(packet.AdaptationFieldLength.Value + 4, SeekOrigin.Current);
                            }


                        }
                        if(packet.PayloadUnitStratIndicator)
                            reader.BaseStream.Seek(1, SeekOrigin.Current);
                        if(pat == null)
                        {
                            if(packet.PID == 0)
                            {
                                pat = ParsePAT(reader, packet);
                            }
                        }
                        else
                        if(pat.PMTs.ContainsKey(packet.PID))
                        {
                            PMT pmt = DisplayPMT(reader, packet);
                        }
                        // Create a 13-bit mask: 0001 1111 1111 1111
                        int mask = 0x1FFF;

                        // Combine the relevant parts of the second and third bytes into a single 16-bit integer
                        int combinedBytes = (firstHeaderPayload[1] << 8) | firstHeaderPayload[2];//shift left 8 time xxxx xxxx 0000 0000
                                                                                                 //into or operation xxxx xxxx yyyy yyyy

                        // Apply the mask to extract the PID
                        int varPidOfBitArray = combinedBytes & mask; //only take the bits 11 to 24 
                        
                        // Display the extracted PID as a binary string
                        string stringPidOfBitArray = Convert.ToString(varPidOfBitArray, 2).PadLeft(13, '0');
                        Console.WriteLine("PID bit array representation: " + stringPidOfBitArray);

                        // Check PID value and return corresponding code
                        if (varPidOfBitArray >= 0)
                        {
                            switch (varPidOfBitArray)
                            {

                                case 0x0000:
                                    Console.WriteLine("PID is 0 -> arrived to PAT");
                                    patFound = true;
                                    // Here, parse the PAT
                                    ParsePAT(firstDataPayload);
                                    return 0;
                                case 0x0001:
                                    Console.WriteLine("PID is 0x0001 -> arrived to CAT");
                                    return 1;
                                case 0x0002:
                                    Console.WriteLine("PID is 0x0002 -> arrived to TSDT");
                                    return 2;
                                case 0x0010:
                                    Console.WriteLine("PID is 0x0010 -> arrived to NIT");
                                    return 4;
                                case int n when (n >= 0x0030 && n <= 0x1FFF):
                                    if (patFound == false)
                                    {
                                        Console.WriteLine("Error: PMT arrived before PAT");
                                        return -4;
                                    }
                                    Console.WriteLine("PID is >= 0x30 && <= 0x1FFF -> arrived to PMT");
                                    // Extract PMT payload from the packet (Assume PMT is the remaining payload after the header)
                                    byte[] pmtData = new byte[firstDataPayload.Length]; // Subtract header length

                                    //public static void Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length);
                                    Array.Copy(firstDataPayload, 0, pmtData, 0, pmtData.Length);

                                    // Parse PMT
                                    DisplayPMT(pmtData);
                                    return 3;
                                default:
                                    Console.WriteLine("Unrecognized PID");
                                    return -1;
                                    // Extract PMT payload
                            }

                        }
                        else
                        {
                            Console.WriteLine("error : varPidOfBitArray < 0");
                            return -3;
                        }
                    }



                }

                
               

                
            }
            Console.WriteLine("for loop ended");
            return -3;

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

                            // Initialize arrays to hold 184 byte data pyloads and the 4 byte headers
                            byte[][] HeaderPayload = new byte[packet.Bytes.Length / packetLength][];
                            byte[][] DataPayload = new byte[packet.Bytes.Length / packetLength][];

                            //call the storeData function, goes throgh the bytes in the packet and stores 184 byte chunks that start with 0x47
                            //and also stores the 4 byte headers in headers [][] byte array 
                            StoreData(packet, HeaderPayload, DataPayload);

                            //returns an int that describes what pid was in the payload : pat,cat,pmt,tsdt,and errors
                            switch (HandleHeaderPayload(HeaderPayload, DataPayload))
                            {
                                case 0:
                                    Console.WriteLine("----------PAT-------");
                                    break;
                                case 1:
                                    Console.WriteLine("----------CAT-------");
                                    break;
                                case 2:
                                    Console.WriteLine("----------TSDT-------");
                                    break;
                                case 3:
                                    Console.WriteLine("----------PMT-------");
                                    break;
                                case -4:
                                    Console.WriteLine("----------Error: PMT arrived before PAT-------");
                                    break;
                                default:
                                    Console.WriteLine("------------------default--------------");
                                    break;
                            }

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

        
    }
}
