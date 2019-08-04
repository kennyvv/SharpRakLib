using System.Net;

namespace SharpRakLib.Util
{
	public class SystemAddress
	{
		private readonly string _ipAddress;
		private readonly int _port;
		private readonly int _version;

		public SystemAddress(string ipAddress, int port, int version)
		{
			this._ipAddress = ipAddress;
			this._port = port;
			this._version = version;
		}

		public static SystemAddress FromIpEndPoint(IPEndPoint address)
		{
			return new SystemAddress(address.Address.ToString(), address.Port, 4);
		}

		public IPEndPoint ToIpEndPoint()
		{
			return new IPEndPoint(IPAddress.Parse(_ipAddress), _port);
		}

		public int GetPort()
		{
			return _port;
		}

		public string GetIpAddress()
		{
			return _ipAddress;
		}

		public int GetVersion()
		{
			return _version;
		}

		public override string ToString()
		{
			return _ipAddress + ":" + _port;
		}

		public override bool Equals(object obj)
		{
			if (obj is IPEndPoint)
			{
				return ((IPEndPoint) obj).Address.ToString().Equals(_ipAddress) && ((IPEndPoint) obj).Port == _port;
			}
			return obj.Equals(this);
		}

		public static implicit operator IPEndPoint(SystemAddress address)
		{
			return address.ToIpEndPoint();
		}
	}
}