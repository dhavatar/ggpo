using System.Net;

namespace GGPOSharp.Interfaces
{
    public interface IUdpCallback
    {
        void OnMessage(IPEndPoint endpoint, NetworkMessage msg);
    }
}
