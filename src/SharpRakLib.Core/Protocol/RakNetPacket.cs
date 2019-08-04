using System.IO;
using SharpRakLib.Util;

namespace SharpRakLib.Protocol
{
	public abstract class RakNetPacket : IPacket<BedrockStream>
	{
		/**
		* The time this packet was last sent at. This is used internally by JRakLibPlus, and
		* sometimes may be null.
		*/
		public long SendTime;

		/**
		 * Encodes this packet into a byte array.
		 * @return The encoded bytes of this packet.
		 */

		protected byte[] Raw { get; set; }
		public byte[] Encode()
		{
			//IBuffer b = JavaByteBuffer.Allocate(GetSize(), false);
			using (MemoryStream ms = new MemoryStream())
			{
				using (BedrockStream stream = new BedrockStream(ms))
				{
					stream.WriteByte(GetPid());
					_encode(stream);
				}

				return ms.ToArray();
			}
		}

		/**
		 * Decodes this packet from a byte array.
		 * @param bytes The raw byte array of this packet to be decoded from.
		 */

		public void Decode(byte[] bytes)
		{
			Raw = bytes;
			//IBuffer b = JavaByteBuffer.Wrap(bytes, false);
			using (BedrockStream stream = new BedrockStream(new MemoryStream(bytes)))
			{
				stream.ReadByte();// b.GetByte();
				_decode(stream);
			}
		}

		public abstract void _encode(BedrockStream buffer);

		public abstract void _decode(BedrockStream buffer);

		/**
		 * Get this packet's PacketID. The PacketID is always the first byte of the packet.
		 * @return This packet's PacketID.
		 */
		public abstract byte GetPid();

		/**
		 * Get the correct size for this packet (in bytes). Subclasses may override this.
		 * @return The size for the packet (in bytes). The default is zero
		 */
		public abstract int GetSize();
		
		public void Encode(BedrockStream stream)
		{
			stream.WriteByte(GetPid());
			_encode(stream);
		}

		public void Decode(BedrockStream stream)
		{
			_decode(stream);
		}
	}
}