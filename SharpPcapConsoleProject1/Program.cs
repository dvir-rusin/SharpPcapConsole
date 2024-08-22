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
            //can jump over if you dont find 0x47 in the last 180
            //cuz needs to be 188 bytes
            for (int i = 0; i < packet.Bytes.Length-1  ; i++)//if you havent found 0x47 in the spot 1315 188 b4 the end just jump to the end for (int i = 0; i < packet.Bytes.Length - (packetLength) -1; i++)
            {
                if (packet.Bytes[i] == 0x47)
                {

                    // Found 0x47, now skip the next 3 bytes and copy the following 184 bytes
                    byte[] singlePacket = new byte[packetLength-amountOfHeaderBytes];//188-4
                    byte[] singleHeaderPacket = new byte[amountOfHeaderBytes];//4
                    Array.Copy(packet.Bytes, i, singleHeaderPacket, 0, amountOfHeaderBytes);//from 0-3 size:4
                    Array.Copy(packet.Bytes, i + amountOfHeaderBytes, singlePacket, 0, packetLength - amountOfHeaderBytes);//from 0-183, size:188-4

                    // Store the 184-byte array in the packet array
                    HeaderPayload[packetIndex] = singleHeaderPacket;
                    DataPayload[packetIndex] = singlePacket;
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

        public static void ParsePAT(byte[] patData)
        {
            int currentByte;
            MemoryStream ms = new MemoryStream(patData);
            BinaryReader binaryReader = new BinaryReader(ms);

            // Extract 8 bit Table ID
            currentByte = binaryReader.ReadByte();
            int tableID = currentByte;

            // Extract 1 bit SectionSyntaxIndicator
            currentByte = binaryReader.ReadByte();
            bool SectionSyntaxIndicator = (currentByte & 0x80) !=0;

            // Extract 12 bit SectionLength
            int SectionLength = ((currentByte & 0x1F) << 8) | binaryReader.ReadByte();

            // Extract the 16 bit transport stream ID
            int transportStreamID = (binaryReader.ReadByte() << 8) | binaryReader.ReadByte();

            // Extract 5 bit VersionNumber
            currentByte = binaryReader.ReadByte();
            int VersionNumber = ((currentByte & 0x37) >> 1);

            //Extract 1 bit CurrentNextIndicator
            bool CurrentNextIndicator = (currentByte & 0x01)!=0;

            //Extract 8 bit SectionNumber
            currentByte = binaryReader.ReadByte();
            int SectionNumber = currentByte;

            //Extract 8 bit LastSectionNumber
            currentByte = binaryReader.ReadByte();
            int LastSectionNumber = currentByte;

            // Create a new PAT object
            PAT pat = new PAT(tableID, SectionSyntaxIndicator? 0:1, SectionLength,
                transportStreamID, VersionNumber, CurrentNextIndicator?0:1, SectionNumber, LastSectionNumber,0);
            //remember to replace null with crc 

            // Now, loop through the programs
            while (ms.Position < ms.Length - 4) // -4 to ignore CRC_32
            {
                // Program number
                int programNumber = (binaryReader.ReadByte() << 8) | binaryReader.ReadByte();

                // PMT PID
                int pmtPID = (binaryReader.ReadByte() & 0x1F) << 8 | binaryReader.ReadByte();

                Console.WriteLine($"Program Number: {programNumber}, PMT PID: 0x{pmtPID:X4}");

                // Add the program and PMT PID to the PAT object
                pat.AddPMT(programNumber, null); // Initially, PMT is null, it will be updated later
            }

            // Optionally, you can read and check the CRC_32 here
            byte[] crc32 = binaryReader.ReadBytes(4);
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
        }

        public static void DisplayPMT(byte[] pmtData)
        {
            MemoryStream ms = new MemoryStream(pmtData);
            BinaryReader binaryReader = new BinaryReader(ms);

            // Extract the Table ID (8 bits)
            int tableID = binaryReader.ReadByte();
            Console.WriteLine($"Table ID: 0x{tableID:X2} ({tableID})");

            // Section Syntax Indicator (1 bit) + 3 bits reserved + Section Length (12 bits)
            int currentByte = binaryReader.ReadByte();
            bool sectionSyntaxIndicator = (currentByte & 0x80) != 0;
            int sectionLength = ((currentByte & 0x0F) << 8) | binaryReader.ReadByte();
            Console.WriteLine($"Section Syntax Indicator: {sectionSyntaxIndicator}");
            Console.WriteLine($"Section Length: {sectionLength}");

            // Program Number (16 bits)
            int programNumber = (binaryReader.ReadByte() << 8) | binaryReader.ReadByte();
            Console.WriteLine($"Program Number: {programNumber}");

            // Reserved (2 bits) + Version Number (5 bits) + Current/Next Indicator (1 bit)
            currentByte = binaryReader.ReadByte();
            int versionNumber = (currentByte & 0x3E) >> 1;
            bool currentNextIndicator = (currentByte & 0x01) != 0;
            Console.WriteLine($"Version Number: {versionNumber}");
            Console.WriteLine($"Current/Next Indicator: {currentNextIndicator}");

            // Section Number (8 bits)
            int sectionNumber = binaryReader.ReadByte();
            Console.WriteLine($"Section Number: {sectionNumber}");

            // Last Section Number (8 bits)
            int lastSectionNumber = binaryReader.ReadByte();
            Console.WriteLine($"Last Section Number: {lastSectionNumber}");

            // Reserved (3 bits) + PCR PID (13 bits)
            currentByte = binaryReader.ReadByte();
            int pcrPID = ((currentByte & 0x1F) << 8) | binaryReader.ReadByte();
            Console.WriteLine($"PCR PID: 0x{pcrPID:X4} ({pcrPID})");

            // Reserved (4 bits) + Program Info Length (12 bits)
            currentByte = binaryReader.ReadByte();
            int programInfoLength = ((currentByte & 0x0F) << 8) | binaryReader.ReadByte();
            Console.WriteLine($"Program Info Length: {programInfoLength}");

            // Program Info Descriptors (variable length based on Program Info Length)
            if (programInfoLength > 0)
            {
                byte[] programInfo = binaryReader.ReadBytes(programInfoLength);
                Console.WriteLine("Program Info Descriptors:");
                Console.WriteLine(BitConverter.ToString(programInfo).Replace("-", " "));
            }

            // Now, loop through the elementary streams
            while (ms.Position < sectionLength + 3 - 4) // Account for the 4 byte CRC_32 at the end
            {
                // Stream Type (8 bits)
                byte streamType = binaryReader.ReadByte();
                Console.WriteLine($"Stream Type: 0x{streamType:X2} ({streamType})");

                // Reserved (3 bits) + Elementary PID (13 bits)
                int elementaryPID = ((binaryReader.ReadByte() & 0x1F) << 8) | binaryReader.ReadByte();
                Console.WriteLine($"Elementary PID: 0x{elementaryPID:X4} ({elementaryPID})");

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
                Console.WriteLine($"ES Info Length: {esInfoLength}");

                // ES Info Descriptors (variable length based on ES Info Length)
                if (esInfoLength > 0)
                {
                    byte[] esInfo = binaryReader.ReadBytes(esInfoLength);
                    Console.WriteLine("ES Info Descriptors:");
                    Console.WriteLine(BitConverter.ToString(esInfo).Replace("-", " "));
                }
            }

            // CRC_32 (32 bits)
            byte[] crc32 = binaryReader.ReadBytes(4);
            Console.WriteLine($"CRC_32: {BitConverter.ToString(crc32).Replace("-", " ")}");

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
        }



        public static int HandleHeaderPayload(byte[][] HeaderPayload, byte[][] DataPayload)
        {

            for (int i = 0; i < HeaderPayload.Length - 1; i++)
            {
                //check if the row is null
                if (HeaderPayload[i] == null)
                {
                    Console.WriteLine("error : HeaderPayload[i] == null, row was null");
                    return -2;
                }

                //check if the row is null
                if (DataPayload[i] == null)
                {
                    Console.WriteLine("error : DataPayload[i] == null, row was null");
                    return -2;
                }

                //give the first header payload to firstHeaderPayload
                byte[] firstHeaderPayload = HeaderPayload[i];

                //give the first data payload to firstDataPayload
                byte[] firstDataPayload = DataPayload[i];

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
            Console.WriteLine("for loop ended");
            return -3;

        }

        public static void HandleDataPayload(byte[][] DataPayload)
        {

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
