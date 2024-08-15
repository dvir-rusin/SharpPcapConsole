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
        //global information 
        private static readonly int targetPort = 2000;
        private static readonly string targetIpAddress = "224.0.0.0";
        static int ByteSize = 8;

        //static 
        static int Packetcount = 0;

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
            
            //global 
            
            int packetLength = 188;
            int amountOfHeaderBytes = 4;

            //static 
            int packetIndex = 0;


            //copying the 184-byte packets into the packetArray
            //going through the entire packet bytes
            //can jump over if you dont find 0x47 in the last 180
            //cuz needs to be 188 bytes
            for (int i = 0; i < packet.Bytes.Length - (packetLength-8); i++)//188-8 jumps to the end if does not find 0x47 at the end
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
                    packetArray[packetIndex] = singlePacket;
                    packetIndex++;

                    // Move the index to the next packet start (i.e., skip the current 188-byte packet)
                    i += packetLength-1; // Because loop increments i, we do i += 187 to move to the next packet start
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


        public static int HandleHeaderPayload(byte[][] HeaderPayload)
        {

            for(int i = 0; i < HeaderPayload.Length-1;i++)
            {
                if (HeaderPayload[i] == null)
                {
                    Console.WriteLine("HeaderPayload amount of rows was 0");
                    return -2;
                }

                byte[] firstHeaderPayload = HeaderPayload[i];
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
                if (varPidOfBitArray == 0x0000)
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
                    var udppacket = new UdpPacket(((UdpPacket)(packet.PayloadPacket.PayloadPacket)).SourcePort,
                    ((UdpPacket)(packet.PayloadPacket.PayloadPacket)).DestinationPort);

                    //check if udp DestinationPort is correct / equal to 2000 port
                    if (udppacket.DestinationPort == targetPort)
                    {
                        Console.WriteLine("UDP packet detected from {0}:{1}", targetIpAddress, targetPort);

                        // Initialize arrays to hold 184 byte data pyloads and the 4 byte headers
                        byte[][] HeaderPayload = new byte[packet.Bytes.Length / packetLength][];
                        byte[][] packetArray = new byte[packet.Bytes.Length / packetLength][];

                        //call the storeData function, goes throgh the bytes in the packet and stores 184 byte chunks that start with 0x47
                        //and also stores the 4 byte headers in headers [][] byte array 
                        StoreData(packet, HeaderPayload, packetArray);

                        //returns an int that describes what pid was in the payload : pat,cat,pmt,tsdt,and errors
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
                                 Console.WriteLine("------------------default--------------");
                                 break;
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
                    //if the ip destination and target port are diffrent still print where the packet has come from 
                    Console.WriteLine("ip was not correct  details:  {0}:{1}", targetIpAddress, targetPort);
                }
            }
        }

        
    }
}
