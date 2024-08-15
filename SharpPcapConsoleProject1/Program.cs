using System;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection.Metadata;
using PacketDotNet;
using PacketDotNet.Ieee80211;
using SharpPcap;
using SharpPcap.LibPcap;

namespace NpcapRemoteCapture
{
    public class Program
    {

        private static readonly int targetPort = 2000;
        private static readonly string targetIpAddress = "224.0.0.0";
        static int Packetcount = 0;
        static int ByteSize = 8;

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

            var device = devices[loopbackDeviceIndex];

            Console.WriteLine("Using device: {0}", device.Description);

            // Open the device for capture
            device.OnPacketArrival += new PacketArrivalEventHandler(dev_OnPacketArrival);

            try
            {
                device.Open(DeviceModes.MaxResponsiveness, 1000);//currently working with running the video on the background
                                                                 //still need to figure out how this works 

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


        

        public static void StoreData(Packet packet, byte[][] HeaderPayload, byte[][] packetArray)
        {
            // Assuming packet.Bytes is the byte array containing your packet data
            
            int packetIndex = 0;


            //copying the 184-byte packets into the packetArray
            for (int i = 0; i < packet.Bytes.Length - 187; i++)//going through the entire packet bytes
            {
                if (packet.Bytes[i] == 0x47)
                {
                    // Found 0x47, now skip the next 3 bytes and copy the following 184 bytes
                    byte[] singlePacket = new byte[184];
                    byte[] singleHeaderPacket = new byte[4];
                    Array.Copy(packet.Bytes, i, singleHeaderPacket, 0, 4);
                    Array.Copy(packet.Bytes, i + 4, singlePacket, 0, 184);

                    // Store the 184-byte array in the packet array
                    HeaderPayload[packetIndex] = singleHeaderPacket;
                    packetArray[packetIndex] = singlePacket;
                    packetIndex++;

                    // Move the index to the next packet start (i.e., skip the current 188-byte packet)
                    i += 187; // Because loop increments i, we do i += 187 to move to the next packet start
                }
            }

            for (int i = 0; i < packetArray.Length - 1; i++)//print number of packet its length and the value inside 
            {
                // Process the 184-byte packets
                if (packetArray[i] != null)
                {
                    Console.WriteLine("Packet {0}: with the length of {1} :{2}", i, packetArray[i].Length, BitConverter.ToString(packetArray[i]));

                }
            }
        }

        static byte rightRotate(byte thiredByte, int amountofrotbits)
        {
            // Ensure that amountofrotbits is between 0 and 7
            amountofrotbits = amountofrotbits % 8;

            // Perform the right rotate
            return (byte)((thiredByte >> amountofrotbits) | (thiredByte << (8 - amountofrotbits)) & 0xFF);
        }


        public static int HandleHeaderPayload(byte[][] HeaderPayload)
        {
            if (HeaderPayload[0]==null)
            {
                return -2;
            }
            byte[] firstHeaderPayload = HeaderPayload[0];

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
            if (varPidOfBitArray == 0)
            {
                Console.WriteLine("PID is 0 -> arrived to PAT");
                return 0;
            }
            if (varPidOfBitArray == 0x0001)
            {
                Console.WriteLine("PID is 0x0001 -> arrived to CAT");
                return 1;
            }
            if (varPidOfBitArray == 0x0002)
            {
                Console.WriteLine("PID is 0x0002 -> arrived to TSDT");
                return 2;
            }
            if (varPidOfBitArray >= 0x0030 && varPidOfBitArray <= 0x1FFF)
            {
                Console.WriteLine("PID is >= 0x30 && <= 0x1FFF -> arrived to PMT");
                return 3;
            }
            if (varPidOfBitArray == 0x0010)
            {
                Console.WriteLine("PID is 0x0010 -> arrived to NIT");
                return 4;
            }
            else
            {
                Console.WriteLine("Unrecognized PID");
                return -1;
            }
        }



        static void dev_OnPacketArrival(object sender, PacketCapture e)
        {
            Packetcount++;
            Console.WriteLine("packets : {0}", Packetcount);

            var rawPacket = e.GetPacket();
            Console.WriteLine("e.GetPacket : {0}", rawPacket);

            var rawPacketLength = rawPacket.Data.Length;
            rawPacket.PacketLength.ToString();

           
            
            var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            
            Console.WriteLine("rawPacketLength : {0}", rawPacketLength);
            Console.WriteLine("rawPacket.LinkLayerType : {0}", rawPacket.LinkLayerType);
            Console.WriteLine("rawPacket.Data : {0}", rawPacket.Data);


            // Check if the Ethernet packet contains an IP packet
            if (packet.PayloadPacket is IPPacket ipPacket)
            {
                if (ipPacket.DestinationAddress.ToString() == targetIpAddress)
                {
                    // Check if the IP packet contains a UDP packet and matches the target IP
                    var udppacket = new UdpPacket(((UdpPacket)(packet.PayloadPacket.PayloadPacket)).SourcePort,
                    ((UdpPacket)(packet.PayloadPacket.PayloadPacket)).DestinationPort);



                    if (udppacket.DestinationPort == targetPort)
                    {
                        Console.WriteLine("UDP packet detected from {0}:{1}", targetIpAddress, targetPort);
                        //byte [] HeaderPayload = packet.PayloadPacket.HeaderData;
                        // Check if the payload is MPEG-TS


                        byte[][] HeaderPayload = new byte[packet.Bytes.Length / 188][];
                        byte[][] packetArray = new byte[packet.Bytes.Length / 188][];  // Initialize an array to hold the byte arrays
                                                                                       //call the store data function which goes throgh the bytes in the packet and store 188 byte chunks that start with 71 decimal 
                        StoreData(packet, HeaderPayload, packetArray);
                        if (HeaderPayload.Length > 0 )
                        {
                            switch (HandleHeaderPayload(HeaderPayload))
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

                                default:
                                    Console.WriteLine("------------------defoult--------------");
                                    break;
                            }
                        }
                        else
                        {
                            Console.WriteLine("HeaderPayload amount of row was 0");
                        }
                    }
                    else
                    {
                        //if the ip destination and target port are diffrent still print where the packet has come from 
                        Console.WriteLine("target port was not correct details:  {0}:{1}", targetIpAddress, targetPort);
                    }
                }
                else
                {
                    Console.WriteLine("ip was not correct  details:  {0}:{1}", targetIpAddress, targetPort);
                }
            }
        }

        
    }
}
