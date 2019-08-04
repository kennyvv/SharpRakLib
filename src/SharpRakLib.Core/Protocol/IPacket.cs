using System.IO;

namespace SharpRakLib.Protocol
{
    public interface IPacket<in TStream> where TStream : Stream
    {
        void Encode(TStream stream);
        void Decode(TStream stream);
    }
}