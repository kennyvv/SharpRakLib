using SharpRakLib.Nio;

namespace SharpRakLib.Protocol
{
	public abstract class RakNetPacket
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

		public byte[] Encode()
		{
			IBuffer b = JavaByteBuffer.Allocate(GetSize(), false);
			b.PutByte(GetPid());
			_encode(b);
			return b.ToByteArray();
		}

		/**
		 * Decodes this packet from a byte array.
		 * @param bytes The raw byte array of this packet to be decoded from.
		 */

		public void Decode(byte[] bytes)
		{
			IBuffer b = JavaByteBuffer.Wrap(bytes, false);
			b.GetByte();
			_decode(b);
		}

		public abstract void _encode(IBuffer buffer);

		public abstract void _decode(IBuffer buffer);

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
	}
}