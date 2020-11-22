using GGPOSharp.Interfaces;
using System;
using System.Net;
using System.Net.Sockets;

namespace GGPOSharp.Network
{
    public class Udp : BaseLogging, IPollSink, IDisposable
    {
        public const int SIO_UDP_CONNRESET = -1744830452;

        private UdpClient udpClient;
        private Poll poll;
        private IUdpCallback callback;
        private IPEndPoint endpoint;

        public Udp(int port, Poll poll, IUdpCallback callback, ILog logger = null)
            : base(logger)
        {
            this.callback = callback;
            this.poll = poll;

            this.poll.RegisterSink(this);

            Log($"binding udp socket to port {port}.");
            endpoint = new IPEndPoint(IPAddress.Any, port);
            udpClient = new UdpClient(endpoint);

            // Ignore the connect reset message in Windows to prevent a UDP shutdown exception
            udpClient.Client.IOControl(
                (IOControlCode)SIO_UDP_CONNRESET,
                new byte[] { 0, 0, 0, 0 },
                null
            );
        }

        public void Dispose()
        {
            udpClient.Close();
            udpClient.Dispose();
            udpClient = null;
        }

        public void SendTo(byte[] buffer, IPEndPoint endpoint)
        {
            Log($"Sending packet size {buffer.Length} to {endpoint.Address}:{endpoint.Port}");
            udpClient.Send(buffer, buffer.Length, endpoint);
        }

        public void OnLoopPoll(object state)
        {
            try
            {
                var endpoint = default(IPEndPoint);
                var data = udpClient.Receive(ref endpoint);

                if (data.Length > 0)
                {
                    Log($"Udp: Received {data.Length} bytes from {endpoint.Address}:{endpoint.Port}");
                    var msg = Utility.Deserialize(data);
                    if (msg != null)
                    {
                        callback.OnMessage(endpoint, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Udp: udpClient.Receive error!: {ex.Message}");
            }
        }
    }
}
