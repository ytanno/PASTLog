using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;

using System;
using System.Text;




namespace PacketCapture
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			//Platform need x86	
			//nuget PcapDotNet
			//use library PcapDotNet https://pcapdotnet.codeplex.com/
			DispDeviceList();

			//InterBankDirect IP may be 204.16.181.24
			DispPacketInfo(0, (ushort)80);

			//SendPacketSimple(0);

			Console.ReadLine();
		}

		private static void SendPacketSimple(int deviceIndex)
		{
			var device = LivePacketDevice.AllLocalMachine[deviceIndex];
			using ( var com = device.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 1000) )
			{
				var srcIP = new IpV4Address("192.168.0.2");
				var dstIP = new IpV4Address("192.168.0.2");
				var packet = GetSimplePacket(srcIP, dstIP);

				for ( int i = 0; i < 100; i++ ) com.SendPacket(packet);
			}
		}

		private static Packet GetSimplePacket(IpV4Address src, IpV4Address dst)
		{
			var packet =
						PacketBuilder.Build(DateTime.Now,
						new EthernetLayer
						{
							Source = new MacAddress("01:01:01:01:01:01"),
							Destination = new MacAddress("02:02:02:02:02:02"),
							EtherType = EthernetType.None,
						},
						new IpV4Layer
						{
							Source = src,
							CurrentDestination = dst,
							Fragmentation = IpV4Fragmentation.None,
							HeaderChecksum = null, // Will be filled automatically.
							Identification = 123,
							Options = IpV4Options.None,
							Protocol = IpV4Protocol.Tcp,
							Ttl = 100,
							TypeOfService = 0,
						},
						new PayloadLayer
						{
							Data = new Datagram(Encoding.ASCII.GetBytes("hello world")),
						});

			return packet;
		}

		private static void DispDeviceList()
		{
			var count = 0;
			foreach ( var device in LivePacketDevice.AllLocalMachine )
			{
				//Console.Write(( count + 1 ) + ". " + device.Name);
				if ( device.Description != null ) Console.WriteLine("[{0}]" + device.Description, count++);
				//else Console.WriteLine(" (No description available)");
			}
		}

		public static void DispPacketInfo(int deviceIndex, ushort searchPort)
		{
			var device = LivePacketDevice.AllLocalMachine[deviceIndex];
			using ( var com = device.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 1000) )
			{
				Console.WriteLine("Listening on " + device.Description + "...");

				using ( var filter = com.CreateFilter("ip and tcp") )
				{
					// Set the filter
					com.SetFilter(filter);
				}

				//unlimited if loopcount less than 1
				var loopCount = -1;
				com.ReceivePackets(loopCount, new HandlePacket((Packet p) =>
				{
					var ip = p.Ethernet.IpV4;
					var tcp = ip.Tcp;
					if ( tcp != null )
					{
						if ( tcp.SourcePort == searchPort || tcp.DestinationPort == searchPort )
						{
							var mainData = ip.Payload;

							Console.WriteLine("{0} {1}:{2} -> {3}:{4} {5}",
								//p.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")
								p.Timestamp.ToString("HH:mm:ss.fff")
								, ip.Source, tcp.SourcePort, ip.Destination, tcp.DestinationPort, p.Length);
						}
					}
				}));
			}
		}
	}
}