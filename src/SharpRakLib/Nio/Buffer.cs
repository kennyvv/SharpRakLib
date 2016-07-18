using SharpRakLib.Util;

namespace SharpRakLib.Nio
{
	public interface IBuffer
	{
		byte[] Get(int len);

		void Put(byte[] bytes);

		byte GetByte();

		short GetShort();

		ushort GetUnsignedShort();

		int GetLTriad();

		int GetInt();

		long GetLong();

		string GetString();

		SystemAddress GetAddress();

		bool GetBoolean();

		void PutByte(byte b);

		void PutShort(short s);

		void PutUnsignedShort(ushort us);

		void PutLTriad(int t);

		void PutInt(int i);

		void PutLong(long l);

		void PutString(string s);

		void PutAddress(SystemAddress address);

		void PutBoolean(bool b);

		/**
		 * Skips <code>len</code> amount of bytes in the buffer. This increments the position by <code>len</code>
		 * @param len The amount of bytes to skip in the buffer.
		 */
		void Skip(int len);

		byte[] ToByteArray();

		int GetRemainingBytes();
	}
}