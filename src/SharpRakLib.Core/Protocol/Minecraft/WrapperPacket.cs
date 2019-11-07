using SharpRakLib.Util;

namespace SharpRakLib.Protocol.Minecraft
{
    public class WrapperPacket : RakNetPacket
    {
        public byte[] Payload { get; set; }
        
        public override void _encode(BedrockStream buffer)
        {
            buffer.Write(Payload);
        }

        public override void _decode(BedrockStream buffer)
        {
            Payload = buffer.Read(0, true);
        }

        public override byte GetPid()
        {
            return 0xfe;
        }

        public override int GetSize()
        {
            return Payload.Length;
        }
    }
}