using System;
using System.Text;

namespace SharpRakLib
{
	public static class Extensions
	{
		public static byte[] GetBytes(this string str)
		{
			return Encoding.UTF8.GetBytes(str);
		}

		public static long NextLong(this Random rnd)
		{
			var data = new byte[8];
			rnd.NextBytes(data);
			return BitConverter.ToInt64(data, 0);
		}
	}
}