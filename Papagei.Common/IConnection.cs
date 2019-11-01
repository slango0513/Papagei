using System;

namespace Papagei
{
    public interface IConnection
    {
        // void PayloadReceived(peer, data, lenght)
        event Action<byte[], int> PayloadReceived;

        void SendPayload(byte[] data, int length);
    }
}
