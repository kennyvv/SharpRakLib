namespace SimpleServer
{
	public class MotdProvider
	{
		private const int Protocol = 81;
		private const string MinecraftVersion = "0.15.0";

		public string Motd = "SimpleServer for MCPE (RakLib Test)";
		public int OnlinePlayers = 0;
		public int MaxPlayers = 1;

		public string GetMotd()
		{
			return string.Format("MCPE;{0};{1};{2};{3};{4}", Motd, Protocol, MinecraftVersion, OnlinePlayers, MaxPlayers);
		}
	}
}
