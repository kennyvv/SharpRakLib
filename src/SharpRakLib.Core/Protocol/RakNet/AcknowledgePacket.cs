using System;
using System.Collections.Generic;
using System.Linq;
using SharpRakLib.Util;

namespace SharpRakLib.Protocol.RakNet
{
	public abstract class AcknowledgePacket : RakNetPacket
	{
		public int[] Packets;

		public override void _encode(BedrockStream buffer)
		{
			//IBuffer payload = JavaByteBuffer.Allocate(0, false);
			var ranges = Slize(Packets.ToList());
			buffer.WriteBEShort((short) ranges.Count);
			foreach (var range in ranges)
			{
				var singleEntry = (byte) (range.Item1 == range.Item2 ? 0x01 : 0);
				buffer.WriteByte(singleEntry);
				buffer.WriteLTriad(range.Item1);
				if (singleEntry == 0)
					buffer.WriteLTriad(range.Item2);
			}
			/*var count = Packets.Length;
			var records = 0;

			if (count > 0)
			{
				var pointer = 0;
				var start = Packets[0];
				var last = Packets[0];

				while (pointer + 1 < count)
				{
					var current = Packets[pointer++];
					var diff = current - last;
					if (diff == 1)
					{
						last = current;
					}
					else if (diff > 1)
					{
						//Forget about duplicated packets (bad queues?)
						if (start == last)
						{
							buffer.WriteBool(true);
							buffer.WriteLTriad(start);
							start = last = current;
						}
						else
						{
							buffer.WriteBool(false);
							buffer.WriteLTriad(start);
							buffer.WriteLTriad(last);
							start = last = current;
						}
						records = records + 1;
					}
				}

				if (start == last)
				{
					buffer.WriteBool(true);
					buffer.WriteLTriad(start);
				}
				else
				{
					buffer.WriteBool(false);
					buffer.WriteLTriad(start);
					buffer.WriteLTriad(last);
				}
				records = records + 1;
			}*/
			/*
        buffer = JavaByteBuffer.allocate(payload.toByteArray().length + 3, ByteOrder.BIG_ENDIAN);
        buffer.putByte(getPID());
        *
			buffer.PutUnsignedShort((ushort) records);
			buffer.Put(payload.ToByteArray());*/
		}

		public override void _decode(BedrockStream buffer)
		{
			int count = buffer.ReadBEShort();
			var packets = new List<int>();
			var cnt = 0;
			for (var i = 0; i < count; i++)
			{
				if (!buffer.ReadBool())
				{
					var start = buffer.ReadLTriad();
					var end = buffer.ReadLTriad();
					if (end - start > 512)
					{
						end = start + 512;
					}
					for (var c = start; c <= end; c++)
					{
						cnt = cnt + 1;
						packets.Add(c);
					}
				}
				else
				{
					packets.Add(buffer.ReadLTriad());
				}
			}
			this.Packets = packets.ToArray();
		}

		public override int GetSize()
		{
			return 1;
		}
		
		public static List<Tuple<int, int>> Slize(List<int> acks)
		{
			List<Tuple<int, int>> ranges = new List<Tuple<int, int>>();

			if (acks.Count == 0) return ranges;

			int start = acks[0];
			int prev = start;

			if (acks.Count == 1)
			{
				ranges.Add(new Tuple<int, int>(start, start));
				return ranges;
			}

			acks.Sort();


			for (int i = 1; i < acks.Count; i++)
			{
				bool isLast = i + 1 == acks.Count;
				int current = acks[i];

				if (current - prev == 1 && !isLast)
				{
					prev = current;
					continue;
				}

				if (current - prev > 1 && !isLast)
				{
					ranges.Add(new Tuple<int, int>(start, prev));

					start = current;
					prev = current;
					continue;
				}

				if (current - prev == 1 && isLast)
				{
					ranges.Add(new Tuple<int, int>(start, current));
				}

				if (current - prev > 1 && isLast)
				{
					if (prev == start)
					{
						ranges.Add(new Tuple<int, int>(start, current));
					}

					if (prev != start)
					{
						ranges.Add(new Tuple<int, int>(start, prev));
						ranges.Add(new Tuple<int, int>(current, current));
					}
				}
			}

			return ranges;
		}
	}
}