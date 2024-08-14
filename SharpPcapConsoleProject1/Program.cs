using System;
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


        public static bool HandleHeaderPayload(byte[][] HeaderPayload)
        {
            byte[] firstHeaderPayload = HeaderPayload[0];
            int[] bitArray = new int[32];

            // Convert the 4 bytes in firstHeaderPayload to a bit array
            for (int i = 0; i < firstHeaderPayload.Length; i++)
            {
                byte currentByte = firstHeaderPayload[i];

                // Iterate over each bit in the current byte
                for (int j = 0; j < 8; j++)
                {
                    // Extract the bit using a mask and bit shift
                    bitArray[i * 8 + j] = (int)(currentByte >> (7 - j)) & 1;
                }
            }
            Console.WriteLine("bit array representation");
            for (int i = 0; i < bitArray.Length - 1; i++) {

                Console.Write(bitArray[i]);
            }

            for(int i = 11; i < 24;i++)
            {
                if (bitArray[i] != 0)
                {
                    Console.WriteLine("false - this is not pid 0");
                    return false;
                }
                    
            }


            // Now you have a bitArray representing the bits of the 4 bytes

            // Example of using thiredByte and forthByte
            //byte thiredByteCopy = firstHeaderPayload[2];
            //byte forthByte = firstHeaderPayload[3];
            //int rotateBitAmount = 1;

            //// Perform the right rotation
            //thiredByteCopy = rightRotate(thiredByteCopy, rotateBitAmount);

            //// Check condition on thiredByteCopy and forthByte
            //if ((thiredByteCopy & 0x80) != 0 || forthByte == 0) // 0x80 is 1000 0000 in binary
            //{
            //    return true;
            //}
            //else
            //{
            //    return false;
            //}
            Console.WriteLine("true - this is pid 0");
            return true;
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
            if (packet.PayloadPacket is IPPacket ipPacket )
            {
                // Check if the IP packet contains a UDP packet and matches the target IP
                    var udppacket = new UdpPacket(((UdpPacket)(packet.PayloadPacket.PayloadPacket)).SourcePort,
                        ((UdpPacket)(packet.PayloadPacket.PayloadPacket)).DestinationPort);

                    if (ipPacket.DestinationAddress.ToString() == targetIpAddress && udppacket.DestinationPort == targetPort)
                    {
                        Console.WriteLine("UDP packet detected from {0}:{1}", targetIpAddress, targetPort);
                        //byte [] HeaderPayload = packet.PayloadPacket.HeaderData;
                        // Check if the payload is MPEG-TS


                        byte[][] HeaderPayload = new byte[packet.Bytes.Length / 188][];
                        byte[][] packetArray = new byte[packet.Bytes.Length / 188][];  // Initialize an array to hold the byte arrays
                                                                                   //call the store data function which goes throgh the bytes in the packet and store 188 byte chunks that start with 71 decimal 
                        StoreData(packet, HeaderPayload, packetArray);
                        if(HandleHeaderPayload(HeaderPayload))
                        {

                        } 
                    }
                    else
                    {
                        //if the ip destination and target port are diffrent still print where the packet has come from 
                        Console.WriteLine("UDP packet detected from {0}:{1}", targetIpAddress, targetPort);
                    }
            }
        }

        
    }
}
