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
using SharpPcapConsoleProject1.DataTypes;


namespace NpcapRemoteCapture
{
    //Main is responsible for picking the correct device and listening to it 

    //the class also handles the arrival of Packets with the function dev_OnPacketArrival(object sender, PacketCapture e)

    //and the 188 bytes payload storing using the function StoreData(Packet packet, byte[][] DataPayload) 

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
        TransportPackets packet = new TransportPackets();
        
        byte currentByte = 0;

        //Main function is responsible for picking the correct device and listening to it
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
            
            //prints all devices description
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



        // StoreData function reacives Packet packet and byte [][] DataPayload array
        // goes through the bytes in the packet and stores 188 byte chunks that start with 0x47 
        // also prints out the number of packets and their length and the value inside
        public static void StoreData(Packet packet, byte[][] DataPayload)
        {

            //global
            int packetLength = 188;

            //static 
            int packetIndex = 0;

            //goes through the bytes in the packet and stores 188 byte chunks that start with 0x47
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

            //print number of packet its length and the value inside 
            for (int i = 0; i < DataPayload.Length - 1; i++)
            {
                // Process the 188-byte packets
                if (DataPayload[i] != null)
                {
                    Console.WriteLine("Packet {0}: with the length of {1} :{2}", i, DataPayload[i].Length, BitConverter.ToString(DataPayload[i]));

                }
            }
        }

        
        //dev_OnPacketArrival function is responsible for handling the arrival of Packets
        //checks if the packet contains IP packet, UDP packet, and if it contains the correct IP and port
        // if so, calls the StoreData function and the ParseHeaderPayload function
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
                            Parser.ParseHeaderPayload(DataPayload,pat);  

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
