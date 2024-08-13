using System;
using System.Net.Sockets;
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
        static int count = 0;
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
                device.Open(DeviceModes.MaxResponsiveness, 1000);//fix 

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



        private static bool IsMpegTsPacket(byte[] data)
        {
            // MPEG-TS packets start with a sync byte 0x47
            return data.Length >= 188;//&& data[0] == 0x47;
        }

        static void dev_OnPacketArrival(object sender, PacketCapture e)
        {
            count++;
            
            Console.WriteLine("packets : {0}", count);

            var rawPacket = e.GetPacket();
            Console.WriteLine("e.GetPacket : {0}", rawPacket);

            var rawPacketLength = rawPacket.Data.Length;
            rawPacket.PacketLength.ToString();

            // Parse the packet using PacketDotNet
            //if (rawPacketLength == 188)
            //{
            
                var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            
                Console.WriteLine("rawPacketLength : {0}", rawPacketLength);
            Console.WriteLine("rawPacket.LinkLayerType : {0}", rawPacket.LinkLayerType);
            Console.WriteLine("rawPacket.Data : {0}", rawPacket.Data);

            // Check if the packet is an Ethernet packet
            //packet.PayloadPacket

            // Check if the Ethernet packet contains an IP packet
            if (packet.PayloadPacket is IPPacket ipPacket )
                {
                // Check if the IP packet contains a UDP packet and matches the target IP
                    var udppacket = new UdpPacket(((UdpPacket)(packet.PayloadPacket.PayloadPacket)).SourcePort, ((UdpPacket)(packet.PayloadPacket.PayloadPacket)).DestinationPort);
                    if (ipPacket.DestinationAddress.ToString() == targetIpAddress &&

                        udppacket.DestinationPort == targetPort)
                    {
                        Console.WriteLine("UDP packet detected from {0}:{1}", targetIpAddress, targetPort);
                        byte [] payload = packet.PayloadPacket.HeaderData;
                    // Check if the payload is MPEG-TS

                    // Assuming packet.Bytes is the byte array containing your packet data
                    byte[][] packetArray = new byte[packet.Bytes.Length / 188][];  // Initialize an array to hold the byte arrays
                    int packetIndex = 0;


                    //copying the 184-byte packets into the packetArray
                    for (int i = 0; i < packet.Bytes.Length - 187; i++)
                    {
                        if (packet.Bytes[i] == 0x47)
                        {
                            // Found 0x47, now skip the next 3 bytes and copy the following 184 bytes
                            byte[] singlePacket = new byte[184];
                            Array.Copy(packet.Bytes, i + 4, singlePacket, 0, 184);

                            // Store the 184-byte array in the packet array
                            packetArray[packetIndex] = singlePacket;
                            packetIndex++;

                            // Move the index to the next packet start (i.e., skip the current 188-byte packet)
                            i += 187; // Because loop increments i, we do i += 187 to move to the next packet start
                        }
                    }

                    for (int i = 0; i < packetArray.Length-1; i++)
                    {
                        // Process the 184-byte packets
                        Console.WriteLine("Packet {0}: with the length of {1} :{2}", i, packetArray[i].Length, BitConverter.ToString(packetArray[i]));
                    }

                    if (IsMpegTsPacket(payload))
                        {
                            Console.WriteLine("MPEG-TS packet captured with 188-byte payload");
                            // Further processing of the MPEG-TS packet
                        }
                    }
                    else
                    {
                        if (packet.PayloadPacket is IPPacket ippacket)
                            Console.WriteLine("test , packet datected from:{0}", ippacket.DestinationAddress.ToString());
                        if (packet.PayloadPacket is UdpPacket udpPacket1)
                            Console.WriteLine("test , packet  ip datected from:{0}", udpPacket1.DestinationPort); 

                    }



                }


            //}
        }
    }
}
