using MemoryPack;
using Network;

namespace JoltPhysics
{
    [MemoryPackable]
    public partial struct WorldDataMessage : INetworkMessage
    {
        public byte[] data;
    }
}
