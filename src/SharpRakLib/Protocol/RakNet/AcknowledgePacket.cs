using System.Collections.Generic;
using SharpRakLib.Nio;

namespace SharpRakLib.Protocol.RakNet
{
	public abstract class AcknowledgePacket : RakNetPacket
	{
		public int[] Packets;

		public override void _encode(IBuffer buffer)
		{
			IBuffer payload = JavaByteBuffer.Allocate(0, false);
			var count = Packets.Length;
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
							payload.PutBoolean(true);
							payload.PutLTriad(start);
							start = last = current;
						}
						else
						{
							payload.PutBoolean(false);
							payload.PutLTriad(start);
							payload.PutLTriad(last);
							start = last = current;
						}
						records = records + 1;
					}
				}

				if (start == last)
				{
					payload.PutBoolean(true);
					payload.PutLTriad(start);
				}
				else
				{
					payload.PutBoolean(false);
					payload.PutLTriad(start);
					payload.PutLTriad(last);
				}
				records = records + 1;
			}
			/*
        buffer = JavaByteBuffer.allocate(payload.toByteArray().length + 3, ByteOrder.BIG_ENDIAN);
        buffer.putByte(getPID());
        */
			buffer.PutUnsignedShort((ushort) records);
			buffer.Put(payload.ToByteArray());
		}

		public override void _decode(IBuffer buffer)
		{
			int count = buffer.GetUnsignedShort();
			var packets = new List<int>();
			var cnt = 0;
			for (var i = 0; i < count && buffer.GetRemainingBytes() > 0 && cnt < 4096; i++)
			{
				if (!buffer.GetBoolean())
				{
					var start = buffer.GetLTriad();
					var end = buffer.GetLTriad();
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
					packets.Add(buffer.GetLTriad());
				}
			}
			this.Packets = packets.ToArray();
		}

		public override int GetSize()
		{
			return 1;
		}
	}
}