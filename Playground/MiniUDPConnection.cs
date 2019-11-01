using Papagei;
using MiniUDP;
using System;

namespace Playground
{
    public class MiniUDPConnection : IConnection
    {
        public event Action<byte[], int> PayloadReceived = (_, __) => { };

        private readonly NetPeer _peer;

        public MiniUDPConnection(NetPeer peer)
        {
            _peer = peer;
            _peer.PayloadReceived += (peer_, data, length) =>
            {
                if (peer_ != _peer)
                {
                    throw new InvalidOperationException("Peer wrapper mismatch");
                }

                PayloadReceived.Invoke(data, length);
            };
        }

        public void SendPayload(byte[] data, int length)
        {
            _peer.SendPayload(data, (ushort)length);
        }
    }
}
